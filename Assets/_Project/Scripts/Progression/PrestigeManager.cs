using System.Collections.Generic;
using TapLegends.Combat;
using TapLegends.Core;
using UnityEngine;

namespace TapLegends.Progression
{
    /// <summary>
    /// Reliquias y mejoras permanentes. Renacer reinicia oro, heroes y nivel (reutilizando
    /// los mismos LoadState() que usa cargar una partida — un "reset" es solo "cargar una
    /// partida vacia") pero NUNCA toca reliquias ni mejoras de prestigio, que son lo unico
    /// que sobrevive de una vida a la siguiente (ver TapLegends-Plan.md, Etapa 5).
    /// </summary>
    public class PrestigeManager : MonoBehaviour
    {
        public static PrestigeManager Instance { get; private set; }

        [SerializeField] private PrestigeUpgradeData[] upgrades = System.Array.Empty<PrestigeUpgradeData>();

        private readonly Dictionary<PrestigeUpgradeData, int> levels = new Dictionary<PrestigeUpgradeData, int>();

        public double Relics { get; private set; }
        public IReadOnlyList<PrestigeUpgradeData> Upgrades => upgrades;

        public void Configure(PrestigeUpgradeData[] roster) => upgrades = roster ?? System.Array.Empty<PrestigeUpgradeData>();

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            foreach (var upgrade in upgrades)
                if (upgrade != null && !levels.ContainsKey(upgrade))
                    levels[upgrade] = 0;
        }

        public int GetLevel(PrestigeUpgradeData upgrade) =>
            upgrade != null && levels.TryGetValue(upgrade, out var lvl) ? lvl : 0;

        public double GetCost(PrestigeUpgradeData upgrade) =>
            upgrade == null ? 0d : upgrade.baseCost * System.Math.Pow(upgrade.costGrowth, GetLevel(upgrade));

        private double SumEffect(PrestigeEffect effect)
        {
            double total = 0d;
            foreach (var upgrade in upgrades)
                if (upgrade != null && upgrade.effect == effect)
                    total += GetLevel(upgrade) * upgrade.valuePerLevel;
            return total;
        }

        public double DamageMultiplier => 1d + SumEffect(PrestigeEffect.GlobalDamage);
        public double GoldMultiplier => 1d + SumEffect(PrestigeEffect.Gold);
        public double HeroDpsMultiplier => 1d + SumEffect(PrestigeEffect.HeroDps);

        /// <summary>Tope del 70% de reduccion: un cooldown que llega a 0 rompe el ritmo de la habilidad.</summary>
        public double CooldownMultiplier => System.Math.Max(0.3d, 1d - SumEffect(PrestigeEffect.SkillCooldownReduction));

        public bool CanPrestige => EnemySpawner.Instance != null && EnemySpawner.Instance.CurrentLevel >= GameConstants.PrestigeMinLevel;

        public double RelicsIfPrestigeNow =>
            EnemySpawner.Instance != null ? GameConstants.RelicsForLevel(EnemySpawner.Instance.CurrentLevel) : 0d;

        public bool TryBuyUpgrade(PrestigeUpgradeData upgrade)
        {
            if (upgrade == null) return false;
            double cost = GetCost(upgrade);
            if (Relics < cost) return false;

            Relics -= cost;
            levels[upgrade] = GetLevel(upgrade) + 1;
            EventBus.Publish(new RelicsChangedEvent { NewTotal = Relics });
            EventBus.Publish(new PrestigeUpgradeChangedEvent { UpgradeId = upgrade.id });
            return true;
        }

        /// <summary>Renacer: gana reliquias por el nivel maximo alcanzado y reinicia oro,
        /// daño de tap, heroes y nivel de oleada. Reliquias y mejoras de prestigio quedan igual.</summary>
        public bool TryPrestige()
        {
            if (!CanPrestige) return false;

            double gained = RelicsIfPrestigeNow;
            Relics += gained;

            GoldWallet.Instance?.LoadState(0d);
            PlayerStats.Instance?.LoadState(0);
            HeroRoster.Instance?.ResetAll();
            // hasLostBossOnce tambien se reinicia: cada Renacer vuelve a mostrar el primer
            // jefe (Grendal, nivel 10) en automatico, igual que la primerisima partida.
            EnemySpawner.Instance?.LoadState(1, 0, false);

            EventBus.Publish(new RelicsChangedEvent { NewTotal = Relics });
            EventBus.Publish(new PrestigeCompletedEvent { RelicsGained = gained });
            return true;
        }

        public List<PrestigeUpgradeSaveEntry> GetSaveEntries()
        {
            var entries = new List<PrestigeUpgradeSaveEntry>();
            foreach (var upgrade in upgrades)
            {
                if (upgrade == null) continue;
                int level = GetLevel(upgrade);
                if (level > 0) entries.Add(new PrestigeUpgradeSaveEntry { upgradeId = upgrade.id, level = level });
            }
            return entries;
        }

        public void LoadState(double savedRelics, List<PrestigeUpgradeSaveEntry> savedUpgrades)
        {
            Relics = savedRelics;

            if (savedUpgrades != null)
            {
                foreach (var entry in savedUpgrades)
                {
                    var upgrade = System.Array.Find(upgrades, u => u != null && u.id == entry.upgradeId);
                    if (upgrade == null) continue;

                    levels[upgrade] = entry.level;
                    EventBus.Publish(new PrestigeUpgradeChangedEvent { UpgradeId = upgrade.id });
                }
            }

            EventBus.Publish(new RelicsChangedEvent { NewTotal = Relics });
        }
    }
}
