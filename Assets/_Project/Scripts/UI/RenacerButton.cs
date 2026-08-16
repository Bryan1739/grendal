using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    [RequireComponent(typeof(Button))]
    public class RenacerButton : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private PrestigeConfirmPanel confirmPanel;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OpenConfirm);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<StageStatusEvent>(HandleStageStatus);
            EventBus.Subscribe<PrestigeCompletedEvent>(HandlePrestigeCompleted);
            Refresh();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<StageStatusEvent>(HandleStageStatus);
            EventBus.Unsubscribe<PrestigeCompletedEvent>(HandlePrestigeCompleted);
        }

        private void HandleStageStatus(StageStatusEvent evt) => Refresh();
        private void HandlePrestigeCompleted(PrestigeCompletedEvent evt) => Refresh();

        private void OpenConfirm() => confirmPanel?.Open();

        private void Refresh()
        {
            if (label == null || PrestigeManager.Instance == null) return;

            bool canPrestige = PrestigeManager.Instance.CanPrestige;
            double relics = PrestigeManager.Instance.RelicsIfPrestigeNow;

            label.text = canPrestige
                ? $"Renacer\n(+{NumberFormatter.Format(relics)} reliquias)"
                : $"Renacer\n(nivel minimo {GameConstants.PrestigeMinLevel})";

            button.interactable = canPrestige;
        }
    }
}
