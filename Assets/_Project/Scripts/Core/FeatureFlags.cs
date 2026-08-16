using UnityEngine;

namespace TapLegends.Core
{
    /// <summary>
    /// Interruptores de sistemas a medio construir. Permite que main compile y sea
    /// jugable aunque un sistema (mascotas, habilidades...) este a medias.
    /// Un solo asset vive en Assets/_Project/Settings/FeatureFlags.asset.
    /// </summary>
    [CreateAssetMenu(fileName = "FeatureFlags", menuName = "Tap Legends/Feature Flags")]
    public class FeatureFlags : ScriptableObject
    {
        [Header("Etapa 6+")]
        public bool equipmentEnabled = false;

        [Header("Etapa 7")]
        public bool petsEnabled = false;

        [Header("Etapa 8")]
        public bool skillsEnabled = false;

        [Header("Etapa 9")]
        public bool questsEnabled = false;
        public bool achievementsEnabled = false;

        [Header("Etapa 4")]
        public bool saveSystemEnabled = false;

        [Header("Debug")]
        public bool debugMenuEnabled = true;
    }
}
