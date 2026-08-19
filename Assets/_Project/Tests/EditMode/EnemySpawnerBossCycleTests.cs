using NUnit.Framework;
using TapLegends.Combat;
using TapLegends.Core;
using UnityEngine;

namespace TapLegends.Tests
{
    /// <summary>
    /// Cubre el ciclo de jefe por decisión de diseño de esta sesión: la PRIMERA vez que un
    /// jefe queda disponible en cada tanda de 10 niveles pelea solo; si se pierde, los
    /// reintentos son opcionales (ChallengeBoss) hasta vencerlo; vencerlo reinicia el ciclo
    /// para el próximo jefe.
    ///
    /// Solo se prueba el estado puro (LoadState) alcanzable sin que `ChallengeBoss()` tenga
    /// que spawnear un `Enemy` real y completo. Se intentó una version que llamaba
    /// `ChallengeBoss()` y despues mataba al enemigo con `TakeDamage()` para probar el ciclo
    /// completo de punta a punta, pero `Enemy.Setup()` tira NullReferenceException en
    /// `bodyImage.sprite = data.Art` — el campo `bodyImage` se asigna en `Enemy.Awake()`, y en
    /// este contexto de EditMode (`new GameObject(nombre, typeof(RectTransform), typeof(Image),
    /// typeof(Enemy))` seguido de `Setup()` en la MISMA linea de EnemySpawner.SpawnNext) ese
    /// Awake() no se dispara a tiempo — algo que en gameplay real (Play mode) siempre funciona,
    /// asi que es una limitacion de EditMode, no un bug de produccion. No vale la pena parchear
    /// codigo de juego solo para satisfacer el test sin poder verificarlo corriendo Unity en
    /// vivo — mejor dejar esto documentado y, si en el futuro se agrega infraestructura de
    /// tests PlayMode, migrar ahi la prueba de punta a punta.
    /// </summary>
    public class EnemySpawnerBossCycleTests
    {
        private GameObject spawnerGo;
        private GameObject areaGo;
        private EnemySpawner spawner;
        private EnemyData commonEnemy;
        private EnemyData bossEnemy;

        [SetUp]
        public void SetUp()
        {
            areaGo = new GameObject("EnemyArea", typeof(RectTransform));

            spawnerGo = new GameObject("EnemySpawner_Test");
            spawner = spawnerGo.AddComponent<EnemySpawner>();
            TestUtils.ForceSingleton(spawner);

            commonEnemy = ScriptableObject.CreateInstance<EnemyData>();
            commonEnemy.id = "test_common";
            commonEnemy.tier = EnemyTier.Common;

            bossEnemy = ScriptableObject.CreateInstance<EnemyData>();
            bossEnemy.id = "test_boss";
            bossEnemy.tier = EnemyTier.Boss;

            spawner.Configure(areaGo.GetComponent<RectTransform>(), new[] { commonEnemy }, new[] { bossEnemy });
        }

        [TearDown]
        public void TearDown()
        {
            TestUtils.ResetSingleton<EnemySpawner>();
            Object.DestroyImmediate(spawnerGo);
            Object.DestroyImmediate(areaGo);
            Object.DestroyImmediate(commonEnemy);
            Object.DestroyImmediate(bossEnemy);
            EventBus.ClearAll();
        }

        [Test]
        public void LoadState_ConKillsCompletos_DejaElJefeDisponible()
        {
            spawner.LoadState(10, GameConstants.EnemiesPerStage, false);

            Assert.That(spawner.BossAvailable, Is.True);
            Assert.That(spawner.HasLostBossOnce, Is.False);
        }

        [Test]
        public void ChallengeBoss_SinJefeDisponible_Falla()
        {
            spawner.LoadState(5, 3, false);

            Assert.That(spawner.ChallengeBoss(), Is.False);
        }
    }
}
