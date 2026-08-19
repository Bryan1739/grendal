using System.Collections.Generic;
using TapLegends.Core;
using UnityEngine;

namespace TapLegends.Progression
{
    /// <summary>Cual skin de espada tiene elegida el jugador ahora. Solo el indice se
    /// guarda (via id, no referencia directa — mismo criterio que el resto del guardado).</summary>
    public class WeaponSkinManager : MonoBehaviour
    {
        public static WeaponSkinManager Instance { get; private set; }

        [SerializeField] private WeaponSkinData[] skins = System.Array.Empty<WeaponSkinData>();

        private int selectedIndex;

        public IReadOnlyList<WeaponSkinData> Skins => skins;
        public WeaponSkinData Selected =>
            skins != null && skins.Length > 0 ? skins[Mathf.Clamp(selectedIndex, 0, skins.Length - 1)] : null;

        public void Configure(WeaponSkinData[] roster) => skins = roster ?? System.Array.Empty<WeaponSkinData>();

        private void Awake() => Instance = this;

        public void Select(WeaponSkinData skin)
        {
            int idx = System.Array.IndexOf(skins, skin);
            if (idx < 0 || idx == selectedIndex) return;

            selectedIndex = idx;
            EventBus.Publish(new WeaponSkinChangedEvent());
        }

        public bool IsSelected(WeaponSkinData skin) => Selected == skin;

        public string GetSaveId() => Selected != null ? Selected.id : "";

        public void LoadState(string savedId)
        {
            int idx = System.Array.FindIndex(skins, s => s != null && s.id == savedId);
            selectedIndex = idx >= 0 ? idx : 0;
            EventBus.Publish(new WeaponSkinChangedEvent());
        }
    }
}
