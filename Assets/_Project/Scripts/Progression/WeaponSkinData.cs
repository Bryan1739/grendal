using UnityEngine;

namespace TapLegends.Progression
{
    /// <summary>
    /// Una skin de espada del personaje principal: icono (para el selector) + una
    /// animacion completa propia (Idle en loop, 3 ataques que se van alternando al tocar).
    /// A diferencia de las skins de heroes/mascotas (PlaceholderArt con sufijo, ver
    /// ProfileManager), esta es arte final real provisto por el usuario desde el dia 1 —
    /// no hay fallback procedural: sin sprites asignados, PlayerAnimator simplemente no
    /// anima nada.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponSkinData", menuName = "Tap Legends/Weapon Skin")]
    public class WeaponSkinData : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite icon;

        public Sprite[] idleFrames;
        public Sprite[] attack1Frames;
        public Sprite[] attack2Frames;
        public Sprite[] attack3Frames;
    }
}
