using TapLegends.Core;
using UnityEngine;

namespace TapLegends.Progression
{
    /// <summary>
    /// InstantTapBurst/InstantHeroBurst: golpe unico que multiplica el daño de tap o el
    /// DPS total de heroes. TimedTapBuff/TimedGoldBuff: multiplicador temporal (buffDurationSeconds)
    /// sobre el daño de tap o el oro ganado — no pegan de una, duran un rato.
    /// </summary>
    public enum SkillKind { InstantTapBurst, InstantHeroBurst, TimedTapBuff, TimedGoldBuff }

    /// <summary>
    /// Habilidad activa. Nivel 0 = todavia no comprada (ver SkillManager.IsOwned) — la
    /// primera mejora la deja usable en su casillero de la barra de habilidades.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillData", menuName = "Tap Legends/Skill")]
    public class SkillData : ScriptableObject
    {
        public string id;
        public string displayName;
        public SkillKind kind = SkillKind.InstantTapBurst;

        [Header("Multiplicador(nivel) = damageMultiplier * multiplierGrowth^(nivel-1)")]
        public double damageMultiplier = 10d;
        public double multiplierGrowthPerLevel = 1.15d;

        [Header("Solo para TimedTapBuff/TimedGoldBuff — cuanto dura el multiplicador activo")]
        public float buffDurationSeconds = 10f;

        [Header("Cooldown y coste de mejora (Nv. 0 -> 1, 1 -> 2...)")]
        public float cooldownSeconds = 8f;
        public double baseUpgradeCost = 50d;
        public double upgradeCostGrowth = 1.15d;

        [Header("Icono placeholder (ver PlaceholderArt.Weapon)")]
        public WeaponKind iconWeapon = WeaponKind.Sword;
        public Color iconColor = new Color(1f, 0.55f, 0.2f);

        [SerializeField] private Sprite icon;
        public Sprite Icon => icon != null ? icon : PlaceholderArt.Weapon(id, iconWeapon, iconColor, 32);
    }
}
