using System.Collections.Generic;
using NUnit.Framework;
using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;

namespace TapLegends.Tests
{
    public class PetManagerTests
    {
        private GameObject petGo;
        private PetManager pets;
        private PetData pet;

        [SetUp]
        public void SetUp()
        {
            petGo = new GameObject("PetManager_Test");
            pets = petGo.AddComponent<PetManager>();
            TestUtils.ForceSingleton(pets);

            pet = ScriptableObject.CreateInstance<PetData>();
            pet.id = "test_pet";
            pet.displayName = "Test Pet";
            pet.effect = PetEffect.Gold;
            pet.baseBonusPercent = 0.05d;
            pet.bonusGrowthPerLevel = 1.10d;

            pets.Configure(new[] { pet });
        }

        [TearDown]
        public void TearDown()
        {
            TestUtils.ResetSingleton<PetManager>();
            Object.DestroyImmediate(petGo);
            Object.DestroyImmediate(pet);
            EventBus.ClearAll();
        }

        [Test]
        public void IsOwned_SinHuevo_EsFalso()
        {
            Assert.That(pets.IsOwned(pet), Is.False);
        }

        [Test]
        public void IsActive_AlInicio_EsFalso()
        {
            Assert.That(pets.IsActive(pet), Is.False);
            Assert.That(pets.GetActiveSlot(0), Is.Null);
            Assert.That(pets.GetActiveSlot(1), Is.Null);
        }

        [Test]
        public void GetBonusPercent_SinPoseer_EsCero()
        {
            Assert.That(pets.GetBonusPercent(pet), Is.EqualTo(0d));
        }

        [Test]
        public void TryUpgrade_SinPoseer_Falla()
        {
            Assert.That(pets.TryUpgrade(pet), Is.False);
        }

        [Test]
        public void LoadState_MarcaComoPoseidaYRestauraNivelYFragmentos()
        {
            pets.LoadState(new List<PetSaveEntry>
            {
                new PetSaveEntry { petId = pet.id, level = 2, fragments = 4, activeSlot = -1 },
            });

            Assert.That(pets.IsOwned(pet), Is.True);
            Assert.That(pets.GetLevel(pet), Is.EqualTo(2));
            Assert.That(pets.GetFragments(pet), Is.EqualTo(4));
        }

        [Test]
        public void LoadState_ConActiveSlot_RestauraElLado()
        {
            pets.LoadState(new List<PetSaveEntry>
            {
                new PetSaveEntry { petId = pet.id, level = 1, fragments = 0, activeSlot = 0 },
            });

            Assert.That(pets.IsActive(pet), Is.True);
            Assert.That(pets.GetActiveSlot(0), Is.EqualTo(pet));
        }

        [Test]
        public void SetActive_MascotaNoPoseida_NoHaceNada()
        {
            pets.SetActive(pet);
            Assert.That(pets.IsActive(pet), Is.False);
        }

        [Test]
        public void SetActive_MascotaPoseida_LaActivaEnElPrimerLadoLibre()
        {
            pets.LoadState(new List<PetSaveEntry>
            {
                new PetSaveEntry { petId = pet.id, level = 1, fragments = 0, activeSlot = -1 },
            });

            pets.SetActive(pet);

            Assert.That(pets.IsActive(pet), Is.True);
            Assert.That(pets.GetActiveSlot(0), Is.EqualTo(pet));
        }

        [Test]
        public void SetActive_MascotaYaActiva_LaDesactiva()
        {
            pets.LoadState(new List<PetSaveEntry>
            {
                new PetSaveEntry { petId = pet.id, level = 1, fragments = 0, activeSlot = 0 },
            });

            pets.SetActive(pet);

            Assert.That(pets.IsActive(pet), Is.False);
            Assert.That(pets.GetActiveSlot(0), Is.Null);
        }

        [Test]
        public void SetActive_DosMascotas_OcupanLadosDistintos()
        {
            var pet2 = ScriptableObject.CreateInstance<PetData>();
            pet2.id = "test_pet_2";
            pet2.effect = PetEffect.Gold;
            pets.Configure(new[] { pet, pet2 });
            pets.LoadState(new List<PetSaveEntry>
            {
                new PetSaveEntry { petId = pet.id, level = 1, fragments = 0, activeSlot = -1 },
                new PetSaveEntry { petId = pet2.id, level = 1, fragments = 0, activeSlot = -1 },
            });

            pets.SetActive(pet);
            pets.SetActive(pet2);

            Assert.That(pets.GetActiveSlot(0), Is.EqualTo(pet));
            Assert.That(pets.GetActiveSlot(1), Is.EqualTo(pet2));
            Object.DestroyImmediate(pet2);
        }

        [Test]
        public void GoldMultiplier_MascotaActiva_UsaElBonusCompleto()
        {
            pets.LoadState(new List<PetSaveEntry>
            {
                new PetSaveEntry { petId = pet.id, level = 1, fragments = 0, activeSlot = 0 },
            });

            Assert.That(pets.GoldMultiplier, Is.EqualTo(1d + pet.baseBonusPercent).Within(1e-6));
        }

        [Test]
        public void GoldMultiplier_MascotaNoActiva_UsaSolo20PorCiento()
        {
            var pet2 = ScriptableObject.CreateInstance<PetData>();
            pet2.id = "test_pet_2";
            pet2.effect = PetEffect.Gold;
            pet2.baseBonusPercent = 0.10d;
            pet2.bonusGrowthPerLevel = 1.10d;
            pets.Configure(new[] { pet, pet2 });

            pets.LoadState(new List<PetSaveEntry>
            {
                new PetSaveEntry { petId = pet.id, level = 1, fragments = 0, activeSlot = 0 },
                new PetSaveEntry { petId = pet2.id, level = 1, fragments = 0, activeSlot = -1 },
            });

            double expected = 1d + pet.baseBonusPercent + pet2.baseBonusPercent * 0.2d;
            Assert.That(pets.GoldMultiplier, Is.EqualTo(expected).Within(1e-6));
            Object.DestroyImmediate(pet2);
        }

        [Test]
        public void TryUpgrade_ConFragmentosSuficientes_SubeNivelYDescuentaFragmentos()
        {
            pets.LoadState(new List<PetSaveEntry>
            {
                new PetSaveEntry { petId = pet.id, level = 1, fragments = 100, activeSlot = -1 },
            });
            double cost = pets.GetUpgradeCost(pet);

            bool success = pets.TryUpgrade(pet);

            Assert.That(success, Is.True);
            Assert.That(pets.GetLevel(pet), Is.EqualTo(2));
            Assert.That(pets.GetFragments(pet), Is.EqualTo(100 - (int)cost));
        }

        [Test]
        public void TryUpgrade_SinFragmentosSuficientes_Falla()
        {
            pets.LoadState(new List<PetSaveEntry>
            {
                new PetSaveEntry { petId = pet.id, level = 1, fragments = 0, activeSlot = -1 },
            });

            Assert.That(pets.TryUpgrade(pet), Is.False);
        }

        [Test]
        public void LoadState_PublicaPetChangedEvent()
        {
            bool fired = false;
            EventBus.Subscribe<PetChangedEvent>(evt => fired = evt.PetId == pet.id);

            pets.LoadState(new List<PetSaveEntry>
            {
                new PetSaveEntry { petId = pet.id, level = 1, fragments = 0, activeSlot = -1 },
            });

            Assert.That(fired, Is.True);
        }
    }
}
