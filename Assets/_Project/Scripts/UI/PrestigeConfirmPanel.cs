using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>Modal de confirmacion del Renacer — es una accion que reinicia progreso,
    /// asi que no dispara directo desde el boton (mismo criterio que las acciones
    /// destructivas en general: confirmar antes de ejecutar).</summary>
    public class PrestigeConfirmPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (confirmButton != null) confirmButton.onClick.AddListener(HandleConfirm);
            if (cancelButton != null) cancelButton.onClick.AddListener(Close);
        }

        public void Open()
        {
            if (panel == null || PrestigeManager.Instance == null) return;

            double relics = PrestigeManager.Instance.RelicsIfPrestigeNow;
            if (messageText != null)
                messageText.text = $"¿Renacer ahora?\n\nGanas {NumberFormatter.Format(relics)} reliquias.\n" +
                                    "Se reinician oro, daño de tap, heroes y nivel.\n" +
                                    "Las reliquias y mejoras permanentes NO se pierden.";

            panel.SetActive(true);
        }

        private void HandleConfirm()
        {
            PrestigeManager.Instance?.TryPrestige();
            Close();
        }

        private void Close()
        {
            if (panel != null) panel.SetActive(false);
        }
    }
}
