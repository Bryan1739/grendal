using System.Collections.Generic;
using TapLegends.Combat;
using TapLegends.Core;
using UnityEngine;

namespace TapLegends.Progression
{
    /// <summary>
    /// Coleccion de mascotas. Se escucha a si mismo via EnemyKilledEvent para tirar un
    /// huevo cuando muere un jefe (probabilidad independiente del botin de equipo — pueden
    /// caer los dos, ninguno, o solo uno). Duplicados de una mascota ya conseguida se
    /// convierten en fragmentos para mejorarla (mismo criterio que EquipmentManager).
    /// </summary>
    public class PetManager : MonoBehaviour
    {
        public static PetManager Instance { get; private set; }

        private const float EggDropChance = 0.35f;
        private const int FragmentsPerDuplicate = 5;

        [SerializeField] private PetData[] pets = System.Array.Empty<PetData>();

        private class PetState
        {
            public bool owned;
            public int level;
            public int fragments;
        }

        private readonly Dictionary<PetData, PetState> state = new Dictionary<PetData, PetState>();

        // Hasta 2 mascotas activas a la vez, una a cada lado del heroe (pedido del usuario:
        // antes solo se podia tener 1 activa). Slot 0 = izquierda, slot 1 = derecha (ver
        // PetFieldSprite/SceneBuilder) — el indice en si no tiene significado de gameplay,
        // solo posicion en pantalla.
        private readonly PetData[] activeSlots = new PetData[2];
        private readonly float[] attackTimers = new float[2];

        public IReadOnlyList<PetData> Pets => pets;

        public void Configure(PetData[] roster) => pets = roster ?? System.Array.Empty<PetData>();

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            foreach (var pet in pets)
                if (pet != null && !state.ContainsKey(pet))
                    state[pet] = new PetState();
        }

        private void OnEnable() => EventBus.Subscribe<EnemyKilledEvent>(HandleEnemyKilled);
        private void OnDisable() => EventBus.Unsubscribe<EnemyKilledEvent>(HandleEnemyKilled);

        private void HandleEnemyKilled(EnemyKilledEvent evt)
        {
            if (evt.WasBoss && Random.value <= EggDropChance) RollEggDrop();
        }

        private void Update()
        {
            for (int i = 0; i < activeSlots.Length; i++)
            {
                var pet = activeSlots[i];
                if (pet == null || pet.attackDps <= 0d) continue;

                attackTimers[i] += Time.deltaTime;
                if (attackTimers[i] < pet.attackInterval) continue;
                attackTimers[i] = 0f;

                double damage = pet.attackDps * System.Math.Max(1, GetLevel(pet));
                EnemySpawner.Instance?.CurrentEnemyInstance?.TakeDamage(damage);
            }
        }

        private PetState GetState(PetData pet)
        {
            if (pet == null) return null;
            if (!state.TryGetValue(pet, out var s))
            {
                s = new PetState();
                state[pet] = s;
            }
            return s;
        }

        public bool IsOwned(PetData pet) => GetState(pet)?.owned ?? false;
        public int GetLevel(PetData pet) => GetState(pet)?.level ?? 0;
        public int GetFragments(PetData pet) => GetState(pet)?.fragments ?? 0;

        public double GetUpgradeCost(PetData pet) => pet == null ? 0d : 3d * (GetLevel(pet) + 1);

        public double GetBonusPercent(PetData pet)
        {
            var s = GetState(pet);
            if (pet == null || s == null || !s.owned) return 0d;
            return pet.baseBonusPercent * System.Math.Pow(pet.bonusGrowthPerLevel, System.Math.Max(0, s.level - 1));
        }

        private double SumBonus(PetEffect effect)
        {
            double total = 0d;
            foreach (var pet in pets)
            {
                if (pet == null || pet.effect != effect || !IsOwned(pet)) continue;
                double factor = IsActive(pet) ? 1d : 0.2d; // ver TapLegends-Plan.md, Etapa 7
                total += GetBonusPercent(pet) * factor;
            }
            return total;
        }

        public double GoldMultiplier => 1d + SumBonus(PetEffect.Gold);
        public double TapDamageMultiplier => 1d + SumBonus(PetEffect.TapDamage);
        public double HeroDpsMultiplier => 1d + SumBonus(PetEffect.HeroDps);

        public bool IsActive(PetData pet) => pet != null && (activeSlots[0] == pet || activeSlots[1] == pet);

        /// <summary>Mascota activa en un lado (0 = izquierda, 1 = derecha) o null si ese
        /// lado esta vacio — ver PetFieldSprite.</summary>
        public PetData GetActiveSlot(int slot) => slot >= 0 && slot < activeSlots.Length ? activeSlots[slot] : null;

        /// <summary>Alterna la mascota: si ya esta activa, la desequipa (libera su lado). Si
        /// no, la equipa en el primer lado libre — si los 2 estan ocupados, reemplaza el
        /// lado 0. Un solo boton "Activar" por mascota (no hay UI para elegir lado a mano;
        /// el usuario solo pidio poder tener hasta 2 a la vez, no eligiendo posicion).</summary>
        public void SetActive(PetData pet)
        {
            if (pet == null || !IsOwned(pet)) return;

            for (int i = 0; i < activeSlots.Length; i++)
            {
                if (activeSlots[i] != pet) continue;
                activeSlots[i] = null;
                EventBus.Publish(new PetChangedEvent { PetId = pet.id });
                return;
            }

            int freeSlot = activeSlots[0] == null ? 0 : activeSlots[1] == null ? 1 : 0;
            activeSlots[freeSlot] = pet;
            attackTimers[freeSlot] = 0f;
            EventBus.Publish(new PetChangedEvent { PetId = pet.id });
        }

        public bool TryUpgrade(PetData pet)
        {
            var s = GetState(pet);
            if (pet == null || s == null || !s.owned) return false;

            double cost = GetUpgradeCost(pet);
            if (s.fragments < cost) return false;

            s.fragments -= (int)cost;
            s.level++;
            EventBus.Publish(new PetChangedEvent { PetId = pet.id });
            EventBus.Publish(new UpgradePurchasedEvent());
            return true;
        }

        private void RollEggDrop()
        {
            if (pets == null || pets.Length == 0) return;

            var pet = pets[Random.Range(0, pets.Length)];
            var s = GetState(pet);
            if (s == null) return;

            bool isNew = !s.owned;
            if (isNew)
            {
                s.owned = true;
                s.level = 1;
                // La primera mascota se activa sola (en el primer lado libre).
                if (activeSlots[0] == null) activeSlots[0] = pet;
                else if (activeSlots[1] == null) activeSlots[1] = pet;
            }
            else
            {
                s.fragments += FragmentsPerDuplicate;
            }

            EventBus.Publish(new PetChangedEvent { PetId = pet.id });
            EventBus.Publish(new PetObtainedEvent
            {
                PetId = pet.id,
                DisplayName = pet.displayName,
                WasNew = isNew,
                FragmentsGained = isNew ? 0 : FragmentsPerDuplicate,
            });
        }

        public List<PetSaveEntry> GetSaveEntries()
        {
            var entries = new List<PetSaveEntry>();
            foreach (var pet in pets)
            {
                if (pet == null || !state.TryGetValue(pet, out var s) || !s.owned) continue;
                int slot = activeSlots[0] == pet ? 0 : activeSlots[1] == pet ? 1 : -1;
                entries.Add(new PetSaveEntry
                {
                    petId = pet.id,
                    level = s.level,
                    fragments = s.fragments,
                    activeSlot = slot,
                });
            }
            return entries;
        }

        public void LoadState(List<PetSaveEntry> savedPets)
        {
            if (savedPets == null) return;
            foreach (var entry in savedPets)
            {
                var pet = System.Array.Find(pets, p => p != null && p.id == entry.petId);
                if (pet == null) continue;

                state[pet] = new PetState { owned = true, level = entry.level, fragments = entry.fragments };
                if (entry.activeSlot == 0 || entry.activeSlot == 1) activeSlots[entry.activeSlot] = pet;

                EventBus.Publish(new PetChangedEvent { PetId = pet.id });
            }
        }
    }
}
