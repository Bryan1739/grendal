using TapLegends.Combat;
using TapLegends.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>
    /// Aparece cuando el jefe de la oleada esta disponible, no hay pelea en curso, Y ya se
    /// perdio ESTE jefe alguna vez (evt.HasLostBossOnce) — antes de eso, y en cada ciclo de
    /// 10 niveles nuevo tras vencer al anterior, el jefe arranca solo, sin boton (ver
    /// EnemySpawner). Visibilidad por CanvasGroup, no SetActive: si se desactiva el propio
    /// GameObject, su OnEnable (donde se suscribe al evento) no vuelve a correr para
    /// reactivarse solo.
    /// </summary>
    [RequireComponent(typeof(Button), typeof(CanvasGroup))]
    public class ChallengeBossButton : MonoBehaviour
    {
        private Button button;
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            button = GetComponent<Button>();
            canvasGroup = GetComponent<CanvasGroup>();
            button.onClick.AddListener(HandleClick);
            SetVisible(false);
        }

        private void OnEnable() => EventBus.Subscribe<StageStatusEvent>(HandleStageStatus);
        private void OnDisable() => EventBus.Unsubscribe<StageStatusEvent>(HandleStageStatus);

        private void HandleStageStatus(StageStatusEvent evt) =>
            SetVisible(evt.BossAvailable && !evt.BossActive && evt.HasLostBossOnce);

        private void HandleClick() => EnemySpawner.Instance?.ChallengeBoss();

        private void SetVisible(bool visible)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }
}
