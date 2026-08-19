using System.Collections;
using System.Collections.Generic;
using TapLegends.Combat;
using TapLegends.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>Cada vez que se vence a un jefe, el fondo cambia de escenario. Usa los 20
    /// fondos reales de Art/Backgrounds/ (via ShuffleBag — orden aleatorio, ninguno se
    /// repite hasta que salieron los 20, pedido explicito del usuario) si estan asignados;
    /// si no, cae al paisaje procedural (PlaceholderArt.MountainLandscape) como hacia antes
    /// de tener arte real. El progreso (que fondo toca ahora) se guarda — sin esto, cerrar y
    /// reabrir la app siempre volvia a arrancar en el fondo 1, y con partidas cortas casi
    /// nunca se llegaba a ver variedad real (motivo real del reporte del usuario).</summary>
    public class BackgroundController : MonoBehaviour
    {
        public static BackgroundController Instance { get; private set; }

        [SerializeField] private Image backgroundImage;
        [SerializeField] private float fadeDuration = 0.6f;
        [SerializeField] private Sprite[] chapterBackgrounds = System.Array.Empty<Sprite>();

        private int chapter = 0;
        private ShuffleBag shuffleBag;

        public void Configure(Sprite[] backgrounds) => chapterBackgrounds = backgrounds ?? System.Array.Empty<Sprite>();

        private void Awake() => Instance = this;

        private void OnEnable() => EventBus.Subscribe<EnemyKilledEvent>(HandleEnemyKilled);
        private void OnDisable() => EventBus.Unsubscribe<EnemyKilledEvent>(HandleEnemyKilled);

        private ShuffleBag Bag()
        {
            // Si Count no coincide (arte agregado/sacado entre versiones) se re-crea con el
            // tamaño nuevo — ShuffleBag.Restore ya maneja ese caso, ver ese archivo.
            if (shuffleBag == null || shuffleBag.Count != chapterBackgrounds.Length)
                shuffleBag = new ShuffleBag(chapterBackgrounds.Length);
            return shuffleBag;
        }

        private void HandleEnemyKilled(EnemyKilledEvent evt)
        {
            if (!evt.WasBoss) return;
            chapter++;
            StopAllCoroutines();
            StartCoroutine(SwapBackground());
        }

        private IEnumerator SwapBackground()
        {
            if (backgroundImage == null) yield break;

            var newSprite = chapterBackgrounds.Length > 0
                ? chapterBackgrounds[Bag().Next()]
                : PlaceholderArt.MountainLandscape($"paisaje_{chapter}");
            var visible = backgroundImage.color;
            var hidden = visible;
            hidden.a = 0f;

            yield return Fade(visible, hidden);
            backgroundImage.sprite = newSprite;
            yield return Fade(hidden, visible);
        }

        private IEnumerator Fade(Color from, Color to)
        {
            // Time.unscaledDeltaTime, no Time.deltaTime: si el jugador abre el menu de
            // pausa (Time.timeScale=0, ver PauseMenuController) justo cuando un jefe muere,
            // un timer escalado se queda congelado a mitad de fade — el fondo podia quedar
            // pegado en alpha bajo/cero (se ve "blanco", el default de una Image sin nada
            // detras) hasta que el jugador volviera a cerrar la pausa. Un fade de UI no
            // tiene por que respetar la pausa de gameplay de todos modos.
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                backgroundImage.color = Color.Lerp(from, to, t / fadeDuration);
                yield return null;
            }
            backgroundImage.color = to;
        }

        public int Chapter => chapter;
        public List<int> GetSaveOrder() => chapterBackgrounds.Length > 0 ? Bag().GetOrderSnapshot() : null;
        public int GetSavePosition() => chapterBackgrounds.Length > 0 ? Bag().Position : 0;

        /// <summary>Restaura el capitulo y el barajo guardados, y aplica de una el fondo que
        /// corresponde (sin fade) — sin esto, la pantalla mostraba el fondo 1 (el default de
        /// SceneBuilder) hasta el PROXIMO jefe vencido, aunque el progreso real ya fuera mas
        /// alto.</summary>
        public void LoadState(int savedChapter, List<int> savedOrder, int savedPosition)
        {
            chapter = savedChapter;
            if (chapterBackgrounds.Length == 0) return;

            Bag().Restore(savedOrder, savedPosition);
            if (chapter > 0 && backgroundImage != null && Bag().Current >= 0)
                backgroundImage.sprite = chapterBackgrounds[Bag().Current];
        }
    }
}
