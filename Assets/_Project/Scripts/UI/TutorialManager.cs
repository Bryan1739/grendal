using TapLegends.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>
    /// Guia de los primeros 3 minutos (Etapa 10, ver TapLegends-Plan.md): tres pasos —
    /// tocar al monstruo, comprar la mejora de daño, contratar un heroe — cada uno
    /// avanza solo con el evento real de haberlo hecho, nunca con un timer. Corre una
    /// sola vez por partida (se guarda en SaveData.tutorialCompleted); "Saltar" lo
    /// marca completado sin pasar por los pasos restantes.
    ///
    /// No bloquea el juego: el overlay no tiene raycastTarget en el fondo, solo la
    /// burbuja de texto y el boton de saltar reciben toques. Un puntero apunta al
    /// objetivo de cada paso convirtiendo su posicion mundial a espacio local de este
    /// overlay (funciona sin importar en que panel anidado viva el objetivo).
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private RectTransform overlayRect;
        [SerializeField] private Text bubbleText;
        [SerializeField] private Button skipButton;
        [SerializeField] private RectTransform pointerArrow;

        [SerializeField] private RectTransform stepEnemyTarget;
        [SerializeField] private RectTransform stepUpgradeTarget;
        [SerializeField] private RectTransform stepHeroesTarget;

        private static readonly string[] StepMessages =
        {
            "Toca al monstruo para atacarlo",
            "Usa tu oro para mejorar el daño de tu toque",
            "Contrata un heroe para que pelee por ti",
        };

        private RectTransform[] stepTargets;
        private int currentStep = -1;

        public bool IsCompleted { get; private set; }

        private void Awake()
        {
            Instance = this;
            stepTargets = new[] { stepEnemyTarget, stepUpgradeTarget, stepHeroesTarget };

            if (skipButton != null) skipButton.onClick.AddListener(Complete);

            // Por defecto (partida nueva, sin LoadState todavia) arranca mostrando el
            // primer paso; LoadState lo desactiva si la partida cargada ya lo termino.
            ShowStep(0);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyKilledEvent>(HandleEnemyKilled);
            EventBus.Subscribe<TapDamageUpgradedEvent>(HandleTapUpgraded);
            EventBus.Subscribe<HeroChangedEvent>(HandleHeroChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyKilledEvent>(HandleEnemyKilled);
            EventBus.Unsubscribe<TapDamageUpgradedEvent>(HandleTapUpgraded);
            EventBus.Unsubscribe<HeroChangedEvent>(HandleHeroChanged);
        }

        public void LoadState(bool completed)
        {
            if (completed) Complete();
            else ShowStep(0);
        }

        private void HandleEnemyKilled(EnemyKilledEvent evt)
        {
            if (currentStep == 0) ShowStep(1);
        }

        private void HandleTapUpgraded(TapDamageUpgradedEvent evt)
        {
            if (currentStep == 1) ShowStep(2);
        }

        private void HandleHeroChanged(HeroChangedEvent evt)
        {
            if (currentStep == 2) Complete();
        }

        private void ShowStep(int step)
        {
            currentStep = step;

            if (step < 0 || step >= StepMessages.Length)
            {
                Complete();
                return;
            }

            if (overlayRoot != null) overlayRoot.SetActive(true);
            if (bubbleText != null) bubbleText.text = StepMessages[step];
        }

        private void Complete()
        {
            currentStep = StepMessages.Length; // fuera de rango: ningun evento vuelve a avanzar nada
            IsCompleted = true;
            if (overlayRoot != null) overlayRoot.SetActive(false);
        }

        private void Update()
        {
            if (IsCompleted || pointerArrow == null || overlayRect == null) return;
            if (currentStep < 0 || currentStep >= stepTargets.Length) return;

            var target = stepTargets[currentStep];

            // El objetivo del paso 3 (pestaña Heroes) puede estar oculto todavia si el
            // jugador aun no llego al nivel de desbloqueo (ver BottomTabController) — no
            // mostrar la flecha apuntando a algo invisible.
            bool targetVisible = target != null && target.gameObject.activeInHierarchy;
            pointerArrow.gameObject.SetActive(targetVisible);
            if (!targetVisible) return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    overlayRect, RectTransformUtility.WorldToScreenPoint(null, target.position),
                    null, out var localPoint))
            {
                float bounce = Mathf.Sin(Time.unscaledTime * 3.5f) * 8f;
                pointerArrow.anchoredPosition = localPoint + new Vector2(0f, 48f + bounce);
            }
        }
    }
}
