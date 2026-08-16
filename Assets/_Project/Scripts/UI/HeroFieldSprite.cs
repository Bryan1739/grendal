using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;

namespace TapLegends.UI
{
    /// <summary>
    /// Sprite del heroe alrededor del monstruo (igual criterio que PlayerCharacter): invisible
    /// hasta que el heroe tiene nivel 1+, y reacciona tanto a compras como a partidas cargadas.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class HeroFieldSprite : MonoBehaviour
    {
        [SerializeField] private HeroData hero;

        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<HeroChangedEvent>(HandleHeroChanged);
            Refresh();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<HeroChangedEvent>(HandleHeroChanged);
        }

        private void HandleHeroChanged(HeroChangedEvent evt)
        {
            if (hero != null && evt.HeroId == hero.id) Refresh();
        }

        private void Refresh()
        {
            if (hero == null || canvasGroup == null || HeroRoster.Instance == null) return;
            canvasGroup.alpha = HeroRoster.Instance.GetLevel(hero) > 0 ? 1f : 0f;
        }
    }
}
