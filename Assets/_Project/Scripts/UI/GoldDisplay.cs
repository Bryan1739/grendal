using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    public class GoldDisplay : MonoBehaviour
    {
        [SerializeField] private Text goldText;

        private void OnEnable()
        {
            EventBus.Subscribe<GoldChangedEvent>(HandleGoldChanged);
            Refresh();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GoldChangedEvent>(HandleGoldChanged);
        }

        private void HandleGoldChanged(GoldChangedEvent evt)
        {
            if (goldText != null) goldText.text = $"Oro: {NumberFormatter.Format(evt.NewTotal)}";
        }

        private void Refresh()
        {
            if (goldText != null && GoldWallet.Instance != null)
                goldText.text = $"Oro: {NumberFormatter.Format(GoldWallet.Instance.Gold)}";
        }
    }
}
