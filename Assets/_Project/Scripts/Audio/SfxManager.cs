using TapLegends.Core;
using UnityEngine;

namespace TapLegends.Audio
{
    /// <summary>
    /// Efectos de sonido puntuales, en un AudioSource separado del de MusicManager para que
    /// el volumen de SFX no dependa del de musica. PlayOneShot permite que se superpongan
    /// sin cortarse (varios taps seguidos, o un tap justo cuando muere el jefe).
    ///
    /// - Tap (PlayerTappedEvent): cada toque del jugador sobre un enemigo vivo.
    /// - Mejora (UpgradePurchasedEvent): tap, habilidades, heroes, equipo, mascotas y
    ///   mejoras de prestigio — cualquier compra que sube de nivel algo. Ver el comentario
    ///   de UpgradePurchasedEvent en EventBus.cs: solo se publica en la compra real, nunca
    ///   al cargar partida ni en acciones que reusan el mismo evento de "cambio" por otro
    ///   motivo (activar mascota, loot de jefe, reset de prestigio).
    /// - Vencer a un jefe (EnemyKilledEvent.WasBoss): solo la victoria — BossEscapedEvent
    ///   (perder contra el jefe por tiempo) no dispara nada aca, es una derrota.
    /// </summary>
    public class SfxManager : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [SerializeField] private AudioClip tapClip;
        [SerializeField] private AudioClip powerUpClip;
        [SerializeField] private AudioClip bossDefeatClip;

        // El tap suena decenas de veces por minuto; mas bajo que el resto para no cansar.
        private const float TapVolumeScale = 0.45f;

        private void Awake()
        {
            if (source == null) return;
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerTappedEvent>(HandlePlayerTapped);
            EventBus.Subscribe<UpgradePurchasedEvent>(HandleUpgradePurchased);
            EventBus.Subscribe<EnemyKilledEvent>(HandleEnemyKilled);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerTappedEvent>(HandlePlayerTapped);
            EventBus.Unsubscribe<UpgradePurchasedEvent>(HandleUpgradePurchased);
            EventBus.Unsubscribe<EnemyKilledEvent>(HandleEnemyKilled);
        }

        private void HandlePlayerTapped(PlayerTappedEvent evt) => Play(tapClip, TapVolumeScale);

        private void HandleUpgradePurchased(UpgradePurchasedEvent evt) => Play(powerUpClip, 1f);

        private void HandleEnemyKilled(EnemyKilledEvent evt)
        {
            if (evt.WasBoss) Play(bossDefeatClip, 1f);
        }

        private void Play(AudioClip clip, float volumeScale)
        {
            if (source == null || clip == null) return;
            source.PlayOneShot(clip, volumeScale);
        }
    }
}
