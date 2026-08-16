using TapLegends.Core;
using UnityEngine;

namespace TapLegends.Progression
{
    /// <summary>Definicion de un heroe de DPS pasivo. El nivel de cada uno vive en HeroRoster, no aca.</summary>
    [CreateAssetMenu(fileName = "HeroData", menuName = "Tap Legends/Hero")]
    public class HeroData : ScriptableObject
    {
        public string id;
        public string displayName;

        [Header("Coste (Nv. 0 -> 1, 1 -> 2...)")]
        public double baseCost = 20d;
        public double costGrowth = 1.12d;

        [Header("DPS(nivel) = baseDps * dpsGrowthMultiplier^(nivel-1). Nv.0 = sin comprar = 0 DPS")]
        public double baseDps = 1d;
        public double dpsGrowthMultiplier = 1.15d;

        [SerializeField] private Sprite art;
        public Sprite Art => art != null ? art : PlaceholderArt.Creature(id, 32, CreatureRole.Hero);

        [Header("Arma (placeholder visual para diferenciar heroes)")]
        public WeaponKind weaponKind = WeaponKind.Sword;
        public Color weaponTint = Color.white;

        [SerializeField] private Sprite weaponIconOverride;
        public Sprite WeaponIcon => weaponIconOverride != null
            ? weaponIconOverride
            : PlaceholderArt.Weapon(id, weaponKind, weaponTint, 24);
    }
}
