using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>Contador chico "X/Y" — visible siempre, sin abrir el modal. El botón ya
    /// muestra el ícono real con "LOGROS" horneado adentro (ver SceneBuilder), así que este
    /// texto se redujo a solo el número — antes decía "Logros: X/Y", repetía la palabra.</summary>
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
            counterText.text = $"{AchievementManager.Instance.UnlockedCount}/{AchievementManager.Instance.TotalCount}";
        }
    }
}
