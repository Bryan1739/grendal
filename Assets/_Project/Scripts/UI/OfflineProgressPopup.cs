using TapLegends.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>"Mientras no estabas ganaste X oro" — se muestra al cargar si hubo progreso offline.</summary>
    public class OfflineProgressPopup : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text messageText;
        [SerializeField] private Button closeButton;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            // El onClick se conecta aca (Awake), no desde el script de Editor que arma la
            // escena: ver la nota en DebugMenuController sobre por que.
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        private void OnEnable() => EventBus.Subscribe<OfflineProgressEvent>(HandleOfflineProgress);
        private void OnDisable() => EventBus.Unsubscribe<OfflineProgressEvent>(HandleOfflineProgress);

        private void HandleOfflineProgress(OfflineProgressEvent evt)
        {
            if (panel == null || messageText == null) return;

            int totalMinutes = Mathf.FloorToInt(evt.SecondsAway / 60f);
            string timeText = totalMinutes < 60
                ? $"{totalMinutes} min"
                : $"{totalMinutes / 60}h {totalMinutes % 60}min";

            messageText.text = $"Mientras no estabas ganaste\n{NumberFormatter.Format(evt.GoldEarned)} de oro\n({timeText})";
            panel.SetActive(true);
        }

        private void Close()
        {
            if (panel != null) panel.SetActive(false);
        }
    }
}
