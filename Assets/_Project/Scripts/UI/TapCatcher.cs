using TapLegends.Combat;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TapLegends.UI
{
    /// <summary>
    /// Captura taps en cualquier parte de la pantalla (no solo encima del sprite del
    /// enemigo) y los reenvia al enemigo actual — mismo criterio que Tap Titans: tocar
    /// "cerca" del monstruo cuenta igual que tocarlo directo.
    ///
    /// Vive MUY temprano en la jerarquia del Canvas (justo despues del fondo, antes de
    /// TopBar/BottomPanel/botones/paneles). Unity solo entrega el evento de puntero al
    /// elemento MAS AL FRENTE bajo el dedo (GraphicRaycaster.FindFirstRaycast), asi que
    /// esto nunca "roba" un tap que iba para un boton: cualquier UI que se dibuje encima
    /// (con raycastTarget=true) intercepta su propio click primero, sin coordinar nada con
    /// este script ni tener que recortar su area a mano. Un CanvasGroup con
    /// blocksRaycasts=false (boton oculto, ej. ChallengeBossButton) tambien deja pasar el
    /// tap hacia aca solo, gratis.
    /// </summary>
    public class TapCatcher : MonoBehaviour, IPointerDownHandler
    {
        public void OnPointerDown(PointerEventData eventData) =>
            EnemySpawner.Instance?.CurrentEnemyInstance?.OnPointerDown(eventData);
    }
}
