using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    [RequireComponent(typeof(Button))]
    public class UpgradeTapDamageButton : MonoBehaviour
    {
        [SerializeField] private Text label;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(HandleClick);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GoldChangedEvent>(HandleGoldChanged);
            EventBus.Subscribe<TapDamageUpgradedEvent>(HandleUpgraded);
            Refresh();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GoldChangedEvent>(HandleGoldChanged);
            EventBus.Unsubscribe<TapDamageUpgradedEvent>(HandleUpgraded);
        }

        private void HandleGoldChanged(GoldChangedEvent evt) => Refresh();
        private void HandleUpgraded(TapDamageUpgradedEvent evt) => Refresh();

        private void HandleClick()
        {
            PlayerStats.Instance?.TryUpgradeTapDamage();
        }

        private void Refresh()
        {
            if (label == null || PlayerStats.Instance == null) return;
            double cost = PlayerStats.Instance.NextUpgradeCost;
            label.text = $"Mejorar daño de tap\n({NumberFormatter.Format(PlayerStats.Instance.TapDamage)} → " +
                         $"{NumberFormatter.Format(PlayerStats.Instance.NextTapDamage)})\n" +
                         $"Coste: {NumberFormatter.Format(cost)}";

            button.interactable = GoldWallet.Instance != null && GoldWallet.Instance.Gold >= cost;
        }
    }
}
