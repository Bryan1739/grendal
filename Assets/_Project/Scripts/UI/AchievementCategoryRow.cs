using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>
    /// Muestra el logro sin desbloquear mas cercano de una categoria — con 30 logros no
    /// entran todos en pantalla sin un ScrollRect (que todavia no existe en el proyecto),
    /// asi que esta es la vista resumida: "a que le falta menos en cada categoria".
    /// Los logros se desbloquean solos, esta fila es solo informativa (no tiene boton).
    /// </summary>
    public class AchievementCategoryRow : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private AchievementCategory category;

        private void OnEnable()
        {
            EventBus.Subscribe<AchievementUnlockedEvent>(HandleUnlocked);
            Refresh();
        }

        private void OnDisable() => EventBus.Unsubscribe<AchievementUnlockedEvent>(HandleUnlocked);

        private void HandleUnlocked(AchievementUnlockedEvent evt) => Refresh();

        private void Refresh()
        {
            if (label == null || AchievementManager.Instance == null) return;

            string categoryName = category switch
            {
                AchievementCategory.Combat => "Combate",
                AchievementCategory.Wealth => "Riqueza",
                AchievementCategory.Collection => "Colección",
                AchievementCategory.Prestige => "Prestigio",
                _ => "",
            };

            var next = AchievementManager.Instance.GetNextLocked(category);
            if (next == null)
            {
                label.text = $"{categoryName}\n¡Todos desbloqueados!";
                return;
            }

            double progress = AchievementManager.Instance.GetProgress(next.objective);
            label.text = $"{categoryName}: {next.displayName}\n{NumberFormatter.Format(progress)}/{NumberFormatter.Format(next.targetAmount)}";
        }
    }
}
