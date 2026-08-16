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
        private float attackTimer;

        public PetData ActivePet { get; private set; }
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
            if (ActivePet == null || ActivePet.attackDps <= 0d) return;

            attackTimer += Time.deltaTime;
            if (attackTimer < ActivePet.attackInterval) return;
            attackTimer = 0f;

            double damage = ActivePet.attackDps * System.Math.Max(1, GetLevel(ActivePet));
            EnemySpawner.Instance?.CurrentEnemyInstance?.TakeDamage(damage);
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
                double factor = pet == ActivePet ? 1d : 0.2d; // ver TapLegends-Plan.md, Etapa 7
                total += GetBonusPercent(pet) * factor;
            }
            return total;
        }

        public double GoldMultiplier => 1d + SumBonus(PetEffect.Gold);
        public double TapDamageMultiplier => 1d + SumBonus(PetEffect.TapDamage);
        public double HeroDpsMultiplier => 1d + SumBonus(PetEffect.HeroDps);

        public void SetActive(PetData pet)
        {
            if (pet == null || !IsOwned(pet) || ActivePet == pet) return;
            ActivePet = pet;
            attackTimer = 0f;
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
                if (ActivePet == null) ActivePet = pet; // la primera mascota se activa sola
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
                entries.Add(new PetSaveEntry
                {
                    petId = pet.id,
                    level = s.level,
                    fragments = s.fragments,
                    isActive = pet == ActivePet,
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
                if (entry.isActive) ActivePet = pet;

                EventBus.Publish(new PetChangedEvent { PetId = pet.id });
            }
        }
    }
}
