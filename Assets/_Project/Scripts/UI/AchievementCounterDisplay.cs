using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>Contador chico "Logros: X/Y" — visible siempre, sin abrir el modal.</summary>
    public class AchievementCounterDisplay : MonoBehaviour
    {
        [SerializeField] private Text counterText;

        private void OnEnable()
        {
            EventBus.Subscribe<AchievementUnlockedEvent>(HandleUnlocked);
            Refresh();
        }

        private void OnDisable() => EventBus.Unsubscribe<AchievementUnlockedEvent>(HandleUnlocked);

        private void HandleUnlocked(AchievementUnlockedEvent evt) => Refresh();

        private void Refresh()
        {
            if (counterText == null || AchievementManager.Instance == null) return;
            counterText.text = $"Logros: {AchievementManager.Instance.UnlockedCount}/{AchievementManager.Instance.TotalCount}";
        }
    }
}
