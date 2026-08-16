using UnityEngine;

namespace TapLegends.Progression
{
    public enum EquipmentSlot { Weapon, Helmet, Armor, Boots, Amulet }

    /// <summary>Nombres iguales a las claves de PlaceholderArt.RarityColor (Core) — asi el
    /// color de rareza se reutiliza por ToString() sin duplicar la paleta.</summary>
    public enum EquipmentRarity { Common, Uncommon, Rare, Epic, Legendary }

    public enum EquipmentEffect { TapDamage, SkillDamage, Gold, HeroDps, SkillCooldown }

    /// <summary>
    /// Una pieza por slot (5 en total): la RAREZA que tenes equipada en ese slot es lo que
    /// sube con el botin, no el objeto en si — igual que Tap Titans/Tap Souls, no hay que
    /// generar decenas de assets por rareza. Ver EquipmentManager para el flujo completo.
    /// </summary>
    [CreateAssetMenu(fileName = "EquipmentData", menuName = "Tap Legends/Equipment")]
    public class EquipmentData : ScriptableObject
    {
        public string id;
        public string displayName;
        public EquipmentSlot slot;
        public EquipmentEffect effect;

        [Tooltip("Bonus POR NIVEL dentro de la rareza actual. Ej. 0.03 = +3% por nivel.")]
        public double baseValuePerLevel = 0.03d;

        [Tooltip("Fragmentos que cuesta cada mejora de nivel, antes de aplicar el crecimiento.")]
        public double baseFragmentCost = 3d;
        public double fragmentCostGrowth = 1.12d;
    }
}
