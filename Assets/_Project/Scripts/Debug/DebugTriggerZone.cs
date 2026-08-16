#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.EventSystems;

namespace TapLegends.DebugTools
{
    /// <summary>Zona invisible en una esquina: triple tap abre el menu de debug.</summary>
    public class DebugTriggerZone : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private DebugMenuController menuController;

        public void OnPointerDown(PointerEventData eventData)
        {
            menuController?.OnTriggerZoneTapped();
        }
    }
}
#endif
