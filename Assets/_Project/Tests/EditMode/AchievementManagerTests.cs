using NUnit.Framework;
using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;

namespace TapLegends.Tests
{
    public class AchievementManagerTests
    {
        private GameObject achievementGo;
        private GameObject walletGo;
        private AchievementManager achievements;
        private GoldWallet wallet;
        private AchievementData killAchievement;

        [SetUp]
        public void SetUp()
        {
            walletGo = new GameObject("GoldWallet_Test");
            wallet = walletGo.AddComponent<GoldWallet>();
            TestUtils.ForceSingleton(wallet);

            achievementGo = new GameObject("AchievementManager_Test");
            achievements = achievementGo.AddComponent<AchievementManager>();
            TestUtils.ForceSingleton(achievements);
            // AddComponent no garantiza que OnEnable ya corrio (ver gotcha de testing #4 en
            // CLAUDE.md), asi que EventBus.Subscribe nunca se llama por si solo — se suscriben
            // a mano los mismos handlers que OnEnable() suscribiria, sin invocar OnEnable() en
            // si (eso rompio el Editor, ver el mismo gotcha).
            EventBus.Subscribe(TestUtils.PrivateHandler<AchievementManager, EnemyKilledEvent>(achievements, "HandleEnemyKilled"));
            EventBus.Subscribe(TestUtils.PrivateHandler<AchievementManager, GoldChangedEvent>(achievements, "HandleGoldChanged"));
            EventBus.Subscribe(TestUtils.PrivateHandler<AchievementManager, StageStatusEvent>(achievements, "HandleStageStatus"));

            killAchievement = ScriptableObject.CreateInstance<AchievementData>();
            killAchievement.id = "kill_10";
            killAchievement.displayName = "Cazador";
            killAchievement.category = AchievementCategory.Combat;
            killAchievement.objective = AchievementObjective.KillEnemies;
            killAchievement.targetAmount = 10d;
            killAchievement.rewardGold = 50d;

            achievements.Configure(new[] { killAchievement });
        }

        [TearDown]
        public void TearDown()
        {
            TestUtils.ResetSingleton<AchievementManager>();
            TestUtils.ResetSingleton<GoldWallet>();
            Object.DestroyImmediate(achievementGo);
            Object.DestroyImmediate(walletGo);
            Object.DestroyImmediate(killAchievement);
            EventBus.ClearAll();
        }

        [Test]
        public void GetProgress_SinEventos_EsCero()
        {
            Assert.That(achievements.GetProgress(AchievementObjective.KillEnemies), Is.EqualTo(0d));
        }

        [Test]
        public void IsUnlocked_AlInicio_EsFalso()
        {
            Assert.That(achievements.IsUnlocked(killAchievement), Is.False);
        }

        [Test]
        public void EnemyKilledEvent_AcumulaElContadorDeKillEnemies()
        {
            EventBus.Publish(new EnemyKilledEvent { EnemyId = "e1", GoldDropped = 1d, WasBoss = false });
            EventBus.Publish(new EnemyKilledEvent { EnemyId = "e2", GoldDropped = 1d, WasBoss = false });

            Assert.That(achievements.GetProgress(AchievementObjective.KillEnemies), Is.EqualTo(2d));
        }

        [Test]
        public void EnemyKilledEvent_ConWasBoss_TambienIncrementaKillBosses()
        {
            EventBus.Publish(new EnemyKilledEvent { EnemyId = "boss1", GoldDropped = 1d, WasBoss = true });

            Assert.That(achievements.GetProgress(AchievementObjective.KillBosses), Is.EqualTo(1d));
            Assert.That(achievements.GetProgress(AchievementObjective.KillEnemies), Is.EqualTo(1d));
        }

        [Test]
        public void AlLlegarAlObjetivo_SeDesbloqueaYSeOtorgaElOro()
        {
            for (int i = 0; i < 10; i++)
                EventBus.Publish(new EnemyKilledEvent { EnemyId = "e", GoldDropped = 1d, WasBoss = false });

            Assert.That(achievements.IsUnlocked(killAchievement), Is.True);
            Assert.That(achievements.UnlockedCount, Is.EqualTo(1));
            Assert.That(wallet.Gold, Is.EqualTo(killAchievement.rewardGold).Within(1e-6));
        }

        [Test]
        public void AlLlegarAlObjetivo_PublicaAchievementUnlockedEventUnaSolaVez()
        {
            int fireCount = 0;
            EventBus.Subscribe<AchievementUnlockedEvent>(evt => fireCount++);

            for (int i = 0; i < 15; i++) // sigue matando despues de desbloquear
                EventBus.Publish(new EnemyKilledEvent { EnemyId = "e", GoldDropped = 1d, WasBoss = false });

            Assert.That(fireCount, Is.EqualTo(1));
        }

        [Test]
        public void StageStatusEvent_SoloSubeSiElNivelEsMayor()
        {
            EventBus.Publish(new StageStatusEvent { Level = 5 });
            EventBus.Publish(new StageStatusEvent { Level = 3 }); // no deberia bajar

            Assert.That(achievements.GetProgress(AchievementObjective.ReachLevel), Is.EqualTo(5d));
        }

        [Test]
        public void GoldChangedEvent_ConDeltaPositivo_AcumulaEarnGoldLifetime()
        {
            EventBus.Publish(new GoldChangedEvent { NewTotal = 100d, Delta = 100d });
            EventBus.Publish(new GoldChangedEvent { NewTotal = 60d, Delta = -40d }); // gasto, no cuenta

            Assert.That(achievements.GetProgress(AchievementObjective.EarnGoldLifetime), Is.EqualTo(100d));
        }

        [Test]
        public void GetNextLocked_DevuelveElDeMenorTargetSinDesbloquear()
        {
            var higherTarget = ScriptableObject.CreateInstance<AchievementData>();
            higherTarget.id = "kill_100";
            higherTarget.category = AchievementCategory.Combat;
            higherTarget.objective = AchievementObjective.KillEnemies;
            higherTarget.targetAmount = 100d;
            achievements.Configure(new[] { killAchievement, higherTarget });

            var next = achievements.GetNextLocked(AchievementCategory.Combat);

            Assert.That(next, Is.EqualTo(killAchievement));
            Object.DestroyImmediate(higherTarget);
        }

        [Test]
        public void LoadState_RestauraContadoresYDesbloqueados()
        {
            var saved = new AchievementSaveData();
            saved.counters.Add(new AchievementCounterEntry { objective = (int)AchievementObjective.KillEnemies, value = 7d });
            saved.unlockedIds.Add(killAchievement.id);

            achievements.LoadState(saved);

            Assert.That(achievements.GetProgress(AchievementObjective.KillEnemies), Is.EqualTo(7d));
            Assert.That(achievements.IsUnlocked(killAchievement), Is.True);
        }
    }
}
