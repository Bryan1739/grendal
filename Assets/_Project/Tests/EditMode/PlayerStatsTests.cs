using NUnit.Framework;
using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;

namespace TapLegends.Tests
{
    public class PlayerStatsTests
    {
        private GameObject statsGo;
        private GameObject walletGo;
        private PlayerStats stats;
        private GoldWallet wallet;

        [SetUp]
        public void SetUp()
        {
            walletGo = new GameObject("GoldWallet_Test");
            wallet = walletGo.AddComponent<GoldWallet>();
            TestUtils.ForceSingleton(wallet);

            statsGo = new GameObject("PlayerStats_Test");
            stats = statsGo.AddComponent<PlayerStats>();
            TestUtils.ForceSingleton(stats);
        }

        [TearDown]
        public void TearDown()
        {
            TestUtils.ResetSingleton<PlayerStats>();
            TestUtils.ResetSingleton<GoldWallet>();
            Object.DestroyImmediate(statsGo);
            Object.DestroyImmediate(walletGo);
            EventBus.ClearAll();
        }

        [Test]
        public void TapDamage_NivelInicial_EsElDañoDeArranque()
        {
            Assert.That(stats.TapDamage, Is.EqualTo(GameConstants.StartingTapDamage).Within(1e-6));
        }

        [Test]
        public void TryUpgradeTapDamage_SinOroSuficiente_Falla()
        {
            // El oro empieza en 0 — la primera mejora ya cuesta GameConstants.TapUpgradeBaseCost
            bool success = stats.TryUpgradeTapDamage();

            Assert.That(success, Is.False);
            Assert.That(stats.TapUpgradeLevel, Is.EqualTo(0));
        }

        [Test]
        public void TryUpgradeTapDamage_ConOroSuficiente_SubeNivelYDañoYDescuentaOro()
        {
            wallet.Add(1000d);
            double dañoAntes = stats.TapDamage;

            bool success = stats.TryUpgradeTapDamage();

            Assert.That(success, Is.True);
            Assert.That(stats.TapUpgradeLevel, Is.EqualTo(1));
            Assert.That(stats.TapDamage, Is.GreaterThan(dañoAntes));
            Assert.That(wallet.Gold, Is.EqualTo(1000d - GameConstants.TapUpgradeCost(0)).Within(1e-6));
        }

        [Test]
        public void TryUpgradeTapDamage_PublicaUpgradePurchasedEvent()
        {
            wallet.Add(1000d);
            bool fired = false;
            EventBus.Subscribe<UpgradePurchasedEvent>(evt => fired = true);

            stats.TryUpgradeTapDamage();

            Assert.That(fired, Is.True);
        }

        [Test]
        public void NextUpgradeCost_CreceDespuesDeCadaCompra()
        {
            wallet.Add(1_000_000d);
            double costo0 = stats.NextUpgradeCost;

            stats.TryUpgradeTapDamage();
            double costo1 = stats.NextUpgradeCost;

            Assert.That(costo1, Is.GreaterThan(costo0));
        }

        [Test]
        public void LoadState_RestauraNivelYRecalculaDaño()
        {
            stats.LoadState(5);

            Assert.That(stats.TapUpgradeLevel, Is.EqualTo(5));
            Assert.That(stats.TapDamage, Is.EqualTo(
                GameConstants.StartingTapDamage * System.Math.Pow(GameConstants.TapUpgradeDamageMultiplier, 5)
            ).Within(1e-6));
        }

        [Test]
        public void LoadState_ConNivelNegativo_SeAplanaACero()
        {
            stats.LoadState(-3);
            Assert.That(stats.TapUpgradeLevel, Is.EqualTo(0));
        }
    }
}
