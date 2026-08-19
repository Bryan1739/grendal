using TapLegends.Core;
using UnityEngine;

namespace TapLegends.Audio
{
    /// <summary>
    /// Musica de fondo: tema base sonando siempre, tema de jefe mientras BossActive este
    /// en true (StageStatusEvent). Vuelve sola al tema base al salir de la pelea — ganada,
    /// perdida o escapada, las tres terminan en isBossFight=false en EnemySpawner, asi que
    /// no hace falta distinguir el motivo aca. Un solo AudioSource con loop=true: cambia de
    /// clip y vuelve a Play() en vez de crossfade, simple y suficiente para el alcance actual.
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [SerializeField] private AudioClip baseMusic;
        [SerializeField] private AudioClip bossMusic;

        private bool isBossActive;
        private bool initialized;

        private void Awake()
        {
            if (source == null) return;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        private void OnEnable() => EventBus.Subscribe<StageStatusEvent>(HandleStageStatus);
        private void OnDisable() => EventBus.Unsubscribe<StageStatusEvent>(HandleStageStatus);

        private void HandleStageStatus(StageStatusEvent evt)
        {
            // Solo reacciona a la transicion real (entrar/salir de pelea de jefe) — este
            // evento se publica en cada kill normal tambien, no solo al cambiar de musica.
            if (initialized && evt.BossActive == isBossActive) return;

            initialized = true;
            isBossActive = evt.BossActive;
            Play(isBossActive ? bossMusic : baseMusic);
        }

        private void Play(AudioClip clip)
        {
            if (source == null || clip == null || source.clip == clip) return;
            source.clip = clip;
            source.Play();
        }
    }
}
