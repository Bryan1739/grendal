using System.Collections;
using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>
    /// Sprite del heroe alrededor del monstruo (igual criterio que PlayerCharacter): invisible
    /// hasta que el heroe tiene nivel 1+, y reacciona tanto a compras como a partidas cargadas.
    /// Tambien refresca el sprite en si cuando cambia la skin elegida en el Perfil.
    /// Mientras esta comprado, alterna Art (de pie) / AttackArt (atacando) en loop continuo
    /// — no esta sincronizado al tick de daño real de HeroRoster (ese aplica el DPS
    /// COMBINADO de todos los heroes una vez por segundo, no por heroe individual), asi que
    /// cada heroe ataca a su propio ritmo — se ve mas vivo con varios heroes en pantalla que
    /// si todos atacaran en perfecta sincronia.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class HeroFieldSprite : MonoBehaviour
    {
        [SerializeField] private HeroData hero;
        [SerializeField] private Image bodyImage;
        [SerializeField] private CanvasGroup baseGroup; // pedestal bajo el heroe (opcional, ver SceneBuilder)

        private const float IdleSeconds = 1.1f;
        private const float AttackSeconds = 0.35f;

        private CanvasGroup canvasGroup;
        private Coroutine attackLoop;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<HeroChangedEvent>(HandleHeroChanged);
            EventBus.Subscribe<SkinChangedEvent>(HandleSkinChanged);
            Refresh();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<HeroChangedEvent>(HandleHeroChanged);
            EventBus.Unsubscribe<SkinChangedEvent>(HandleSkinChanged);
            StopAttackLoop();
        }

        private void HandleHeroChanged(HeroChangedEvent evt)
        {
            if (hero != null && evt.HeroId == hero.id) Refresh();
        }

        private void HandleSkinChanged(SkinChangedEvent evt)
        {
            if (hero != null && (string.IsNullOrEmpty(evt.CharacterId) || evt.CharacterId == hero.id)) Refresh();
        }

        private void Refresh()
        {
            if (hero == null || canvasGroup == null || HeroRoster.Instance == null) return;

            bool owned = HeroRoster.Instance.GetLevel(hero) > 0;
            canvasGroup.alpha = owned ? 1f : 0f;
            if (baseGroup != null) baseGroup.alpha = owned ? 1f : 0f;

            if (owned && attackLoop == null) attackLoop = StartCoroutine(AttackLoop());
            else if (!owned) StopAttackLoop();
        }

        private void StopAttackLoop()
        {
            if (attackLoop == null) return;
            StopCoroutine(attackLoop);
            attackLoop = null;
            if (bodyImage != null && hero != null) bodyImage.sprite = hero.Art;
        }

        private IEnumerator AttackLoop()
        {
            // Offset inicial aleatorio: sin esto, todos los heroes comprados en la misma
            // sesion arrancan el loop en el mismo frame y atacan siempre en perfecta
            // sincronia entre si, que se ve raro con 2+ heroes en pantalla.
            yield return new WaitForSeconds(Random.Range(0f, IdleSeconds));

            while (true)
            {
                if (bodyImage != null && hero != null) bodyImage.sprite = hero.Art;
                yield return new WaitForSeconds(IdleSeconds);

                if (bodyImage != null && hero != null) bodyImage.sprite = hero.AttackArt;
                yield return new WaitForSeconds(AttackSeconds);
            }
        }
    }
}
