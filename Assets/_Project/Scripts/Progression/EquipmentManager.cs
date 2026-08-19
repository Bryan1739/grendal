using System.Collections.Generic;
using TapLegends.Core;
using UnityEngine;

namespace TapLegends.Progression
{
    /// <summary>
    /// Estado de las 5 piezas de equipo. Se escucha a si mismo via EnemyKilledEvent para
    /// tirar botin cuando muere un jefe — nadie mas necesita saber que el loot existe.
    /// Botin: rareza mas alta que la equipada -> se equipa (nivel vuelve a 1). Rareza igual
    /// o menor -> se convierte en fragmentos para mejorar la pieza actual (ver plan, Etapa 6).
    /// </summary>
    public class EquipmentManager : MonoBehaviour
    {
        public static EquipmentManager Instance { get; private set; }

        private static readonly EquipmentRarity[] RarityOrder =
        {
            EquipmentRarity.Common, EquipmentRarity.Uncommon, EquipmentRarity.Rare,
            EquipmentRarity.Epic, EquipmentRarity.Legendary,
        };

        // Probabilidad acumulada de tabla de botin (debe sumar 1).
        private static readonly float[] RarityWeights = { 0.50f, 0.30f, 0.15f, 0.04f, 0.01f };
        private static readonly int[] FragmentsPerRarity = { 1, 3, 8, 20, 50 };

        [SerializeField] private EquipmentData[] items = System.Array.Empty<EquipmentData>();

        private class SlotState
        {
            public bool owned;
            public EquipmentRarity rarity = EquipmentRarity.Common;
            public int level;
            public int fragments;
        }

        private readonly Dictionary<EquipmentData, SlotState> state = new Dictionary<EquipmentData, SlotState>();

        public IReadOnlyList<EquipmentData> Items => items;

        public void Configure(EquipmentData[] roster) => items = roster ?? System.Array.Empty<EquipmentData>();

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            foreach (var item in items)
                if (item != null && !state.ContainsKey(item))
                    state[item] = new SlotState();
        }

        private void OnEnable() => EventBus.Subscribe<EnemyKilledEvent>(HandleEnemyKilled);
        private void OnDisable() => EventBus.Unsubscribe<EnemyKilledEvent>(HandleEnemyKilled);

        private void HandleEnemyKilled(EnemyKilledEvent evt)
        {
            if (evt.WasBoss) RollDrop();
        }

        private SlotState GetState(EquipmentData item)
        {
            if (item == null) return null;
            if (!state.TryGetValue(item, out var s))
            {
                s = new SlotState();
                state[item] = s;
            }
            return s;
        }

        public bool IsOwned(EquipmentData item) => GetState(item)?.owned ?? false;
        public EquipmentRarity GetRarity(EquipmentData item) => GetState(item)?.rarity ?? EquipmentRarity.Common;
        public int GetLevel(EquipmentData item) => GetState(item)?.level ?? 0;
        public int GetFragments(EquipmentData item) => GetState(item)?.fragments ?? 0;

        public double GetUpgradeCost(EquipmentData item)
        {
            if (item == null) return 0d;
            int level = GetLevel(item);
            return item.baseFragmentCost * System.Math.Pow(item.fragmentCostGrowth, level);
        }

        public double GetEffectValue(EquipmentData item)
        {
            var s = GetState(item);
            if (item == null || s == null || !s.owned) return 0d;
            return item.baseValuePerLevel * RarityMultiplier(s.rarity) * Mathf.Max(1, s.level);
        }

        private static double RarityMultiplier(EquipmentRarity rarity) => rarity switch
        {
            EquipmentRarity.Common => 1d,
            EquipmentRarity.Uncommon => 1.8d,
            EquipmentRarity.Rare => 3.2d,
            EquipmentRarity.Epic => 6d,
            EquipmentRarity.Legendary => 12d,
            _ => 1d,
        };

        private double SumEffect(EquipmentEffect effect)
        {
            double total = 0d;
            foreach (var item in items)
                if (item != null && item.effect == effect)
                    total += GetEffectValue(item);
            return total;
        }

        public double TapDamageMultiplier => 1d + SumEffect(EquipmentEffect.TapDamage);
        public double SkillDamageMultiplier => 1d + SumEffect(EquipmentEffect.SkillDamage);
        public double GoldMultiplier => 1d + SumEffect(EquipmentEffect.Gold);
        public double HeroDpsMultiplier => 1d + SumEffect(EquipmentEffect.HeroDps);
        public double CooldownMultiplier => System.Math.Max(0.3d, 1d - SumEffect(EquipmentEffect.SkillCooldown));

        public bool TryUpgrade(EquipmentData item)
        {
            var s = GetState(item);
            if (item == null || s == null || !s.owned) return false;

            double cost = GetUpgradeCost(item);
            if (s.fragments < cost) return false;

            s.fragments -= (int)cost;
            s.level++;
            EventBus.Publish(new EquipmentChangedEvent { EquipmentId = item.id });
            EventBus.Publish(new UpgradePurchasedEvent());
            return true;
        }

        private void RollDrop()
        {
            if (items == null || items.Length == 0) return;

            var item = items[Random.Range(0, items.Length)];
            var rarity = RollRarity();
            ApplyDrop(item, rarity);
        }

        private static EquipmentRarity RollRarity()
        {
            float roll = Random.value;
            float cumulative = 0f;
            for (int i = 0; i < RarityWeights.Length; i++)
            {
                cumulative += RarityWeights[i];
                if (roll <= cumulative) return RarityOrder[i];
            }
            return EquipmentRarity.Common;
        }

        private void ApplyDrop(EquipmentData item, EquipmentRarity rarity)
        {
            var s = GetState(item);
            if (item == null || s == null) return;

            bool isUpgrade = !s.owned || rarity > s.rarity;

            if (isUpgrade)
            {
                s.owned = true;
                s.rarity = rarity;
                s.level = 1;
            }
            else
            {
                s.fragments += FragmentsPerRarity[System.Array.IndexOf(RarityOrder, rarity)];
            }

            EventBus.Publish(new EquipmentChangedEvent { EquipmentId = item.id });
            EventBus.Publish(new LootObtainedEvent
            {
                EquipmentId = item.id,
                DisplayName = item.displayName,
                Rarity = rarity.ToString(),
                WasUpgrade = isUpgrade,
                FragmentsGained = isUpgrade ? 0 : FragmentsPerRarity[System.Array.IndexOf(RarityOrder, rarity)],
            });
        }

        public List<EquipmentSaveEntry> GetSaveEntries()
        {
            var entries = new List<EquipmentSaveEntry>();
            foreach (var item in items)
            {
                if (item == null || !state.TryGetValue(item, out var s) || !s.owned) continue;
                entries.Add(new EquipmentSaveEntry
                {
                    equipmentId = item.id,
                    rarity = (int)s.rarity,
                    level = s.level,
                    fragments = s.fragments,
                });
            }
            return entries;
        }

        public void LoadState(List<EquipmentSaveEntry> savedItems)
        {
            if (savedItems == null) return;
            foreach (var entry in savedItems)
            {
                var item = System.Array.Find(items, i => i != null && i.id == entry.equipmentId);
                if (item == null) continue;

                state[item] = new SlotState
                {
                    owned = true,
                    rarity = (EquipmentRarity)entry.rarity,
                    level = entry.level,
                    fragments = entry.fragments,
                };
                EventBus.Publish(new EquipmentChangedEvent { EquipmentId = item.id });
            }
        }
    }
}
