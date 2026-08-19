using NUnit.Framework;
using TapLegends.Combat;
using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;

namespace TapLegends.Tests
{
    /// <summary>
    /// Cubre GameManager.DebugSimulateOfflineSeconds — la unica puerta publica hacia la
    /// formula de oro offline (ComputeOfflineGold/GoldPerDamageAtLevel son privados,
    /// LoadGame() depende de SaveSystem.Load() leyendo disco, no se testea aca). El bug real
    /// que motivo estos tests: el oro offline no aplicaba los multiplicadores de
    /// Prestigio/Equipo/Mascotas que el oro activo si aplica (ver GameManager.cs y
    /// docs/ludo/L4-economia-offline.md) — GoldMultiplier_AplicaLosTresMultiplicadoresPasivos
    /// guarda esa correccion.
    /// </summary>
    public class GameManagerOfflineGoldTests
    {
        private GameObject gameManagerGo;
        private GameObject walletGo;
        private GameObject spawnerGo;
        private GameObject rosterGo;
        private GameManager gameManager;
        private GoldWallet wallet;
        private EnemySpawner spawner;
        private HeroRoster roster;
        private HeroData hero;

        [SetUp]
        public void SetUp()
        {
            walletGo = new GameObject("GoldWallet_Test");
            wallet = walletGo.AddComponent<GoldWallet>();
            TestUtils.ForceSingleton(wallet);

            spawnerGo = new GameObject("EnemySpawner_Test");
            spawner = spawnerGo.AddComponent<EnemySpawner>();
            TestUtils.ForceSingleton(spawner);
            spawner.LoadState(5, 0, false);

            rosterGo = new GameObject("HeroRoster_Test");
            roster = rosterGo.AddComponent<HeroRoster>();
            TestUtils.ForceSingleton(roster);

            hero = ScriptableObject.CreateInstance<HeroData>();
            hero.id = "test_hero";
            hero.baseCost = 10d;
            hero.costGrowth = 1.1d;
            hero.baseDps = 5d;
            hero.dpsGrowthMultiplier = 1.1d;
            roster.Configure(new[] { hero });
            wallet.Add(1000d);
            roster.TryBuyOrUpgrade(hero); // nivel 1, DPS = 5

            gameManagerGo = new GameObject("GameManager_Test");
            gameManager = gameManagerGo.AddComponent<GameManager>();
            TestUtils.ForceSingleton(gameManager);
        }

        [TearDown]
        public void TearDown()
        {
            TestUtils.ResetSingleton<GameManager>();
            TestUtils.ResetSingleton<GoldWallet>();
            TestUtils.ResetSingleton<EnemySpawner>();
            TestUtils.ResetSingleton<HeroRoster>();
            Object.DestroyImmediate(gameManagerGo);
            Object.DestroyImmediate(walletGo);
            Object.DestroyImmediate(spawnerGo);
            Object.DestroyImmediate(rosterGo);
            Object.DestroyImmediate(hero);
            EventBus.ClearAll();
        }

        private static double ExpectedGoldPerDamage(int level)
        {
            double health = GameConstants.EnemyHealthForLevel(level);
            double gold = GameConstants.EnemyGoldForLevel(level);
            return gold / health; // sin Prestigio/Equipo/Mascotas activos en este setup
        }

        [Test]
        public void DebugSimulateOfflineSeconds_SinDps_NoOtorgaOro()
        {
            roster.Configure(System.Array.Empty<HeroData>()); // TotalDps = 0
            wallet.LoadState(0d);

            gameManager.DebugSimulateOfflineSeconds(3600f);

            Assert.That(wallet.Gold, Is.EqualTo(0d));
        }

        [Test]
        public void DebugSimulateOfflineSeconds_OtorgaOroSegunLaFormulaDocumentada()
        {
            wallet.LoadState(0d); // resetea el oro gastado en Configure() de arriba
            float seconds = 1800f;

            gameManager.DebugSimulateOfflineSeconds(seconds);

            double expected = roster.TotalDps * seconds * GameConstants.OfflineDpsFactor * ExpectedGoldPerDamage(5);
            Assert.That(wallet.Gold, Is.EqualTo(expected).Within(1e-6));
        }

        [Test]
        public void DebugSimulateOfflineSeconds_PublicaOfflineProgressEvent()
        {
            OfflineProgressEvent? received = null;
            EventBus.Subscribe<OfflineProgressEvent>(evt => received = evt);

            gameManager.DebugSimulateOfflineSeconds(600f);

            Assert.That(received, Is.Not.Null);
            Assert.That(received.Value.SecondsAway, Is.EqualTo(600f));
            Assert.That(received.Value.GoldEarned, Is.GreaterThan(0d));
        }

        [Test]
        public void DebugSimulateOfflineSeconds_ConCeroSegundos_NoOtorgaOro()
        {
            wallet.LoadState(0d);
            gameManager.DebugSimulateOfflineSeconds(0f);

            Assert.That(wallet.Gold, Is.EqualTo(0d));
        }
    }
}
