using NUnit.Framework;
using TapLegends.Combat;
using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;

namespace TapLegends.Tests
{
    public class PrestigeManagerTests
    {
        private GameObject prestigeGo;
        private GameObject spawnerGo;
        private PrestigeManager prestige;
        private EnemySpawner spawner;
        private PrestigeUpgradeData upgrade;

        [SetUp]
        public void SetUp()
        {
            spawnerGo = new GameObject("EnemySpawner_Test");
            spawner = spawnerGo.AddComponent<EnemySpawner>();
            TestUtils.ForceSingleton(spawner);

            prestigeGo = new GameObject("PrestigeManager_Test");
            prestige = prestigeGo.AddComponent<PrestigeManager>();
            TestUtils.ForceSingleton(prestige);

            upgrade = ScriptableObject.CreateInstance<PrestigeUpgradeData>();
            upgrade.id = "test_upgrade";
            upgrade.effect = PrestigeEffect.GlobalDamage;
            upgrade.valuePerLevel = 0.05d;
            upgrade.baseCost = 1d;
            upgrade.costGrowth = 1.2d;

            prestige.Configure(new[] { upgrade });
        }

        [TearDown]
        public void TearDown()
        {
            TestUtils.ResetSingleton<PrestigeManager>();
            TestUtils.ResetSingleton<EnemySpawner>();
            Object.DestroyImmediate(prestigeGo);
            Object.DestroyImmediate(spawnerGo);
            Object.DestroyImmediate(upgrade);
            EventBus.ClearAll();
        }

        [Test]
        public void CanPrestige_AntesDelNivelMinimo_EsFalso()
        {
            spawner.LoadState(GameConstants.PrestigeMinLevel - 1, 0, false);
            Assert.That(prestige.CanPrestige, Is.False);
        }

        [Test]
        public void CanPrestige_EnElNivelMinimo_EsVerdadero()
        {
            spawner.LoadState(GameConstants.PrestigeMinLevel, 0, false);
            Assert.That(prestige.CanPrestige, Is.True);
        }

        [Test]
        public void RelicsIfPrestigeNow_CoincideConLaFormulaDeGameConstants()
        {
            spawner.LoadState(60, 0, false);
            Assert.That(prestige.RelicsIfPrestigeNow, Is.EqualTo(GameConstants.RelicsForLevel(60)).Within(1e-6));
        }

        [Test]
        public void GetCost_NivelInicial_EsElCosteBase()
        {
            Assert.That(prestige.GetCost(upgrade), Is.EqualTo(upgrade.baseCost).Within(1e-6));
        }

        [Test]
        public void TryBuyUpgrade_SinReliquiasSuficientes_Falla()
        {
            bool success = prestige.TryBuyUpgrade(upgrade);

            Assert.That(success, Is.False);
            Assert.That(prestige.GetLevel(upgrade), Is.EqualTo(0));
        }

        [Test]
        public void TryBuyUpgrade_ConReliquiasSuficientes_SubeNivelYDescuentaReliquias()
        {
            prestige.LoadState(100d, null);

            bool success = prestige.TryBuyUpgrade(upgrade);

            Assert.That(success, Is.True);
            Assert.That(prestige.GetLevel(upgrade), Is.EqualTo(1));
            Assert.That(prestige.Relics, Is.EqualTo(100d - upgrade.baseCost).Within(1e-6));
        }

        [Test]
        public void DamageMultiplier_SinMejoras_EsUno()
        {
            Assert.That(prestige.DamageMultiplier, Is.EqualTo(1d));
        }

        [Test]
        public void DamageMultiplier_DespuesDeComprar_SubeSegunValuePerLevel()
        {
            prestige.LoadState(100d, null);
            prestige.TryBuyUpgrade(upgrade); // nivel 1, effect = GlobalDamage

            Assert.That(prestige.DamageMultiplier, Is.EqualTo(1d + upgrade.valuePerLevel).Within(1e-6));
        }

        [Test]
        public void CooldownMultiplier_NuncaBajaDelTope()
        {
            // Tope documentado: 0.3 minimo (70% de reduccion maxima) — un cooldown que
            // llega a 0 rompe el ritmo de la habilidad (ver PrestigeManager.cs).
            var cooldownUpgrade = ScriptableObject.CreateInstance<PrestigeUpgradeData>();
            cooldownUpgrade.id = "cooldown_test";
            cooldownUpgrade.effect = PrestigeEffect.SkillCooldownReduction;
            cooldownUpgrade.valuePerLevel = 1d; // exagerado a proposito para forzar el tope
            cooldownUpgrade.baseCost = 1d;
            cooldownUpgrade.costGrowth = 1d;
            prestige.Configure(new[] { cooldownUpgrade });
            prestige.LoadState(1000d, null);

            for (int i = 0; i < 5; i++) prestige.TryBuyUpgrade(cooldownUpgrade);

            Assert.That(prestige.CooldownMultiplier, Is.GreaterThanOrEqualTo(0.3d));
            Object.DestroyImmediate(cooldownUpgrade);
        }
    }
}
