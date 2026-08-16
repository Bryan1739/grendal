using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>Mascota activa flotando junto al personaje — "sprite animado flotando junto
    /// al heroe" (ver TapLegends-Plan.md, Etapa 7). La animacion es un simple vaiven en Y.</summary>
    public class PetFieldSprite : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private float floatAmplitude = 8f;
        [SerializeField] private float floatSpeed = 2f;

        private RectTransform rect;
        private Vector2 basePosition;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            basePosition = rect.anchoredPosition;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PetChangedEvent>(HandleChanged);
            Refresh();
        }

        private void OnDisable() => EventBus.Unsubscribe<PetChangedEvent>(HandleChanged);

        private void HandleChanged(PetChangedEvent evt) => Refresh();

        private void Refresh()
        {
            if (icon == null) return;

            var active = PetManager.Instance != null ? PetManager.Instance.ActivePet : null;
            icon.enabled = active != null;
            if (active != null) icon.sprite = active.Art;
        }

        private void Update()
        {
            if (rect == null) return;
            float y = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            rect.anchoredPosition = basePosition + new Vector2(0f, y);
        }
    }
}
