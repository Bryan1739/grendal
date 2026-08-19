using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>
    /// Mantener pulsado repite el click del Button al que se agrega (ver
    /// TapLegends-Plan.md, Etapa 10: "botones de compra con mantener pulsado para comprar
    /// x10/x100"). No sabe nada del sistema que compra — solo re-invoca el mismo onClick
    /// que el boton ya tiene, asi que cada repeticion sigue pasando por el chequeo de "me
    /// alcanza" de siempre (se frena sola si te quedas sin oro/fragmentos/reliquias a mitad
    /// de camino, porque cada click individual ya valida eso).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class HoldToRepeatButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private const float InitialDelay = 0.45f;
        private const float RepeatInterval = 0.09f;

        private Button button;
        private Coroutine routine;

        private void Awake() => button = GetComponent<Button>();

        public void OnPointerDown(PointerEventData eventData)
        {
            StopRepeating();
            routine = StartCoroutine(RepeatWhileHeld());
        }

        public void OnPointerUp(PointerEventData eventData) => StopRepeating();
        public void OnPointerExit(PointerEventData eventData) => StopRepeating();

        private void OnDisable() => StopRepeating();

        private void StopRepeating()
        {
            if (routine == null) return;
            StopCoroutine(routine);
            routine = null;
        }

        private IEnumerator RepeatWhileHeld()
        {
            // El primer click ya lo maneja el Button nativo al soltar (o al mantener mas
            // de InitialDelay, este bucle empieza a simular clicks extra encima).
            yield return new WaitForSeconds(InitialDelay);
            while (true)
            {
                if (button != null && button.interactable) button.onClick.Invoke();
                yield return new WaitForSeconds(RepeatInterval);
            }
        }
    }
}
