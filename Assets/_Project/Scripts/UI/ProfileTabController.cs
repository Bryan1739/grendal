using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>Sub-pestañas Info/Estadísticas/Skins dentro del panel de Perfil. Mismo
    /// criterio que BottomTabController pero sin colapso — solo cambia cual sub-panel
    /// esta activo.</summary>
    public class ProfileTabController : MonoBehaviour
    {
        private enum Tab { Info, Stats, Skins }

        [SerializeField] private GameObject infoPanel;
        [SerializeField] private GameObject statsPanel;
        [SerializeField] private GameObject skinsPanel;
        [SerializeField] private Button infoTabButton;
        [SerializeField] private Button statsTabButton;
        [SerializeField] private Button skinsTabButton;

        private Tab activeTab = Tab.Info;

        private void Awake()
        {
            if (infoTabButton != null) infoTabButton.onClick.AddListener(ShowInfo);
            if (statsTabButton != null) statsTabButton.onClick.AddListener(ShowStats);
            if (skinsTabButton != null) skinsTabButton.onClick.AddListener(ShowSkins);
            Apply();
        }

        public void ShowInfo() { activeTab = Tab.Info; Apply(); }
        public void ShowStats() { activeTab = Tab.Stats; Apply(); }
        public void ShowSkins() { activeTab = Tab.Skins; Apply(); }

        private void Apply()
        {
            if (infoPanel != null) infoPanel.SetActive(activeTab == Tab.Info);
            if (statsPanel != null) statsPanel.SetActive(activeTab == Tab.Stats);
            if (skinsPanel != null) skinsPanel.SetActive(activeTab == Tab.Skins);
        }
    }
}
