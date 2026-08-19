using TapLegends.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>
    /// Pestañas Mejoras/Heroes/Equipo/Mascotas/Prestigio + colapso del panel inferior a
    /// una tira delgada, para liberar pantalla y ver mejor a los heroes/mascota alrededor
    /// del monstruo. Heroes/Equipo/Mascotas/Prestigio ademas se desbloquean por nivel
    /// (Etapa 10, ver GameConstants) para no mostrar las 5 pestañas desde el nivel 1.
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
        // Antes 0.34 (mucho mas alto que los pies del personaje, en BottomPanelHeightFraction
        // = 0.28 de SceneBuilder) — bajado primero a 0.28 para que coincidiera exacto con
        // los pies. El usuario reporto que TODAVIA quedaba muy pegado: no alcanza con que el
        // borde del panel llegue justo a los pies, hay que dejar los CUADROS de mejora
        // (adentro del panel, con su propio margen para el tab bar) claramente por debajo
        // del personaje completo, no solo de sus pies. Bajado otro tramo mas para dejar ese
        // aire visible.
        [SerializeField] private float expandedAnchorY = 0.22f;
        [SerializeField] private float collapsedAnchorY = 0.03f; // ~36px de handle sobre 1280 de referencia

        private Tab activeTab = Tab.Upgrades;
        private bool isExpanded = true;

        private bool heroesUnlocked;
        private bool equipmentUnlocked;
        private bool petsUnlocked;
        private bool prestigeUnlocked;
        private bool receivedFirstStatus;

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

        private void OnEnable() => EventBus.Subscribe<StageStatusEvent>(HandleStageStatus);
        private void OnDisable() => EventBus.Unsubscribe<StageStatusEvent>(HandleStageStatus);

        private void HandleStageStatus(StageStatusEvent evt)
        {
            bool wasHeroes = heroesUnlocked;
            bool wasEquipment = equipmentUnlocked;
            bool wasPets = petsUnlocked;
            bool wasPrestige = prestigeUnlocked;

            heroesUnlocked = evt.Level >= GameConstants.HeroesUnlockLevel;
            equipmentUnlocked = evt.Level >= GameConstants.EquipmentUnlockLevel;
            petsUnlocked = evt.Level >= GameConstants.PetsUnlockLevel;
            prestigeUnlocked = evt.Level >= GameConstants.PrestigeMinLevel;

            // El primer StageStatusEvent de la sesion (partida nueva o cargada) solo fija el
            // estado inicial, sin avisar: no es un desbloqueo "nuevo" si ya estaba desbloqueado
            // en una partida guardada.
            if (receivedFirstStatus)
            {
                if (heroesUnlocked && !wasHeroes) Announce("Heroes");
                if (equipmentUnlocked && !wasEquipment) Announce("Equipo");
                if (petsUnlocked && !wasPets) Announce("Mascotas");
                if (prestigeUnlocked && !wasPrestige) Announce("Prestigio");
            }
            receivedFirstStatus = true;

            ApplyTabVisibility();
        }

        private static void Announce(string systemName) =>
            EventBus.Publish(new SystemUnlockedEvent { SystemName = systemName });

        private void ApplyTabVisibility()
        {
            if (heroesTabButton != null) heroesTabButton.gameObject.SetActive(heroesUnlocked);
            if (equipmentTabButton != null) equipmentTabButton.gameObject.SetActive(equipmentUnlocked);
            if (petsTabButton != null) petsTabButton.gameObject.SetActive(petsUnlocked);
            if (prestigeTabButton != null) prestigeTabButton.gameObject.SetActive(prestigeUnlocked);
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
