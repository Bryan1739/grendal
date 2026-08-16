using System.Collections;
using TapLegends.Combat;
using TapLegends.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>Cada vez que se vence a un jefe, el fondo cambia de paleta y silueta
    /// (siempre paisaje: cielo + montañas + suelo, ver PlaceholderArt.MountainLandscape).</summary>
    public class BackgroundController : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private float fadeDuration = 0.6f;

        private int chapter = 0;

        private void OnEnable() => EventBus.Subscribe<EnemyKilledEvent>(HandleEnemyKilled);
        private void OnDisable() => EventBus.Unsubscribe<EnemyKilledEvent>(HandleEnemyKilled);

        private void HandleEnemyKilled(EnemyKilledEvent evt)
        {
            if (!evt.WasBoss) return;
            chapter++;
            StopAllCoroutines();
            StartCoroutine(SwapBackground());
        }

        private IEnumerator SwapBackground()
        {
            if (backgroundImage == null) yield break;

            var newSprite = PlaceholderArt.MountainLandscape($"paisaje_{chapter}");
            var visible = backgroundImage.color;
            var hidden = visible;
            hidden.a = 0f;

            yield return Fade(visible, hidden);
            backgroundImage.sprite = newSprite;
            yield return Fade(hidden, visible);
        }

        private IEnumerator Fade(Color from, Color to)
        {
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                backgroundImage.color = Color.Lerp(from, to, t / fadeDuration);
                yield return null;
            }
            backgroundImage.color = to;
        }
    }
}
