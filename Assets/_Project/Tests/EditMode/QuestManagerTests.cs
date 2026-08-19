using System.Collections.Generic;
using NUnit.Framework;
using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;

namespace TapLegends.Tests
{
    public class QuestManagerTests
    {
        private GameObject questGo;
        private GameObject walletGo;
        private QuestManager questManager;
        private GoldWallet wallet;
        private QuestData quest;

        [SetUp]
        public void SetUp()
        {
            walletGo = new GameObject("GoldWallet_Test");
            wallet = walletGo.AddComponent<GoldWallet>();
            TestUtils.ForceSingleton(wallet);

            questGo = new GameObject("QuestManager_Test");
            questManager = questGo.AddComponent<QuestManager>();
            TestUtils.ForceSingleton(questManager);
            // Ver gotcha de testing #4 en CLAUDE.md — se suscriben a mano los mismos handlers
            // que OnEnable() suscribiria, sin invocar OnEnable() en si.
            EventBus.Subscribe(TestUtils.PrivateHandler<QuestManager, EnemyKilledEvent>(questManager, "HandleEnemyKilled"));
            EventBus.Subscribe(TestUtils.PrivateHandler<QuestManager, SkillActivatedEvent>(questManager, "HandleSkillActivated"));
            EventBus.Subscribe(TestUtils.PrivateHandler<QuestManager, GoldChangedEvent>(questManager, "HandleGoldChanged"));

            quest = ScriptableObject.CreateInstance<QuestData>();
            quest.id = "kill_5";
            quest.objective = QuestObjective.KillEnemies;
            quest.targetAmount = 5d;
            quest.rewardGold = 20d;

            questManager.Configure(new[] { quest });

            // QuestManager solo asigna misiones en Start() (EditMode no lo dispara), asi
            // que se seedea el slot 0 via LoadState — mismo camino que usa una partida real.
            questManager.LoadState(new QuestSaveData
            {
                dayKey = "2026-08-17",
                slots = new List<QuestSlotSaveEntry>
                {
                    new QuestSlotSaveEntry { questId = quest.id, progress = 0d, claimed = false },
                },
            });
        }

        [TearDown]
        public void TearDown()
        {
            TestUtils.ResetSingleton<QuestManager>();
            TestUtils.ResetSingleton<GoldWallet>();
            Object.DestroyImmediate(questGo);
            Object.DestroyImmediate(walletGo);
            Object.DestroyImmediate(quest);
            EventBus.ClearAll();
        }

        [Test]
        public void LoadState_AsignaLaMisionAlSlot()
        {
            Assert.That(questManager.GetQuest(0), Is.EqualTo(quest));
            Assert.That(questManager.GetProgress(0), Is.EqualTo(0d));
            Assert.That(questManager.IsClaimed(0), Is.False);
        }

        [Test]
        public void EnemyKilledEvent_SumaProgresoAlSlotConElObjetivoQueCorresponde()
        {
            EventBus.Publish(new EnemyKilledEvent { EnemyId = "e1", GoldDropped = 1d, WasBoss = false });
            EventBus.Publish(new EnemyKilledEvent { EnemyId = "e2", GoldDropped = 1d, WasBoss = false });

            Assert.That(questManager.GetProgress(0), Is.EqualTo(2d));
        }

        [Test]
        public void AddProgress_NuncaSuperaElTargetAmount()
        {
            for (int i = 0; i < 20; i++)
                EventBus.Publish(new EnemyKilledEvent { EnemyId = "e", GoldDropped = 1d, WasBoss = false });

            Assert.That(questManager.GetProgress(0), Is.EqualTo(quest.targetAmount));
        }

        [Test]
        public void IsComplete_AntesDeAlcanzarElObjetivo_EsFalso()
        {
            Assert.That(questManager.IsComplete(0), Is.False);
        }

        [Test]
        public void IsComplete_AlAlcanzarElObjetivo_EsVerdadero()
        {
            for (int i = 0; i < 5; i++)
                EventBus.Publish(new EnemyKilledEvent { EnemyId = "e", GoldDropped = 1d, WasBoss = false });

            Assert.That(questManager.IsComplete(0), Is.True);
        }

        [Test]
        public void TryClaim_SinCompletar_Falla()
        {
            Assert.That(questManager.TryClaim(0), Is.False);
        }

        [Test]
        public void TryClaim_CompletaYSinReclamar_OtorgaOroYMarcaReclamado()
        {
            for (int i = 0; i < 5; i++)
                EventBus.Publish(new EnemyKilledEvent { EnemyId = "e", GoldDropped = 1d, WasBoss = false });

            bool success = questManager.TryClaim(0);

            Assert.That(success, Is.True);
            Assert.That(questManager.IsClaimed(0), Is.True);
            Assert.That(wallet.Gold, Is.EqualTo(quest.rewardGold).Within(1e-6));
        }

        [Test]
        public void TryClaim_YaReclamado_Falla()
        {
            for (int i = 0; i < 5; i++)
                EventBus.Publish(new EnemyKilledEvent { EnemyId = "e", GoldDropped = 1d, WasBoss = false });
            questManager.TryClaim(0);

            bool secondClaim = questManager.TryClaim(0);

            Assert.That(secondClaim, Is.False);
            Assert.That(wallet.Gold, Is.EqualTo(quest.rewardGold).Within(1e-6));
        }

        [Test]
        public void AddProgress_MisionYaReclamada_NoSigueAcumulando()
        {
            for (int i = 0; i < 5; i++)
                EventBus.Publish(new EnemyKilledEvent { EnemyId = "e", GoldDropped = 1d, WasBoss = false });
            questManager.TryClaim(0);

            EventBus.Publish(new EnemyKilledEvent { EnemyId = "e", GoldDropped = 1d, WasBoss = false });

            Assert.That(questManager.GetProgress(0), Is.EqualTo(quest.targetAmount));
        }

        [Test]
        public void GetSaveData_RedondeaElEstadoActualDelSlot()
        {
            EventBus.Publish(new EnemyKilledEvent { EnemyId = "e", GoldDropped = 1d, WasBoss = false });

            var saved = questManager.GetSaveData();

            Assert.That(saved.dayKey, Is.EqualTo("2026-08-17"));
            Assert.That(saved.slots[0].questId, Is.EqualTo(quest.id));
            Assert.That(saved.slots[0].progress, Is.EqualTo(1d));
        }
    }
}
