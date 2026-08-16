using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>Fila de una mejora permanente, comprada con reliquias. Mismo patron que HeroRow.</summary>
    [RequireComponent(typeof(Button))]
    public class PrestigeUpgradeRow : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private PrestigeUpgradeData upgrade;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(HandleClick);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<RelicsChangedEvent>(HandleRelicsChanged);
            EventBus.Subscribe<PrestigeUpgradeChangedEvent>(HandleUpgradeChanged);
            Refresh();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<RelicsChangedEvent>(HandleRelicsChanged);
            EventBus.Unsubscribe<PrestigeUpgradeChangedEvent>(HandleUpgradeChanged);
        }

        private void HandleRelicsChanged(RelicsChangedEvent evt) => Refresh();

        private void HandleUpgradeChanged(PrestigeUpgradeChangedEvent evt)
        {
            if (upgrade != null && evt.UpgradeId == upgrade.id) Refresh();
        }

        private void HandleClick() => PrestigeManager.Instance?.TryBuyUpgrade(upgrade);

        private void Refresh()
        {
            if (upgrade == null || label == null || PrestigeManager.Instance == null) return;

            int level = PrestigeManager.Instance.GetLevel(upgrade);
            double cost = PrestigeManager.Instance.GetCost(upgrade);
            string effectText = FormatEffect(upgrade, level);

            label.text = $"{upgrade.displayName} (Nv.{level})\n{effectText}\nCoste: {NumberFormatter.Format(cost)} reliquias";
            button.interactable = PrestigeManager.Instance.Relics >= cost;
        }

        private static string FormatEffect(PrestigeUpgradeData upgrade, int level)
        {
            double current = level * upgrade.valuePerLevel * 100d;
            double next = (level + 1) * upgrade.valuePerLevel * 100d;

            return upgrade.effect switch
            {
                PrestigeEffect.GlobalDamage => $"+{current:0}% → +{next:0}% daño",
                PrestigeEffect.Gold => $"+{current:0}% → +{next:0}% oro",
                PrestigeEffect.HeroDps => $"+{current:0}% → +{next:0}% DPS de heroes",
                PrestigeEffect.SkillCooldownReduction => $"-{current:0}% → -{next:0}% cooldown",
                _ => "",
            };
        }
    }
}
