using System.Collections.Generic;
using TapLegends.Combat;
using TapLegends.Core;
using UnityEngine;

namespace TapLegends.Progression
{
    /// <summary>
    /// Nivel, cooldown y activacion de cada habilidad. Los golpes instantaneos usan el
    /// TapDamage/DPS de heroes actual como base (mejorar esos sistemas tambien hace mas
    /// fuertes las habilidades). Los buffs temporales (TimedTapBuff/TimedGoldBuff) quedan
    /// activos un rato y se consultan desde PlayerStats/Enemy via TapBuffMultiplier/GoldBuffMultiplier
    /// — el mismo patron que Prestige/Equipment/Pet, todos exponen un multiplicador que se
    /// consulta en la fuente, ninguno "empuja" el valor a otro sistema.
    /// </summary>
    public class SkillManager : MonoBehaviour
    {
        public static SkillManager Instance { get; private set; }

        [SerializeField] private SkillData[] skills = System.Array.Empty<SkillData>();

        private readonly Dictionary<SkillData, int> levels = new Dictionary<SkillData, int>();
        private readonly Dictionary<SkillData, float> cooldownRemaining = new Dictionary<SkillData, float>();
        private readonly Dictionary<SkillData, float> buffRemaining = new Dictionary<SkillData, float>();

        public IReadOnlyList<SkillData> Skills => skills;

        public void Configure(SkillData[] roster) => skills = roster ?? System.Array.Empty<SkillData>();

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            foreach (var skill in skills)
                if (skill != null && !levels.ContainsKey(skill))
                    levels[skill] = 0;
        }

        private void Update()
        {
            foreach (var skill in skills)
            {
                if (skill == null) continue;

                if (cooldownRemaining.TryGetValue(skill, out var cd) && cd > 0f)
                    cooldownRemaining[skill] = Mathf.Max(0f, cd - Time.deltaTime);

                if (buffRemaining.TryGetValue(skill, out var buff) && buff > 0f)
                    buffRemaining[skill] = Mathf.Max(0f, buff - Time.deltaTime);
            }
        }

        public int GetLevel(SkillData skill) => skill != null && levels.TryGetValue(skill, out var lvl) ? lvl : 0;

        /// <summary>Nivel 0 = todavia no comprada (no equipada, no usable). La primera
        /// compra (nivel 1) la deja en su multiplicador base; desde ahi cada mejora
        /// adicional lo escala igual que el DPS de los heroes.</summary>
        public bool IsOwned(SkillData skill) => GetLevel(skill) > 0;

        public double GetMultiplier(SkillData skill)
        {
            if (skill == null) return 0d;
            int level = GetLevel(skill);
            return level <= 0 ? 0d : skill.damageMultiplier * System.Math.Pow(skill.multiplierGrowthPerLevel, level - 1);
        }

        public double GetUpgradeCost(SkillData skill) =>
            skill == null ? 0d : skill.baseUpgradeCost * System.Math.Pow(skill.upgradeCostGrowth, GetLevel(skill));

        public float GetCooldownRemaining(SkillData skill) =>
            skill != null && cooldownRemaining.TryGetValue(skill, out var r) ? r : 0f;

        public float GetCooldownDuration(SkillData skill)
        {
            if (skill == null) return 0f;
            double prestige = PrestigeManager.Instance != null ? PrestigeManager.Instance.CooldownMultiplier : 1d;
            double equipment = EquipmentManager.Instance != null ? EquipmentManager.Instance.CooldownMultiplier : 1d;
            return skill.cooldownSeconds * (float)(prestige * equipment);
        }

        public bool IsBuffActive(SkillData skill) => skill != null && buffRemaining.TryGetValue(skill, out var r) && r > 0f;
        public float GetBuffRemaining(SkillData skill) => skill != null && buffRemaining.TryGetValue(skill, out var r) ? r : 0f;

        /// <summary>Multiplicador combinado de todos los buffs TimedTapBuff activos ahora mismo.</summary>
        public double TapBuffMultiplier
        {
            get
            {
                double mult = 1d;
                foreach (var skill in skills)
                    if (skill != null && skill.kind == SkillKind.TimedTapBuff && IsBuffActive(skill))
                        mult *= GetMultiplier(skill);
                return mult;
            }
        }

        public double GoldBuffMultiplier
        {
            get
            {
                double mult = 1d;
                foreach (var skill in skills)
                    if (skill != null && skill.kind == SkillKind.TimedGoldBuff && IsBuffActive(skill))
                        mult *= GetMultiplier(skill);
                return mult;
            }
        }

        public List<SkillSaveEntry> GetSaveEntries()
        {
            var entries = new List<SkillSaveEntry>();
            foreach (var skill in skills)
            {
                if (skill == null) continue;
                int level = GetLevel(skill);
                if (level > 0) entries.Add(new SkillSaveEntry { skillId = skill.id, level = level });
            }
            return entries;
        }

        /// <summary>Restaura niveles desde un guardado, emparejando por SkillData.id.</summary>
        public void LoadState(List<SkillSaveEntry> savedSkills)
        {
            if (savedSkills == null) return;
            foreach (var entry in savedSkills)
            {
                var skill = System.Array.Find(skills, s => s != null && s.id == entry.skillId);
                if (skill == null) continue;

                levels[skill] = entry.level;
                EventBus.Publish(new SkillChangedEvent { SkillId = skill.id });
            }
        }

        public bool TryUpgrade(SkillData skill)
        {
            if (skill == null) return false;
            double cost = GetUpgradeCost(skill);
            if (GoldWallet.Instance == null || !GoldWallet.Instance.TrySpend(cost)) return false;

            levels[skill] = GetLevel(skill) + 1;
            EventBus.Publish(new SkillChangedEvent { SkillId = skill.id });
            return true;
        }

        /// <summary>Dispara la habilidad si esta comprada y no esta en cooldown. Devuelve si se activo.</summary>
        public bool TryActivate(SkillData skill)
        {
            if (skill == null || !IsOwned(skill)) return false;
            if (GetCooldownRemaining(skill) > 0f) return false;

            switch (skill.kind)
            {
                case SkillKind.InstantTapBurst:
                    if (!DealInstantDamage(TapBurstDamage(skill))) return false;
                    break;

                case SkillKind.InstantHeroBurst:
                    if (!DealInstantDamage(HeroBurstDamage(skill))) return false;
                    break;

                case SkillKind.TimedTapBuff:
                case SkillKind.TimedGoldBuff:
                    buffRemaining[skill] = skill.buffDurationSeconds;
                    break;
            }

            cooldownRemaining[skill] = GetCooldownDuration(skill);
            EventBus.Publish(new SkillActivatedEvent { SkillId = skill.id });
            return true;
        }

        private double TapBurstDamage(SkillData skill)
        {
            if (PlayerStats.Instance == null) return 0d;
            double equipmentMultiplier = EquipmentManager.Instance != null ? EquipmentManager.Instance.SkillDamageMultiplier : 1d;
            return PlayerStats.Instance.TapDamage * GetMultiplier(skill) * equipmentMultiplier;
        }

        private double HeroBurstDamage(SkillData skill)
        {
            if (HeroRoster.Instance == null) return 0d;
            double equipmentMultiplier = EquipmentManager.Instance != null ? EquipmentManager.Instance.SkillDamageMultiplier : 1d;
            return HeroRoster.Instance.TotalDps * GetMultiplier(skill) * equipmentMultiplier;
        }

        private static bool DealInstantDamage(double amount)
        {
            if (amount <= 0d) return false;
            var enemy = EnemySpawner.Instance != null ? EnemySpawner.Instance.CurrentEnemyInstance : null;
            if (enemy == null) return false;

            enemy.TakeDamage(amount);
            return true;
        }
    }
}
