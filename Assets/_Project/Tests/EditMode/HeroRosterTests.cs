using NUnit.Framework;
using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;

namespace TapLegends.Tests
{
    public class HeroRosterTests
    {
        private GameObject rosterGo;
        private GameObject walletGo;
        private HeroRoster roster;
        private GoldWallet wallet;
        private HeroData hero;

        [SetUp]
        public void SetUp()
        {
            walletGo = new GameObject("GoldWallet_Test");
            wallet = walletGo.AddComponent<GoldWallet>();
            TestUtils.ForceSingleton(wallet); // ver gotcha de testing #4 en CLAUDE.md

            rosterGo = new GameObject("HeroRoster_Test");
            roster = rosterGo.AddComponent<HeroRoster>();
            TestUtils.ForceSingleton(roster);

            hero = ScriptableObject.CreateInstance<HeroData>();
            hero.id = "test_hero";
            hero.baseCost = 20d;
            hero.costGrowth = 1.12d;
            hero.baseDps = 1d;
            hero.dpsGrowthMultiplier = 1.15d;

            roster.Configure(new[] { hero });
        }

        [TearDown]
        public void TearDown()
        {
            TestUtils.ResetSingleton<HeroRoster>();
            TestUtils.ResetSingleton<GoldWallet>();
            Object.DestroyImmediate(rosterGo);
            Object.DestroyImmediate(walletGo);
            Object.DestroyImmediate(hero);
            EventBus.ClearAll();
        }

        [Test]
        public void GetLevel_HeroeSinComprar_EsCero()
        {
            Assert.That(roster.GetLevel(hero), Is.EqualTo(0));
        }

        [Test]
        public void GetCost_NivelInicial_EsElCosteBase()
        {
            Assert.That(roster.GetCost(hero), Is.EqualTo(hero.baseCost).Within(1e-6));
        }

        [Test]
        public void GetDps_HeroeSinComprar_EsCero()
        {
            Assert.That(roster.GetDps(hero), Is.EqualTo(0d));
        }

        [Test]
        public void TryBuyOrUpgrade_SinOroSuficiente_Falla()
        {
            bool success = roster.TryBuyOrUpgrade(hero);

            Assert.That(success, Is.False);
            Assert.That(roster.GetLevel(hero), Is.EqualTo(0));
        }

        [Test]
        public void TryBuyOrUpgrade_ConOroSuficiente_SubeNivelYDescuentaOro()
        {
            wallet.Add(1000d);
            double costo = roster.GetCost(hero);

            bool success = roster.TryBuyOrUpgrade(hero);

            Assert.That(success, Is.True);
            Assert.That(roster.GetLevel(hero), Is.EqualTo(1));
            Assert.That(wallet.Gold, Is.EqualTo(1000d - costo).Within(1e-6));
        }

        [Test]
        public void GetCost_CreceMultiplicativamenteConCadaCompra()
        {
            wallet.Add(1_000_000d);
            double costoNivel0 = roster.GetCost(hero);

            roster.TryBuyOrUpgrade(hero);
            double costoNivel1 = roster.GetCost(hero);

            Assert.That(costoNivel1, Is.EqualTo(costoNivel0 * hero.costGrowth).Within(1e-6));
        }

        [Test]
        public void GetDps_DespuesDeComprar_UsaLaFormulaBase()
        {
            wallet.Add(1_000_000d);
            roster.TryBuyOrUpgrade(hero); // nivel 1

            // nivel 1: baseDps * dpsGrowth^(1-1) = baseDps
            Assert.That(roster.GetDps(hero), Is.EqualTo(hero.baseDps).Within(1e-6));
        }

        [Test]
        public void TotalDps_SumaTodosLosHeroesComprados()
        {
            var hero2 = ScriptableObject.CreateInstance<HeroData>();
            hero2.id = "test_hero_2";
            hero2.baseCost = 10d;
            hero2.costGrowth = 1.1d;
            hero2.baseDps = 2d;
            hero2.dpsGrowthMultiplier = 1.1d;
            roster.Configure(new[] { hero, hero2 });

            wallet.Add(1_000_000d);
            roster.TryBuyOrUpgrade(hero);
            roster.TryBuyOrUpgrade(hero2);

            Assert.That(roster.TotalDps, Is.EqualTo(hero.baseDps + hero2.baseDps).Within(1e-6));
            Object.DestroyImmediate(hero2);
        }

        [Test]
        public void ResetAll_VuelveTodosLosHeroesANivelCero()
        {
            wallet.Add(1_000_000d);
            roster.TryBuyOrUpgrade(hero);
            Assert.That(roster.GetLevel(hero), Is.EqualTo(1));

            roster.ResetAll();

            Assert.That(roster.GetLevel(hero), Is.EqualTo(0));
        }

        [Test]
        public void TryBuyOrUpgrade_PublicaHeroChangedEventYUpgradePurchasedEvent()
        {
            wallet.Add(1000d);
            bool heroChanged = false;
            bool upgradePurchased = false;
            EventBus.Subscribe<HeroChangedEvent>(evt => heroChanged = evt.HeroId == hero.id);
            EventBus.Subscribe<UpgradePurchasedEvent>(evt => upgradePurchased = true);

            roster.TryBuyOrUpgrade(hero);

            Assert.That(heroChanged, Is.True);
            Assert.That(upgradePurchased, Is.True);
        }
    }
}
