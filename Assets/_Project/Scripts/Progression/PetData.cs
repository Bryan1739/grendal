using TapLegends.Core;
using UnityEngine;

namespace TapLegends.Progression
{
    public enum PetEffect { Gold, TapDamage, HeroDps }

    /// <summary>
    /// Mascota coleccionable. Solo una esta ACTIVA a la vez (PetManager) — las demas dan
    /// el 20% de su bonus, para incentivar coleccionarlas igual (ver TapLegends-Plan.md,
    /// Etapa 7). El ataque propio es opcional: attackDps=0 significa que no ataca sola.
    /// </summary>
    [CreateAssetMenu(fileName = "PetData", menuName = "Tap Legends/Pet")]
    public class PetData : ScriptableObject
    {
        public string id;
        public string displayName;
        public PetEffect effect;

        [Header("Bonus(nivel) = baseBonusPercent * growth^(nivel-1). Ej. 0.05 = +5%")]
        public double baseBonusPercent = 0.05d;
        public double bonusGrowthPerLevel = 1.10d;

        [Header("Ataque propio (opcional) — 0 de DPS significa que no ataca sola")]
        public double attackDps = 0d;
        public float attackInterval = 3f;

        // Publico (no [SerializeField] private) a proposito: CreateInitialData.cs (modo
        // Editor) necesita asignarlo directo al crear el asset — mismo criterio que
        // HeroData/WeaponSkinData.
        public Sprite art;
        public Sprite Art => art != null ? art : PlaceholderArt.Creature(SkinnedId, 28, CreatureRole.Pet);
        public Sprite attackArt;
        public Sprite AttackArt => attackArt != null ? attackArt : Art;

        // Skin elegida en el Perfil (ver ProfileManager) — mismo criterio que HeroData.
        private string SkinnedId => ProfileManager.Instance != null ? ProfileManager.Instance.ApplySkin(id) : id;
    }
}
