using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>
    /// Pestañas Mejoras/Heroes/Equipo/Mascotas/Prestigio + colapso del panel inferior a
    /// una tira delgada, para liberar pantalla y ver mejor a los heroes/mascota alrededor
    /// del monstruo.
    /// </summary>
    public class BottomTabController : MonoBehaviour
    {
        private enum Tab { Upgrades, Heroes, Equipment, Pets, Prestige }

        [SerializeField] private GameObject upgradesPanel;
        [SerializeField] private GameObject heroesPanel;
        [SerializeField] private GameObject equipmentPanel;
        [SerializeField] private GameObject petsPanel;
        [SerializeField] private GameObject prestigePanel;
        [SerializeField] private Button upgradesTabButton;
        [SerializeField] private Button heroesTabButton;
        [SerializeField] private Button equipmentTabButton;
        [SerializeField] private Button petsTabButton;
        [SerializeField] private Button prestigeTabButton;

        [Header("Colapso")]
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private GameObject tabBar;
        [SerializeField] private Button collapseButton;
        [SerializeField] private Text collapseLabel;
        [SerializeField] private float expandedAnchorY = 0.34f;
        [SerializeField] private float collapsedAnchorY = 0.03f; // ~36px de handle sobre 1280 de referencia

        private Tab activeTab = Tab.Upgrades;
        private bool isExpanded = true;

        private void Awake()
        {
            // El AddListener va aca (Awake), no en el script de Editor que arma la escena:
            // un listener registrado desde un script de Editor no sobrevive la recarga de
            // dominio al entrar en Play mode.
            if (upgradesTabButton != null) upgradesTabButton.onClick.AddListener(ShowUpgrades);
            if (heroesTabButton != null) heroesTabButton.onClick.AddListener(ShowHeroes);
            if (equipmentTabButton != null) equipmentTabButton.onClick.AddListener(ShowEquipment);
            if (petsTabButton != null) petsTabButton.onClick.AddListener(ShowPets);
            if (prestigeTabButton != null) prestigeTabButton.onClick.AddListener(ShowPrestige);
            if (collapseButton != null) collapseButton.onClick.AddListener(ToggleCollapse);

            Apply();
        }

        public void ShowUpgrades()
        {
            activeTab = Tab.Upgrades;
            Apply();
        }

        public void ShowHeroes()
        {
            activeTab = Tab.Heroes;
            Apply();
        }

        public void ShowEquipment()
        {
            activeTab = Tab.Equipment;
            Apply();
        }

        public void ShowPets()
        {
            activeTab = Tab.Pets;
            Apply();
        }

        public void ShowPrestige()
        {
            activeTab = Tab.Prestige;
            Apply();
        }

        public void ToggleCollapse()
        {
            isExpanded = !isExpanded;
            Apply();
        }

        private void Apply()
        {
            if (panelRect != null)
            {
                var anchorMax = panelRect.anchorMax;
                anchorMax.y = isExpanded ? expandedAnchorY : collapsedAnchorY;
                panelRect.anchorMax = anchorMax;
            }

            if (tabBar != null) tabBar.SetActive(isExpanded);
            if (upgradesPanel != null) upgradesPanel.SetActive(isExpanded && activeTab == Tab.Upgrades);
            if (heroesPanel != null) heroesPanel.SetActive(isExpanded && activeTab == Tab.Heroes);
            if (equipmentPanel != null) equipmentPanel.SetActive(isExpanded && activeTab == Tab.Equipment);
            if (petsPanel != null) petsPanel.SetActive(isExpanded && activeTab == Tab.Pets);
            if (prestigePanel != null) prestigePanel.SetActive(isExpanded && activeTab == Tab.Prestige);
            if (collapseLabel != null) collapseLabel.text = isExpanded ? "▼" : "▲";
        }
    }
}
