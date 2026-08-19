using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>Sub-pestaña "Info" del Perfil: nombre y presentacion (pensados para un
    /// futuro modo online, ver ProfileManager). Guarda solo, sin boton — cada campo
    /// escribe a ProfileManager en onEndEdit (al perder foco o apretar Enter).</summary>
    public class ProfileInfoPanel : MonoBehaviour
    {
        [SerializeField] private InputField usernameField;
        [SerializeField] private InputField bioField;

        private void Awake()
        {
            if (usernameField != null) usernameField.onEndEdit.AddListener(HandleUsernameChanged);
            if (bioField != null) bioField.onEndEdit.AddListener(HandleBioChanged);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<ProfileChangedEvent>(HandleProfileChanged);
            Refresh();
        }

        private void OnDisable() => EventBus.Unsubscribe<ProfileChangedEvent>(HandleProfileChanged);

        private void HandleProfileChanged(ProfileChangedEvent evt) => Refresh();

        private void HandleUsernameChanged(string value) => ProfileManager.Instance?.SetUsername(value);
        private void HandleBioChanged(string value) => ProfileManager.Instance?.SetBio(value);

        private void Refresh()
        {
            if (ProfileManager.Instance == null) return;

            // No pisar el campo si el jugador esta escribiendo en el en este momento
            // (un LoadState u otro cambio de perfil no deberia interrumpir el tipeo).
            if (usernameField != null && !usernameField.isFocused) usernameField.text = ProfileManager.Instance.Username;
            if (bioField != null && !bioField.isFocused) bioField.text = ProfileManager.Instance.Bio;
        }
    }
}
