using UnityEngine;

namespace TapLegends.Progression
{
    public enum PrestigeEffect { GlobalDamage, Gold, HeroDps, SkillCooldownReduction }

    /// <summary>Mejora permanente comprada con reliquias — sobrevive a los Renaceres
    /// siguientes, a diferencia del oro/heroes/nivel (ver PrestigeManager.TryPrestige).</summary>
    [CreateAssetMenu(fileName = "PrestigeUpgradeData", menuName = "Tap Legends/Prestige Upgrade")]
    public class PrestigeUpgradeData : ScriptableObject
    {
        public string id;
        public string displayName;
        public PrestigeEffect effect;

        [Tooltip("Ej. 0.05 = +5% por nivel (o -5% de cooldown para SkillCooldownReduction)")]
        public double valuePerLevel = 0.05d;

        public double baseCost = 1d;
        public double costGrowth = 1.15d;
    }
}
