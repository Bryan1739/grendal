using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>Casillero de skin de espada en la pestaña Skins del Perfil — mismo
    /// criterio visual que SkinSwatchButton, pero habla con WeaponSkinManager (arte real)
    /// en vez de ProfileManager (PlaceholderArt procedural).</summary>
    [RequireComponent(typeof(Button), typeof(Image))]
    public class WeaponSkinSwatchButton : MonoBehaviour
    {
        [SerializeField] private WeaponSkinData skin;

        private static readonly Color SelectedColor = new Color(0.85f, 0.70f, 0.20f);
        private static readonly Color UnselectedColor = new Color(0.16f, 0.16f, 0.20f);

        private Image background;

        private void Awake()
        {
            background = GetComponent<Image>();
            GetComponent<Button>().onClick.AddListener(HandleClick);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<WeaponSkinChangedEvent>(HandleSkinChanged);
            Refresh();
        }

        private void OnDisable() => EventBus.Unsubscribe<WeaponSkinChangedEvent>(HandleSkinChanged);

        private void HandleClick() => WeaponSkinManager.Instance?.Select(skin);

        private void HandleSkinChanged(WeaponSkinChangedEvent evt) => Refresh();

        private void Refresh()
        {
            if (background == null || WeaponSkinManager.Instance == null) return;
            background.color = WeaponSkinManager.Instance.IsSelected(skin) ? SelectedColor : UnselectedColor;
        }
    }
}
