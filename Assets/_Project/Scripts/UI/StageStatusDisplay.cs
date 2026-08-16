using System.Collections;
using TapLegends.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>
    /// Muestra "Nivel X — K/10" en oleada normal, el temporizador del jefe en rojo durante
    /// la pelea, y un mensaje de 2s cuando el jefe escapa (ver analisis L2: el fracaso
    /// tiene que ser informativo, no un reseteo silencioso que confunda al jugador).
    /// </summary>
    public class StageStatusDisplay : MonoBehaviour
    {
        [SerializeField] private Text statusText;

        private static readonly Color NormalColor = Color.white;
        private static readonly Color BossColor = new Color(1f, 0.35f, 0.35f);

        private Coroutine escapeMessageCoroutine;

        private void OnEnable()
        {
            EventBus.Subscribe<StageStatusEvent>(HandleStageStatus);
            EventBus.Subscribe<BossEscapedEvent>(HandleBossEscaped);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<StageStatusEvent>(HandleStageStatus);
            EventBus.Unsubscribe<BossEscapedEvent>(HandleBossEscaped);
        }

        private void HandleBossEscaped(BossEscapedEvent evt)
        {
            if (escapeMessageCoroutine != null) StopCoroutine(escapeMessageCoroutine);
            escapeMessageCoroutine = StartCoroutine(ShowEscapeMessage());
        }

        private IEnumerator ShowEscapeMessage()
        {
            if (statusText != null)
            {
                statusText.text = "¡El jefe escapó! Repitiendo oleada...";
                statusText.color = BossColor;
            }
            yield return new WaitForSeconds(2f);
            escapeMessageCoroutine = null;
        }

        private void HandleStageStatus(StageStatusEvent evt)
        {
            if (statusText == null || escapeMessageCoroutine != null) return; // no pisar el mensaje de escape

            if (evt.BossActive)
            {
                statusText.text = $"¡JEFE! {Mathf.CeilToInt(evt.BossSecondsRemaining)}s";
                statusText.color = BossColor;
            }
            else
            {
                statusText.text = $"Nivel {evt.Level} — {evt.KillsInStage}/{evt.EnemiesPerStage}";
                statusText.color = NormalColor;
            }
        }
    }
}
