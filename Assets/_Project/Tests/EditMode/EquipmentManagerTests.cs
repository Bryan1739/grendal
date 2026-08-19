using System.Collections.Generic;
using NUnit.Framework;
using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;

namespace TapLegends.Tests
{
    public class EquipmentManagerTests
    {
        private GameObject equipmentGo;
        private EquipmentManager equipment;
        private EquipmentData item;

        [SetUp]
        public void SetUp()
        {
            equipmentGo = new GameObject("EquipmentManager_Test");
            equipment = equipmentGo.AddComponent<EquipmentManager>();
            TestUtils.ForceSingleton(equipment);

            item = ScriptableObject.CreateInstance<EquipmentData>();
            item.id = "test_weapon";
            item.displayName = "Test Weapon";
            item.effect = EquipmentEffect.TapDamage;
            item.baseValuePerLevel = 0.05d;
            item.baseFragmentCost = 3d;
            item.fragmentCostGrowth = 1.12d;

            equipment.Configure(new[] { item });
        }

        [TearDown]
        public void TearDown()
        {
            TestUtils.ResetSingleton<EquipmentManager>();
            Object.DestroyImmediate(equipmentGo);
            Object.DestroyImmediate(item);
            EventBus.ClearAll();
        }

        [Test]
        public void IsOwned_SinBotin_EsFalso()
        {
            Assert.That(equipment.IsOwned(item), Is.False);
        }

        [Test]
        public void GetEffectValue_SinPoseer_EsCero()
        {
            Assert.That(equipment.GetEffectValue(item), Is.EqualTo(0d));
        }

        [Test]
        public void TryUpgrade_SinPoseer_Falla()
        {
            bool success = equipment.TryUpgrade(item);
            Assert.That(success, Is.False);
        }

        [Test]
        public void LoadState_MarcaComoPoseidoYRestauraRarezaYNivel()
        {
            equipment.LoadState(new List<EquipmentSaveEntry>
            {
                new EquipmentSaveEntry { equipmentId = item.id, rarity = (int)EquipmentRarity.Rare, level = 3, fragments = 5 },
            });

            Assert.That(equipment.IsOwned(item), Is.True);
            Assert.That(equipment.GetRarity(item), Is.EqualTo(EquipmentRarity.Rare));
            Assert.That(equipment.GetLevel(item), Is.EqualTo(3));
            Assert.That(equipment.GetFragments(item), Is.EqualTo(5));
        }

        [Test]
        public void GetEffectValue_UsaMultiplicadorDeRarezaYNivel()
        {
            equipment.LoadState(new List<EquipmentSaveEntry>
            {
                new EquipmentSaveEntry { equipmentId = item.id, rarity = (int)EquipmentRarity.Rare, level = 2, fragments = 0 },
            });

            // Rare = x3.2 (ver EquipmentManager.RarityMultiplier)
            double expected = item.baseValuePerLevel * 3.2d * 2;
            Assert.That(equipment.GetEffectValue(item), Is.EqualTo(expected).Within(1e-6));
        }

        [Test]
        public void TapDamageMultiplier_SumaElEfectoDeLasPiezasPoseidas()
        {
            equipment.LoadState(new List<EquipmentSaveEntry>
            {
                new EquipmentSaveEntry { equipmentId = item.id, rarity = (int)EquipmentRarity.Common, level = 1, fragments = 0 },
            });

            Assert.That(equipment.TapDamageMultiplier, Is.EqualTo(1d + item.baseValuePerLevel).Within(1e-6));
        }

        [Test]
        public void TryUpgrade_ConFragmentosSuficientes_SubeNivelYDescuentaFragmentos()
        {
            equipment.LoadState(new List<EquipmentSaveEntry>
            {
                new EquipmentSaveEntry { equipmentId = item.id, rarity = (int)EquipmentRarity.Common, level = 1, fragments = 100 },
            });

            bool success = equipment.TryUpgrade(item);

            Assert.That(success, Is.True);
            Assert.That(equipment.GetLevel(item), Is.EqualTo(2));
            Assert.That(equipment.GetFragments(item), Is.EqualTo(100 - (int)item.baseFragmentCost));
        }

        [Test]
        public void TryUpgrade_SinFragmentosSuficientes_Falla()
        {
            equipment.LoadState(new List<EquipmentSaveEntry>
            {
                new EquipmentSaveEntry { equipmentId = item.id, rarity = (int)EquipmentRarity.Common, level = 1, fragments = 0 },
            });

            bool success = equipment.TryUpgrade(item);

            Assert.That(success, Is.False);
            Assert.That(equipment.GetLevel(item), Is.EqualTo(1));
        }

        [Test]
        public void CooldownMultiplier_NuncaBajaDelTope()
        {
            var cooldownItem = ScriptableObject.CreateInstance<EquipmentData>();
            cooldownItem.id = "cooldown_test";
            cooldownItem.effect = EquipmentEffect.SkillCooldown;
            cooldownItem.baseValuePerLevel = 5d; // exagerado a proposito para forzar el tope
            equipment.Configure(new[] { cooldownItem });
            equipment.LoadState(new List<EquipmentSaveEntry>
            {
                new EquipmentSaveEntry { equipmentId = cooldownItem.id, rarity = (int)EquipmentRarity.Legendary, level = 10, fragments = 0 },
            });

            Assert.That(equipment.CooldownMultiplier, Is.GreaterThanOrEqualTo(0.3d));
            Object.DestroyImmediate(cooldownItem);
        }

        [Test]
        public void LoadState_PublicaEquipmentChangedEvent()
        {
            bool fired = false;
            EventBus.Subscribe<EquipmentChangedEvent>(evt => fired = evt.EquipmentId == item.id);

            equipment.LoadState(new List<EquipmentSaveEntry>
            {
                new EquipmentSaveEntry { equipmentId = item.id, rarity = (int)EquipmentRarity.Common, level = 1, fragments = 0 },
            });

            Assert.That(fired, Is.True);
        }
    }
}
