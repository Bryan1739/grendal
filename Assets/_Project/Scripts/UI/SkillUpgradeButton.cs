using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>Mejora de habilidad, vive en la misma pestaña que la mejora de daño de tap.</summary>
    [RequireComponent(typeof(Button))]
    public class SkillUpgradeButton : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private SkillData skill;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(HandleClick);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GoldChangedEvent>(HandleGoldChanged);
            EventBus.Subscribe<SkillChangedEvent>(HandleSkillChanged);
            Refresh();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GoldChangedEvent>(HandleGoldChanged);
            EventBus.Unsubscribe<SkillChangedEvent>(HandleSkillChanged);
        }

        private void HandleGoldChanged(GoldChangedEvent evt) => Refresh();

        private void HandleSkillChanged(SkillChangedEvent evt)
        {
            if (skill != null && evt.SkillId == skill.id) Refresh();
        }

        private void HandleClick() => SkillManager.Instance?.TryUpgrade(skill);

        private void Refresh()
        {
            if (skill == null || label == null || SkillManager.Instance == null) return;

            int level = SkillManager.Instance.GetLevel(skill);
            double cost = SkillManager.Instance.GetUpgradeCost(skill);
            // Multiplicador que quedaria TRAS comprar (nivel actual -> nivel+1). GetMultiplier
            // usa exponente (nivel-1), asi que en nivel actual "level" el proximo exponente es "level".
            double nextMultiplier = skill.damageMultiplier * System.Math.Pow(skill.multiplierGrowthPerLevel, level);

            if (level <= 0)
            {
                label.text = $"Desbloquear {skill.displayName}\n(daño x{NumberFormatter.Format(nextMultiplier)})\n" +
                             $"Coste: {NumberFormatter.Format(cost)}";
            }
            else
            {
                double currentMultiplier = SkillManager.Instance.GetMultiplier(skill);
                label.text = $"Mejorar {skill.displayName}\n(x{NumberFormatter.Format(currentMultiplier)} → x{NumberFormatter.Format(nextMultiplier)})\n" +
                             $"Coste: {NumberFormatter.Format(cost)}";
            }

            button.interactable = GoldWallet.Instance != null && GoldWallet.Instance.Gold >= cost;
        }
    }
}
