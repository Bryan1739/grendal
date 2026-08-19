#if UNITY_EDITOR || DEVELOPMENT_BUILD
using TapLegends.Combat;
using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.DebugTools
{
    /// <summary>
    /// Menu de trucos — la herramienta mas importante del proyecto (ver TapLegends-Plan.md,
    /// seccion "MVP continuo"). Sin esto, probar el contenido de horas avanzadas de la
    /// economia cuesta horas reales. Se compila solo en builds de desarrollo/editor.
    ///
    /// Triple tap en la esquina superior derecha para abrir/cerrar.
    /// </summary>
    public class DebugMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private float tripleTapWindow = 0.6f;

        // Los botones se crean por codigo en SceneBuilder (sin prefab en la v0.1), pero el
        // onClick.AddListener DEBE registrarse aca, en Awake — un AddListener hecho desde un
        // script de Editor no sobrevive la recarga de dominio al entrar en Play mode.
        [SerializeField] private Button[] cheatButtons;

        private static readonly System.Action<DebugMenuController>[] CheatActions =
        {
            m => m.AddGold1K(),
            m => m.AddGold1M(),
            m => m.AddGold1B(),
            m => m.AddGold1T(),
            m => m.SetTapDamage1000(),
            m => m.ResetSave(),
            m => m.SimulateOffline1Hour(),
            m => m.ResetBossAutoFight(),
        };

        private int tapCount;
        private float lastTapTime;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);

            if (cheatButtons == null) return;
            for (int i = 0; i < cheatButtons.Length && i < CheatActions.Length; i++)
            {
                if (cheatButtons[i] == null) continue;
                var action = CheatActions[i];
                cheatButtons[i].onClick.AddListener(() => action(this));
            }
        }

        public void OnTriggerZoneTapped()
        {
            float now = Time.unscaledTime;
            if (now - lastTapTime > tripleTapWindow) tapCount = 0;

            tapCount++;
            lastTapTime = now;

            if (tapCount >= 3)
            {
                tapCount = 0;
                TogglePanel();
            }
        }

        private void TogglePanel()
        {
            if (panel != null) panel.SetActive(!panel.activeSelf);
        }

        // --- Trucos ---

        public void AddGold1K() => GoldWallet.Instance?.DebugAdd(1_000d);
        public void AddGold1M() => GoldWallet.Instance?.DebugAdd(1_000_000d);
        public void AddGold1B() => GoldWallet.Instance?.DebugAdd(1_000_000_000d);
        public void AddGold1T() => GoldWallet.Instance?.DebugAdd(1_000_000_000_000d);

        public void SetTapDamage1000() => PlayerStats.Instance?.DebugSetTapDamage(1000d);

        /// <summary>Borra el guardado en disco. No reinicia el estado en memoria de la
        /// partida actual — hace falta recargar la escena/app para verlo desde cero.</summary>
        public void ResetSave()
        {
            SaveSystem.DeleteSave();
            UnityEngine.Debug.Log("[DebugMenu] Guardado borrado. Reinicia la app (o recarga la escena) para arrancar de cero.");
        }

        public void SimulateOffline1Hour() => GameManager.Instance?.DebugSimulateOfflineSeconds(3600f);

        /// <summary>Fuerza el modo "automatico" del jefe actual sin tener que vencerlo de
        /// verdad (vencerlo ya lo hace solo, ver EnemySpawner.HandleEnemyDied) — util para
        /// probar el mecanismo repetidas veces rapido.</summary>
        public void ResetBossAutoFight() => EnemySpawner.Instance?.DebugResetBossAutoFight();
    }
}
#endif
