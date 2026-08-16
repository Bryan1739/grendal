using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>Fila de una pieza de equipo. Si no esta conseguida todavia, muestra un
    /// aviso en vez de datos — no hay nada que mejorar hasta que cae de un jefe.</summary>
    [RequireComponent(typeof(Button))]
    public class EquipmentRow : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private Image icon;
        [SerializeField] private EquipmentData item;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(HandleClick);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EquipmentChangedEvent>(HandleEquipmentChanged);
            Refresh();
        }

        private void OnDisable() => EventBus.Unsubscribe<EquipmentChangedEvent>(HandleEquipmentChanged);

        private void HandleEquipmentChanged(EquipmentChangedEvent evt)
        {
            if (item != null && evt.EquipmentId == item.id) Refresh();
        }

        private void HandleClick() => EquipmentManager.Instance?.TryUpgrade(item);

        private void Refresh()
        {
            if (item == null || label == null || EquipmentManager.Instance == null) return;

            bool owned = EquipmentManager.Instance.IsOwned(item);
            var rarity = EquipmentManager.Instance.GetRarity(item);
            var rarityColor = PlaceholderArt.RarityColor.TryGetValue(rarity.ToString(), out var c) ? c : Color.white;

            if (icon != null) icon.color = owned ? rarityColor : new Color(1f, 1f, 1f, 0.15f);

            if (!owned)
            {
                label.text = $"{item.displayName}\nSin conseguir — cae de jefes";
                button.interactable = false;
                return;
            }

            int level = EquipmentManager.Instance.GetLevel(item);
            int fragments = EquipmentManager.Instance.GetFragments(item);
            double cost = EquipmentManager.Instance.GetUpgradeCost(item);
            double effectPercent = EquipmentManager.Instance.GetEffectValue(item) * 100d;

            label.text = $"{item.displayName} ({rarity}, Nv.{level})\n+{effectPercent:0.0}%   Fragmentos: {fragments}/{NumberFormatter.Format(cost)}";
            button.interactable = fragments >= cost;
        }
    }
}
