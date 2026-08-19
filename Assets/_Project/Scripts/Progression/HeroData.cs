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

        // Publico (no [SerializeField] private) a proposito: CreateInitialData.cs (modo
        // Editor) necesita asignarlo directo al crear el asset, igual que WeaponSkinData —
        // ver ese archivo para el mismo criterio.
        public Sprite art;
        public Sprite Art => art != null ? art : PlaceholderArt.Creature(SkinnedId, 32, CreatureRole.Hero);

        // Pose de ataque (opcional): si esta asignada, HeroFieldSprite alterna Art/AttackArt
        // en loop mientras el heroe este comprado — si no, se queda quieto en Art (mismo
        // criterio de siempre: la animacion es un extra, no un requisito).
        public Sprite attackArt;
        public Sprite AttackArt => attackArt != null ? attackArt : Art;

        // Skin elegida en el Perfil (ver ProfileManager) — null en modo Editor (fuera de
        // Play) o antes de que ProfileManager.Awake corra, asi que cae de vuelta al id
        // sin skin (aspecto "de siempre") en esos casos.
        private string SkinnedId => ProfileManager.Instance != null ? ProfileManager.Instance.ApplySkin(id) : id;

        [Header("Arma (placeholder visual para diferenciar heroes)")]
        public WeaponKind weaponKind = WeaponKind.Sword;
        public Color weaponTint = Color.white;

        [SerializeField] private Sprite weaponIconOverride;
        public Sprite WeaponIcon => weaponIconOverride != null
            ? weaponIconOverride
            : PlaceholderArt.Weapon(id, weaponKind, weaponTint, 24);
    }
}
