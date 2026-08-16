using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    public class RelicsDisplay : MonoBehaviour
    {
        [SerializeField] private Text relicsText;

        private void OnEnable()
        {
            EventBus.Subscribe<RelicsChangedEvent>(HandleRelicsChanged);
            Refresh();
        }

        private void OnDisable() => EventBus.Unsubscribe<RelicsChangedEvent>(HandleRelicsChanged);

        private void HandleRelicsChanged(RelicsChangedEvent evt)
        {
            if (relicsText != null) relicsText.text = $"Reliquias: {NumberFormatter.Format(evt.NewTotal)}";
        }

        private void Refresh()
        {
            if (relicsText != null && PrestigeManager.Instance != null)
                relicsText.text = $"Reliquias: {NumberFormatter.Format(PrestigeManager.Instance.Relics)}";
        }
    }
}
