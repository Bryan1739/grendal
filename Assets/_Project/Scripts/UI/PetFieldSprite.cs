using System.Collections;
using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>Mascota activa en un lado del heroe (hasta 2 a la vez, una por lado — ver
    /// PetManager). Alterna Art/AttackArt en loop (mismo patron que HeroFieldSprite) para
    /// verse viva; ya NO tiene vaiven en Y (antes "levitaba" con un seno en Update, el
    /// usuario pidio que la unica animacion sea el cambio de sprites).</summary>
    public class PetFieldSprite : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private int slot; // 0 = izquierda, 1 = derecha (ver PetManager.GetActiveSlot)
        [SerializeField] private float fixedHeight = 70f;

        private const float IdleSeconds = 1.4f;
        private const float ActionSeconds = 0.4f;

        private RectTransform rect;
        private Coroutine actionLoop;
        private PetData current;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
        }

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
            StopActionLoop();
        }

        private void HandleChanged(PetChangedEvent evt) => Refresh();

        private void HandleSkinChanged(SkinChangedEvent evt)
        {
            if (current != null && (string.IsNullOrEmpty(evt.CharacterId) || evt.CharacterId == current.id)) Refresh();
        }

        private void Refresh()
        {
            if (icon == null || rect == null) return;

            var active = PetManager.Instance != null ? PetManager.Instance.GetActiveSlot(slot) : null;
            icon.enabled = active != null;
            current = active;

            if (active == null)
            {
                StopActionLoop();
                return;
            }

            // Alto fijo, ancho derivado de la proporcion real de cada mascota -- distintas
            // mascotas comparten lienzo con dimensiones distintas entre si; esto solo evita
            // que el tamaño en pantalla salte de golpe al cambiar de mascota activa.
            icon.sprite = active.Art;
            float aspect = active.Art.rect.width / active.Art.rect.height;
            rect.sizeDelta = new Vector2(fixedHeight * aspect, fixedHeight);

            // El arte de mascotas mira a la derecha por defecto (misma convencion que
            // heroes/personaje). El lado derecho del heroe queda de espaldas al jefe sin
            // esto -- se espeja para que las 2 mascotas queden viendo hacia el centro/jefe
            // en vez de hacia afuera (pedido del usuario).
            rect.localScale = slot == 1 ? new Vector3(-1f, 1f, 1f) : Vector3.one;

            if (actionLoop == null) actionLoop = StartCoroutine(ActionLoop());
        }

        private void StopActionLoop()
        {
            if (actionLoop == null) return;
            StopCoroutine(actionLoop);
            actionLoop = null;
        }

        private IEnumerator ActionLoop()
        {
            yield return new WaitForSeconds(Random.Range(0f, IdleSeconds));
            while (true)
            {
                if (icon != null && current != null) icon.sprite = current.Art;
                yield return new WaitForSeconds(IdleSeconds);
                if (icon != null && current != null) icon.sprite = current.AttackArt;
                yield return new WaitForSeconds(ActionSeconds);
            }
        }
    }
}
