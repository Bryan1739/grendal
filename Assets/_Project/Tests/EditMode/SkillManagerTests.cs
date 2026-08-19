using NUnit.Framework;
using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;

namespace TapLegends.Tests
{
    public class SkillManagerTests
    {
        private GameObject skillGo;
        private GameObject walletGo;
        private SkillManager skillManager;
        private GoldWallet wallet;
        private SkillData buffSkill;

        [SetUp]
        public void SetUp()
        {
            walletGo = new GameObject("GoldWallet_Test");
            wallet = walletGo.AddComponent<GoldWallet>();
            TestUtils.ForceSingleton(wallet);

            skillGo = new GameObject("SkillManager_Test");
            skillManager = skillGo.AddComponent<SkillManager>();
            TestUtils.ForceSingleton(skillManager);

            buffSkill = ScriptableObject.CreateInstance<SkillData>();
            buffSkill.id = "test_buff";
            buffSkill.kind = SkillKind.TimedTapBuff;
            buffSkill.damageMultiplier = 2d;
            buffSkill.multiplierGrowthPerLevel = 1.15d;
            buffSkill.buffDurationSeconds = 10f;
            buffSkill.cooldownSeconds = 8f;
            buffSkill.baseUpgradeCost = 50d;
            buffSkill.upgradeCostGrowth = 1.15d;

            skillManager.Configure(new[] { buffSkill });
        }

        [TearDown]
        public void TearDown()
        {
            TestUtils.ResetSingleton<SkillManager>();
            TestUtils.ResetSingleton<GoldWallet>();
            Object.DestroyImmediate(skillGo);
            Object.DestroyImmediate(walletGo);
            Object.DestroyImmediate(buffSkill);
            EventBus.ClearAll();
        }

        [Test]
        public void IsOwned_SinComprar_EsFalso()
        {
            Assert.That(skillManager.IsOwned(buffSkill), Is.False);
        }

        [Test]
        public void GetMultiplier_SinComprar_EsCero()
        {
            Assert.That(skillManager.GetMultiplier(buffSkill), Is.EqualTo(0d));
        }

        [Test]
        public void TryUpgrade_SinOroSuficiente_Falla()
        {
            Assert.That(skillManager.TryUpgrade(buffSkill), Is.False);
        }

        [Test]
        public void TryUpgrade_ConOroSuficiente_SubeNivelYDescuentaOro()
        {
            wallet.Add(1000d);
            double costo = skillManager.GetUpgradeCost(buffSkill);

            bool success = skillManager.TryUpgrade(buffSkill);

            Assert.That(success, Is.True);
            Assert.That(skillManager.GetLevel(buffSkill), Is.EqualTo(1));
            Assert.That(skillManager.IsOwned(buffSkill), Is.True);
            Assert.That(wallet.Gold, Is.EqualTo(1000d - costo).Within(1e-6));
        }

        [Test]
        public void GetMultiplier_DespuesDeComprar_EsElMultiplicadorBase()
        {
            wallet.Add(1000d);
            skillManager.TryUpgrade(buffSkill); // nivel 1

            Assert.That(skillManager.GetMultiplier(buffSkill), Is.EqualTo(buffSkill.damageMultiplier).Within(1e-6));
        }

        [Test]
        public void TryActivate_SinComprar_Falla()
        {
            Assert.That(skillManager.TryActivate(buffSkill), Is.False);
        }

        [Test]
        public void TryActivate_TimedBuff_ActivaElBuffYElCooldown()
        {
            wallet.Add(1000d);
            skillManager.TryUpgrade(buffSkill);

            bool success = skillManager.TryActivate(buffSkill);

            Assert.That(success, Is.True);
            Assert.That(skillManager.IsBuffActive(buffSkill), Is.True);
            Assert.That(skillManager.GetCooldownRemaining(buffSkill), Is.GreaterThan(0f));
        }

        [Test]
        public void TryActivate_EnCooldown_Falla()
        {
            wallet.Add(1000d);
            skillManager.TryUpgrade(buffSkill);
            skillManager.TryActivate(buffSkill);

            bool secondActivation = skillManager.TryActivate(buffSkill);

            Assert.That(secondActivation, Is.False);
        }

        [Test]
        public void TapBuffMultiplier_SinBuffActivo_EsUno()
        {
            Assert.That(skillManager.TapBuffMultiplier, Is.EqualTo(1d));
        }

        [Test]
        public void TapBuffMultiplier_ConBuffActivo_UsaElMultiplicador()
        {
            wallet.Add(1000d);
            skillManager.TryUpgrade(buffSkill);
            skillManager.TryActivate(buffSkill);

            Assert.That(skillManager.TapBuffMultiplier, Is.EqualTo(buffSkill.damageMultiplier).Within(1e-6));
        }

        [Test]
        public void GetCooldownDuration_SinMejorasDeReduccion_EsElCooldownBase()
        {
            Assert.That(skillManager.GetCooldownDuration(buffSkill), Is.EqualTo(buffSkill.cooldownSeconds).Within(1e-4));
        }

        [Test]
        public void TryUpgrade_PublicaSkillChangedEventYUpgradePurchasedEvent()
        {
            wallet.Add(1000d);
            bool skillChanged = false;
            bool upgradePurchased = false;
            EventBus.Subscribe<SkillChangedEvent>(evt => skillChanged = evt.SkillId == buffSkill.id);
            EventBus.Subscribe<UpgradePurchasedEvent>(evt => upgradePurchased = true);

            skillManager.TryUpgrade(buffSkill);

            Assert.That(skillChanged, Is.True);
            Assert.That(upgradePurchased, Is.True);
        }
    }
}
