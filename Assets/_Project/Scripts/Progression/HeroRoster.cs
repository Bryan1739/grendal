using System.Collections.Generic;
using TapLegends.Combat;
using TapLegends.Core;
using UnityEngine;

namespace TapLegends.Progression
{
    /// <summary>
    /// Nivel de cada heroe y su DPS combinado. Aplica daño al enemigo actual una vez por
    /// segundo (no por frame: evita spam de popups/flashes y coincide con como se
    /// balancearon las formulas — "daño por segundo" tal cual, no por frame).
    /// </summary>
    public class HeroRoster : MonoBehaviour
    {
        public static HeroRoster Instance { get; private set; }

        [SerializeField] private HeroData[] heroes = System.Array.Empty<HeroData>();

        private readonly Dictionary<HeroData, int> levels = new Dictionary<HeroData, int>();
        private float tickTimer;

        public IReadOnlyList<HeroData> Heroes => heroes;

        public void Configure(HeroData[] roster) => heroes = roster ?? System.Array.Empty<HeroData>();

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            foreach (var hero in heroes)
                if (hero != null && !levels.ContainsKey(hero))
                    levels[hero] = 0;
        }

        public int GetLevel(HeroData hero) => hero != null && levels.TryGetValue(hero, out var lvl) ? lvl : 0;

        public double GetCost(HeroData hero) =>
            hero == null ? 0d : hero.baseCost * System.Math.Pow(hero.costGrowth, GetLevel(hero));

        public double GetDps(HeroData hero)
        {
            if (hero == null) return 0d;
            int level = GetLevel(hero);
            if (level <= 0) return 0d;

            double baseValue = hero.baseDps * System.Math.Pow(hero.dpsGrowthMultiplier, level - 1);

            double prestigeMultiplier = PrestigeManager.Instance != null
                ? PrestigeManager.Instance.DamageMultiplier * PrestigeManager.Instance.HeroDpsMultiplier
                : 1d;
            double equipmentMultiplier = EquipmentManager.Instance != null
                ? EquipmentManager.Instance.HeroDpsMultiplier
                : 1d;
            double petMultiplier = PetManager.Instance != null
                ? PetManager.Instance.HeroDpsMultiplier
                : 1d;

            return baseValue * prestigeMultiplier * equipmentMultiplier * petMultiplier;
        }

        public double TotalDps
        {
            get
            {
                double total = 0d;
                foreach (var hero in heroes) total += GetDps(hero);
                return total;
            }
        }

        public List<HeroSaveEntry> GetSaveEntries()
        {
            var entries = new List<HeroSaveEntry>();
            foreach (var hero in heroes)
            {
                if (hero == null) continue;
                int level = GetLevel(hero);
                if (level > 0) entries.Add(new HeroSaveEntry { heroId = hero.id, level = level });
            }
            return entries;
        }

        /// <summary>Restaura niveles desde un guardado, emparejando por HeroData.id. Publica
        /// HeroChangedEvent por cada uno — sin esto, las filas de la UI y los sprites del
        /// campo de batalla se quedan mostrando "nivel 0" hasta la primera compra despues
        /// de cargar, porque nadie mas les avisa que el nivel real ya no es el inicial.</summary>
        public void LoadState(List<HeroSaveEntry> savedHeroes)
        {
            if (savedHeroes == null) return;
            foreach (var entry in savedHeroes)
            {
                var hero = System.Array.Find(heroes, h => h != null && h.id == entry.heroId);
                if (hero == null) continue;

                levels[hero] = entry.level;
                EventBus.Publish(new HeroChangedEvent { HeroId = hero.id });
            }
        }

        /// <summary>Vuelve todos los heroes a nivel 0. Usado solo por PrestigeManager.TryPrestige().</summary>
        public void ResetAll()
        {
            var heroesWithLevels = new List<HeroData>(levels.Keys);
            foreach (var hero in heroesWithLevels)
            {
                if (levels[hero] == 0) continue;
                levels[hero] = 0;
                EventBus.Publish(new HeroChangedEvent { HeroId = hero.id });
            }
        }

        public bool TryBuyOrUpgrade(HeroData hero)
        {
            if (hero == null) return false;
            double cost = GetCost(hero);
            if (GoldWallet.Instance == null || !GoldWallet.Instance.TrySpend(cost)) return false;

            levels[hero] = GetLevel(hero) + 1;
            EventBus.Publish(new HeroChangedEvent { HeroId = hero.id });
            return true;
        }

        private void Update()
        {
            double dps = TotalDps;
            if (dps <= 0d) return;

            tickTimer += Time.deltaTime;
            if (tickTimer < 1f) return;
            tickTimer -= 1f;

            EnemySpawner.Instance?.CurrentEnemyInstance?.TakeDamage(dps);
        }
    }
}
