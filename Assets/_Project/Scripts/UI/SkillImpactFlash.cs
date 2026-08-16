using System.Collections;
using TapLegends.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>
    /// "Sacudida de pantalla" al activar una habilidad (ver TapLegends-Plan.md, Etapa 8) —
    /// implementada como flash de pantalla completa, no shake de camara: el Canvas es
    /// Screen Space Overlay, que se dibuja independiente de la camara, asi que mover la
    /// camara no cambiaria nada visible. Un flash breve da la misma sensacion de impacto.
    /// </summary>
    public class SkillImpactFlash : MonoBehaviour
    {
        [SerializeField] private Image flashImage;
        [SerializeField] private float flashDuration = 0.18f;
        [SerializeField] private float peakAlpha = 0.35f;

        private Coroutine routine;

        private void OnEnable() => EventBus.Subscribe<SkillActivatedEvent>(HandleActivated);
        private void OnDisable() => EventBus.Unsubscribe<SkillActivatedEvent>(HandleActivated);

        private void HandleActivated(SkillActivatedEvent evt)
        {
            if (flashImage == null) return;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Flash());
        }

        private IEnumerator Flash()
        {
            var color = flashImage.color;
            color.a = peakAlpha;
            flashImage.color = color;

            float t = 0f;
            while (t < flashDuration)
            {
                t += Time.deltaTime;
                color.a = Mathf.Lerp(peakAlpha, 0f, t / flashDuration);
                flashImage.color = color;
                yield return null;
            }

            color.a = 0f;
            flashImage.color = color;
            routine = null;
        }
    }
}
