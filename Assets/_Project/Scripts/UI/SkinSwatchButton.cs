using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>Un casillero de skin dentro de una fila de la pestaña Skins del Perfil.
    /// El preview (que aspecto tiene esa skin) es una imagen hija fija, asignada una sola
    /// vez por SceneBuilder; este componente solo maneja el click y el resaltado de
    /// "elegida ahora".</summary>
    [RequireComponent(typeof(Button), typeof(Image))]
    public class SkinSwatchButton : MonoBehaviour
    {
        [SerializeField] private string characterId;
        [SerializeField] private int skinIndex;

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
            EventBus.Subscribe<SkinChangedEvent>(HandleSkinChanged);
            Refresh();
        }

        private void OnDisable() => EventBus.Unsubscribe<SkinChangedEvent>(HandleSkinChanged);

        private void HandleClick() => ProfileManager.Instance?.SetSkin(characterId, skinIndex);

        private void HandleSkinChanged(SkinChangedEvent evt)
        {
            if (string.IsNullOrEmpty(evt.CharacterId) || evt.CharacterId == characterId) Refresh();
        }

        private void Refresh()
        {
            if (background == null || ProfileManager.Instance == null) return;
            background.color = ProfileManager.Instance.GetSkin(characterId) == skinIndex ? SelectedColor : UnselectedColor;
        }
    }
}
