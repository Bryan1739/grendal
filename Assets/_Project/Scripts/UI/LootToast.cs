using System.Collections;
using TapLegends.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>Aviso temporal cuando un jefe suelta botin — sin esto, un drop de equipo
    /// pasaria completamente desapercibido (el jugador ni se enteraria de que paso algo).</summary>
    public class LootToast : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text messageText;
        [SerializeField] private float showSeconds = 2.5f;

        private Coroutine routine;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<LootObtainedEvent>(HandleLoot);
            EventBus.Subscribe<PetObtainedEvent>(HandlePetObtained);
            EventBus.Subscribe<AchievementUnlockedEvent>(HandleAchievementUnlocked);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<LootObtainedEvent>(HandleLoot);
            EventBus.Unsubscribe<PetObtainedEvent>(HandlePetObtained);
            EventBus.Unsubscribe<AchievementUnlockedEvent>(HandleAchievementUnlocked);
        }

        private void HandleLoot(LootObtainedEvent evt)
        {
            if (panel == null || messageText == null) return;

            messageText.text = evt.WasUpgrade
                ? $"¡Nuevo equipo!\n{evt.DisplayName} ({evt.Rarity})"
                : $"+{evt.FragmentsGained} fragmentos de {evt.DisplayName}";

            Show();
        }

        private void HandlePetObtained(PetObtainedEvent evt)
        {
            if (panel == null || messageText == null) return;

            messageText.text = evt.WasNew
                ? $"¡Nueva mascota!\n{evt.DisplayName}"
                : $"+{evt.FragmentsGained} fragmentos de {evt.DisplayName}";

            Show();
        }

        private void HandleAchievementUnlocked(AchievementUnlockedEvent evt)
        {
            if (panel == null || messageText == null) return;

            messageText.text = $"¡Logro desbloqueado!\n{evt.DisplayName}\n+{NumberFormatter.Format(evt.RewardGold)} oro";
            Show();
        }

        private void Show()
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(ShowThenHide());
        }

        private IEnumerator ShowThenHide()
        {
            panel.SetActive(true);
            yield return new WaitForSeconds(showSeconds);
            panel.SetActive(false);
            routine = null;
        }
    }
}
