using TapLegends.Core;
using UnityEngine;

namespace TapLegends.Combat
{
    public enum EnemyTier { Common, Elite, Boss }

    /// <summary>
    /// Definicion de un "aspecto" de enemigo (variedad visual). Las estadisticas reales
    /// en combate salen de GameConstants segun el nivel de la etapa (mismo enemigo,
    /// distinto nivel, mas fuerte) — ver GameConstants.EnemyHealthForLevel.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Tap Legends/Enemy")]
    public class EnemyData : ScriptableObject
    {
        public string id;
        public string displayName;
        public EnemyTier tier = EnemyTier.Common;

        [Tooltip("Vacio hasta la Etapa 9: se usa el generador procedural mientras tanto.")]
        [SerializeField] private Sprite art;

        [Tooltip("Tamaño del sprite generado si 'art' esta vacio.")]
        public int placeholderSize = 32;

        public Sprite Art => art != null
            ? art
            : PlaceholderArt.Creature(id, placeholderSize, TierToRole(tier));

        private static CreatureRole TierToRole(EnemyTier tier) => tier switch
        {
            EnemyTier.Elite => CreatureRole.EliteEnemy,
            EnemyTier.Boss => CreatureRole.Boss,
            _ => CreatureRole.CommonEnemy,
        };
    }
}
