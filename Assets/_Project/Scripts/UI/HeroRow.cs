using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    [RequireComponent(typeof(Button))]
    public class HeroRow : MonoBehaviour
    {
        [SerializeField] private Text label;
        // Asignado desde SceneBuilder via SerializedObject (no con un metodo Setup en
        // tiempo de editor): un campo no serializado pierde su valor al entrar en Play
        // mode, porque la escena se recarga desde su estado serializado, no desde el
        // grafo de objetos que quedo en memoria mientras se armaba en el editor.
        [SerializeField] private HeroData hero;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(HandleClick);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GoldChangedEvent>(HandleGoldChanged);
            EventBus.Subscribe<HeroChangedEvent>(HandleHeroChanged);
            Refresh();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GoldChangedEvent>(HandleGoldChanged);
            EventBus.Unsubscribe<HeroChangedEvent>(HandleHeroChanged);
        }

        private void HandleGoldChanged(GoldChangedEvent evt) => Refresh();

        private void HandleHeroChanged(HeroChangedEvent evt)
        {
            if (hero != null && evt.HeroId == hero.id) Refresh();
        }

        private void HandleClick() => HeroRoster.Instance?.TryBuyOrUpgrade(hero);

        private void Refresh()
        {
            if (hero == null || label == null || HeroRoster.Instance == null) return;

            int level = HeroRoster.Instance.GetLevel(hero);
            double cost = HeroRoster.Instance.GetCost(hero);
            double dps = HeroRoster.Instance.GetDps(hero);

            label.text = $"{hero.displayName}  (Nv.{level})\n" +
                         $"DPS: {NumberFormatter.Format(dps)}   Coste: {NumberFormatter.Format(cost)}";

            button.interactable = GoldWallet.Instance != null && GoldWallet.Instance.Gold >= cost;
        }
    }
}
