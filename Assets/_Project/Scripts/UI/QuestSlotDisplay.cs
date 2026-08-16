using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>Una de las 3 misiones diarias. Tocarla reclama la recompensa si ya esta completa.</summary>
    [RequireComponent(typeof(Button))]
    public class QuestSlotDisplay : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private int slotIndex;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(HandleClick);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<QuestsChangedEvent>(HandleChanged);
            Refresh();
        }

        private void OnDisable() => EventBus.Unsubscribe<QuestsChangedEvent>(HandleChanged);

        private void HandleChanged(QuestsChangedEvent evt) => Refresh();

        private void HandleClick() => QuestManager.Instance?.TryClaim(slotIndex);

        private void Refresh()
        {
            if (label == null || QuestManager.Instance == null) return;

            var quest = QuestManager.Instance.GetQuest(slotIndex);
            if (quest == null)
            {
                label.text = "...";
                button.interactable = false;
                return;
            }

            double progress = QuestManager.Instance.GetProgress(slotIndex);
            bool complete = QuestManager.Instance.IsComplete(slotIndex);
            bool claimed = QuestManager.Instance.IsClaimed(slotIndex);

            string status = claimed
                ? "Reclamada"
                : complete
                    ? "¡Completa! Tocar para reclamar"
                    : $"{NumberFormatter.Format(progress)}/{NumberFormatter.Format(quest.targetAmount)}";

            label.text = $"{quest.description}\n{status}   +{NumberFormatter.Format(quest.rewardGold)} oro";
            button.interactable = complete && !claimed;
        }
    }
}
