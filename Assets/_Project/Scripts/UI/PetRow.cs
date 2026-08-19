using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>
    /// Fila de una mascota. El boton principal de la fila mejora con fragmentos (mismo
    /// criterio que EquipmentRow); un boton chico aparte activa la mascota — son dos
    /// acciones distintas (mejorar cuesta fragmentos, activar no cuesta nada).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class PetRow : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private Image icon;
        [SerializeField] private Button activateButton;
        [SerializeField] private Text activateLabel;
        [SerializeField] private PetData pet;

        private Button upgradeButton;

        private void Awake()
        {
            upgradeButton = GetComponent<Button>();
            upgradeButton.onClick.AddListener(HandleUpgradeClick);
            if (activateButton != null) activateButton.onClick.AddListener(HandleActivateClick);
        }

        // A diferencia de HeroRow/EquipmentRow, se refresca con CUALQUIER PetChangedEvent,
        // no solo el de esta mascota: activar OTRA mascota tambien cambia lo que esta fila
        // tiene que mostrar (deja de estar "[ACTIVA]").
        private void OnEnable()
        {
            EventBus.Subscribe<PetChangedEvent>(HandleChanged);
            EventBus.Subscribe<SkinChangedEvent>(HandleSkinChanged);
            Refresh();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PetChangedEvent>(HandleChanged);
            EventBus.Unsubscribe<SkinChangedEvent>(HandleSkinChanged);
        }

        private void HandleChanged(PetChangedEvent evt) => Refresh();

        private void HandleSkinChanged(SkinChangedEvent evt)
        {
            if (pet != null && (string.IsNullOrEmpty(evt.CharacterId) || evt.CharacterId == pet.id)) Refresh();
        }

        private void HandleUpgradeClick() => PetManager.Instance?.TryUpgrade(pet);
        private void HandleActivateClick() => PetManager.Instance?.SetActive(pet);

        private void Refresh()
        {
            if (pet == null || label == null || PetManager.Instance == null) return;

            bool owned = PetManager.Instance.IsOwned(pet);
            if (icon != null)
            {
                icon.sprite = pet.Art;
                icon.color = owned ? Color.white : new Color(1f, 1f, 1f, 0.15f);
            }

            if (!owned)
            {
                label.text = $"{pet.displayName}\nSin conseguir — cae de jefes";
                upgradeButton.interactable = false;
                if (activateButton != null) activateButton.interactable = false;
                if (activateLabel != null) activateLabel.text = "";
                return;
            }

            bool isActive = PetManager.Instance.IsActive(pet);
            int level = PetManager.Instance.GetLevel(pet);
            int fragments = PetManager.Instance.GetFragments(pet);
            double cost = PetManager.Instance.GetUpgradeCost(pet);
            double bonusPercent = PetManager.Instance.GetBonusPercent(pet) * 100d;
            string effectName = pet.effect switch
            {
                PetEffect.Gold => "oro",
                PetEffect.TapDamage => "daño de tap",
                PetEffect.HeroDps => "DPS de heroes",
                _ => "",
            };

            string activeTag = isActive ? " [ACTIVA]" : " (20%)";
            label.text = $"{pet.displayName} (Nv.{level}){activeTag}\n+{bonusPercent:0.0}% {effectName}   " +
                         $"Fragmentos: {fragments}/{NumberFormatter.Format(cost)}";
            upgradeButton.interactable = fragments >= cost;

            // El boton ahora ALTERNA (hasta 2 mascotas activas a la vez, una a cada lado —
            // ver PetManager.SetActive): activa si estaba libre, desactiva si ya estaba
            // activa. Antes se deshabilitaba estando activa porque solo habia 1 slot y no
            // tenia sentido "reactivarla"; ahora hace falta poder soltarla para liberar un
            // lado.
            if (activateButton != null) activateButton.interactable = true;
            if (activateLabel != null) activateLabel.text = isActive ? "Quitar" : "Activar";
        }
    }
}
