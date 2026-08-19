using System.Collections;
using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TapLegends.UI
{
    /// <summary>
    /// Reproduce la animacion del personaje principal: Idle en loop, y uno de los 3
    /// ataques (alternando en orden 1-2-3-1...) cada vez que el jugador toca. Un tap
    /// mientras ya hay un ataque en curso no reinicia el swing — deja que termine, el
    /// daño del tap ya se aplico igual (Enemy.OnPointerDown), esto es solo el dibujo.
    /// Idle a 10fps "chunky" (ver TapLegends-Plan.md); el ataque va mas rapido (~17fps)
    /// porque en un tapper se dispara muy seguido — a 10fps se sentia lento/pastoso.
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private Image bodyImage;

        private const float IdleFrameDuration = 0.1f;
        private const float AttackFrameDuration = 0.06f;

        private Coroutine routine;
        private bool isAttacking;
        private int nextAttackIndex;

        private void OnEnable()
        {
            EventBus.Subscribe<WeaponSkinChangedEvent>(HandleSkinChanged);
            EventBus.Subscribe<PlayerTappedEvent>(HandleTapped);
            PlayIdle();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<WeaponSkinChangedEvent>(HandleSkinChanged);
            EventBus.Unsubscribe<PlayerTappedEvent>(HandleTapped);
        }

        private void HandleSkinChanged(WeaponSkinChangedEvent evt)
        {
            if (!isAttacking) PlayIdle();
        }

        private void HandleTapped(PlayerTappedEvent evt)
        {
            if (!isAttacking) PlayAttack();
        }

        private void PlayIdle()
        {
            var skin = WeaponSkinManager.Instance != null ? WeaponSkinManager.Instance.Selected : null;
            PlayLoop(skin != null ? skin.idleFrames : null);
        }

        private void PlayAttack()
        {
            var skin = WeaponSkinManager.Instance != null ? WeaponSkinManager.Instance.Selected : null;
            if (skin == null) return;

            Sprite[] frames = nextAttackIndex switch
            {
                1 => skin.attack2Frames,
                2 => skin.attack3Frames,
                _ => skin.attack1Frames,
            };
            nextAttackIndex = (nextAttackIndex + 1) % 3;

            if (frames == null || frames.Length == 0) return;

            if (routine != null) StopCoroutine(routine);
            isAttacking = true;
            routine = StartCoroutine(PlayOnceThenIdle(frames));
        }

        private IEnumerator PlayOnceThenIdle(Sprite[] frames)
        {
            foreach (var frame in frames)
            {
                if (bodyImage != null) bodyImage.sprite = frame;
                yield return new WaitForSeconds(AttackFrameDuration);
            }
            isAttacking = false;
            PlayIdle();
        }

        private void PlayLoop(Sprite[] frames)
        {
            if (routine != null) StopCoroutine(routine);
            routine = frames != null && frames.Length > 0 ? StartCoroutine(LoopFrames(frames)) : null;
        }

        private IEnumerator LoopFrames(Sprite[] frames)
        {
            int i = 0;
            while (true)
            {
                if (bodyImage != null) bodyImage.sprite = frames[i];
                i = (i + 1) % frames.Length;
                yield return new WaitForSeconds(IdleFrameDuration);
            }
        }
    }
}
