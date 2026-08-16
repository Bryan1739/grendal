using UnityEngine;

namespace TapLegends.Progression
{
    public enum AchievementCategory { Combat, Wealth, Collection, Prestige }

    public enum AchievementObjective
    {
        KillEnemies, KillBosses, ReachLevel, EarnGoldLifetime,
        OwnHeroes, OwnEquipment, OwnPets, PrestigeCount, UpgradeTapLevel,
    }

    /// <summary>Logro permanente (no se reinicia, a diferencia de las misiones diarias).
    /// AchievementManager lo desbloquea solo cuando el contador correspondiente llega
    /// a targetAmount — no hay "reclamar", la recompensa se da al instante.</summary>
    [CreateAssetMenu(fileName = "AchievementData", menuName = "Tap Legends/Achievement")]
    public class AchievementData : ScriptableObject
    {
        public string id;
        public string displayName;
        public AchievementCategory category;
        public AchievementObjective objective;
        public double targetAmount;
        public double rewardGold;
    }
}
