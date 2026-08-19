using TapLegends.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>
    /// Menu de pausa: boton dedicado (esquina superior izquierda) Y boton de retroceso
    /// de Android, que en Unity con el Input Manager viejo llega como
    /// Input.GetKeyDown(KeyCode.Escape) — tambien es la tecla Esc en el Editor, asi que
    /// esto se prueba igual sin dispositivo. Antes de este script, el boton de retroceso
    /// cerraba la app directamente (comportamiento por defecto de Android); ahora
    /// siempre abre/cierra un panel en vez de salir (ver TapLegends-Plan.md, Etapa 10).
    ///
    /// Mientras cualquier panel de este menu esta abierto, Time.timeScale = 0: pausa de
    /// verdad el juego (temporizador de jefe incluido), no es solo un overlay decorativo.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private Button menuButton;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button openSettingsButton;
        [SerializeField] private Button quitButton;

        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button settingsBackButton;
        [SerializeField] private Button vibrationToggleButton;
        [SerializeField] private Text vibrationToggleLabel;
        [SerializeField] private Button openCreditsButton;
        [SerializeField] private Button deleteSaveButton;

        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private Button creditsBackButton;

        [SerializeField] private GameObject deleteConfirmPanel;
        [SerializeField] private Button deleteConfirmYesButton;
        [SerializeField] private Button deleteConfirmNoButton;

        private void Awake()
        {
            if (menuButton != null) menuButton.onClick.AddListener(OpenPause);
            if (resumeButton != null) resumeButton.onClick.AddListener(ClosePause);
            if (openSettingsButton != null) openSettingsButton.onClick.AddListener(OpenSettings);
            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
            if (settingsBackButton != null) settingsBackButton.onClick.AddListener(CloseSettings);
            if (vibrationToggleButton != null) vibrationToggleButton.onClick.AddListener(ToggleVibration);
            if (openCreditsButton != null) openCreditsButton.onClick.AddListener(OpenCredits);
            if (creditsBackButton != null) creditsBackButton.onClick.AddListener(CloseCredits);
            if (deleteSaveButton != null) deleteSaveButton.onClick.AddListener(OpenDeleteConfirm);
            if (deleteConfirmYesButton != null) deleteConfirmYesButton.onClick.AddListener(ConfirmDeleteSave);
            if (deleteConfirmNoButton != null) deleteConfirmNoButton.onClick.AddListener(CloseDeleteConfirm);

            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (deleteConfirmPanel != null) deleteConfirmPanel.SetActive(false);

            RefreshVibrationLabel();
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            if (deleteConfirmPanel != null && deleteConfirmPanel.activeSelf) CloseDeleteConfirm();
            else if (creditsPanel != null && creditsPanel.activeSelf) CloseCredits();
            else if (settingsPanel != null && settingsPanel.activeSelf) CloseSettings();
            else if (pausePanel != null && pausePanel.activeSelf) ClosePause();
            else OpenPause();
        }

        public void OpenPause()
        {
            if (pausePanel != null) pausePanel.SetActive(true);
            RefreshTimeScale();
        }

        private void ClosePause()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            RefreshTimeScale();
        }

        private void OpenSettings()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        private void CloseSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(true);
        }

        private void OpenCredits()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(true);
        }

        private void CloseCredits()
        {
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        private void OpenDeleteConfirm()
        {
            if (deleteConfirmPanel != null) deleteConfirmPanel.SetActive(true);
        }

        private void CloseDeleteConfirm()
        {
            if (deleteConfirmPanel != null) deleteConfirmPanel.SetActive(false);
        }

        /// <summary>Application.Quit() no hace nada en el Editor (limitacion de Unity) — el
        /// #if deja que el boton "Salir" tambien funcione probando en el Editor, no solo en
        /// un build real. OnApplicationQuit ya guarda la partida (ver GameManager), no hace
        /// falta llamar a SaveNow() a mano aca.</summary>
        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ToggleVibration()
        {
            SettingsManager.VibrationEnabled = !SettingsManager.VibrationEnabled;
            RefreshVibrationLabel();
        }

        private void RefreshVibrationLabel()
        {
            if (vibrationToggleLabel != null)
                vibrationToggleLabel.text = "Vibracion: " + (SettingsManager.VibrationEnabled ? "activada" : "desactivada");
        }

        private void ConfirmDeleteSave()
        {
            SaveSystem.DeleteSave();
            Time.timeScale = 1f;
            // Recargar la escena es el "cargar partida vacia" mas simple y confiable:
            // todos los Manager vuelven a su estado inicial sin tener que resetear cada
            // uno a mano desde aca.
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>Time.timeScale = 0 mientras cualquier panel de este menu este abierto:
        /// pausa de verdad (temporizador de jefe, autoguardado) en vez de solo tapar la
        /// pantalla con un overlay decorativo.</summary>
        private void RefreshTimeScale()
        {
            bool anyOpen = (pausePanel != null && pausePanel.activeSelf)
                || (settingsPanel != null && settingsPanel.activeSelf)
                || (creditsPanel != null && creditsPanel.activeSelf)
                || (deleteConfirmPanel != null && deleteConfirmPanel.activeSelf);

            Time.timeScale = anyOpen ? 0f : 1f;
        }
    }
}
