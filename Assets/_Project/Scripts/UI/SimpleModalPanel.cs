using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>Modal generico de abrir/cerrar (Misiones, Logros...) — sin contenido propio,
    /// solo el SetActive + boton de cerrar. El contenido lo arman otros componentes hijos.</summary>
    public class SimpleModalPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        public void Open()
        {
            if (panel != null) panel.SetActive(true);
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
        }
    }
}
