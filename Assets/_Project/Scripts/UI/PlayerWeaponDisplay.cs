using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>El arma del personaje principal cambia de color segun la rareza del arma
    /// equipada — "el arma debe verse en el sprite del heroe" (ver TapLegends-Plan.md, Etapa 6).</summary>
    public class PlayerWeaponDisplay : MonoBehaviour
    {
        [SerializeField] private Image weaponImage;
        [SerializeField] private EquipmentData weaponItem;
        [SerializeField] private Color defaultColor = new Color(0.95f, 0.82f, 0.35f);

        private void OnEnable()
        {
            EventBus.Subscribe<EquipmentChangedEvent>(HandleEquipmentChanged);
            Refresh();
        }

        private void OnDisable() => EventBus.Unsubscribe<EquipmentChangedEvent>(HandleEquipmentChanged);

        private void HandleEquipmentChanged(EquipmentChangedEvent evt)
        {
            if (weaponItem != null && evt.EquipmentId == weaponItem.id) Refresh();
        }

        private void Refresh()
        {
            if (weaponImage == null) return;

            if (weaponItem == null || EquipmentManager.Instance == null || !EquipmentManager.Instance.IsOwned(weaponItem))
            {
                weaponImage.color = defaultColor;
                return;
            }

            var rarity = EquipmentManager.Instance.GetRarity(weaponItem);
            weaponImage.color = PlaceholderArt.RarityColor.TryGetValue(rarity.ToString(), out var c) ? c : defaultColor;
        }
    }
}
