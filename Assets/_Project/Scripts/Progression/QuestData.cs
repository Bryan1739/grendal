using UnityEngine;

namespace TapLegends.Progression
{
    public enum QuestObjective { KillEnemies, KillBosses, UseSkills, EarnGold }

    /// <summary>
    /// Plantilla de mision diaria. QuestManager elige 3 al azar del pool cada dia — esto
    /// es solo la "receta" (objetivo + cantidad + recompensa), no el progreso en si.
    /// </summary>
    [CreateAssetMenu(fileName = "QuestData", menuName = "Tap Legends/Quest")]
    public class QuestData : ScriptableObject
    {
        public string id;
        public string description;
        public QuestObjective objective;
        public double targetAmount;
        public double rewardGold;
    }
}
