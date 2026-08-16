using TapLegends.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>
    /// Casillero de la barra de habilidades. Antes de comprarla (nivel 0) se ve apagado y
    /// no responde al toque — "equiparla" es exactamente eso: la primera compra la deja
    /// usable ahi mismo, sin un paso separado de "equipar".
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SkillSlotButton : MonoBehaviour
    {
        [SerializeField] private SkillData skill;
        [SerializeField] private Image icon;
        [SerializeField] private Image cooldownOverlay;

        private static readonly Color LockedTint = new Color(1f, 1f, 1f, 0.12f);
        private static readonly Color BuffActiveTint = new Color(1f, 0.92f, 0.55f);

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(HandleClick);
        }

        private void HandleClick() => SkillManager.Instance?.TryActivate(skill);

        private void Update()
        {
            if (skill == null || SkillManager.Instance == null) return;

            bool owned = SkillManager.Instance.IsOwned(skill);
            bool buffActive = owned && SkillManager.Instance.IsBuffActive(skill);
            if (icon != null) icon.color = !owned ? LockedTint : buffActive ? BuffActiveTint : Color.white;

            if (!owned)
            {
                button.interactable = false;
                if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0f;
                return;
            }

            float remaining = SkillManager.Instance.GetCooldownRemaining(skill);
            float duration = SkillManager.Instance.GetCooldownDuration(skill);
            if (cooldownOverlay != null)
                cooldownOverlay.fillAmount = duration > 0f ? Mathf.Clamp01(remaining / duration) : 0f;

            button.interactable = remaining <= 0f;
        }
    }
}
