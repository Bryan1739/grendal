using NUnit.Framework;
using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;

namespace TapLegends.Tests
{
    public class WeaponSkinManagerTests
    {
        private GameObject managerGo;
        private WeaponSkinManager manager;
        private WeaponSkinData skinA;
        private WeaponSkinData skinB;

        [SetUp]
        public void SetUp()
        {
            managerGo = new GameObject("WeaponSkinManager_Test");
            manager = managerGo.AddComponent<WeaponSkinManager>();
            TestUtils.ForceSingleton(manager);

            skinA = ScriptableObject.CreateInstance<WeaponSkinData>();
            skinA.id = "espada_madera";
            skinB = ScriptableObject.CreateInstance<WeaponSkinData>();
            skinB.id = "espada_hierro";

            manager.Configure(new[] { skinA, skinB });
        }

        [TearDown]
        public void TearDown()
        {
            TestUtils.ResetSingleton<WeaponSkinManager>();
            Object.DestroyImmediate(managerGo);
            Object.DestroyImmediate(skinA);
            Object.DestroyImmediate(skinB);
            EventBus.ClearAll();
        }

        [Test]
        public void Selected_AlInicio_EsLaPrimera()
        {
            Assert.That(manager.Selected, Is.EqualTo(skinA));
        }

        [Test]
        public void IsSelected_LaPrimeraAlInicio_EsVerdadero()
        {
            Assert.That(manager.IsSelected(skinA), Is.True);
            Assert.That(manager.IsSelected(skinB), Is.False);
        }

        [Test]
        public void Select_CambiaLaSkinSeleccionada()
        {
            manager.Select(skinB);
            Assert.That(manager.Selected, Is.EqualTo(skinB));
        }

        [Test]
        public void Select_SkinNoEnLaLista_NoHaceNada()
        {
            var outsider = ScriptableObject.CreateInstance<WeaponSkinData>();
            outsider.id = "no_registrada";

            manager.Select(outsider);

            Assert.That(manager.Selected, Is.EqualTo(skinA));
            Object.DestroyImmediate(outsider);
        }

        [Test]
        public void Select_PublicaWeaponSkinChangedEvent()
        {
            bool fired = false;
            EventBus.Subscribe<WeaponSkinChangedEvent>(evt => fired = true);

            manager.Select(skinB);

            Assert.That(fired, Is.True);
        }

        [Test]
        public void GetSaveId_DevuelveElIdDeLaSeleccionada()
        {
            manager.Select(skinB);
            Assert.That(manager.GetSaveId(), Is.EqualTo(skinB.id));
        }

        [Test]
        public void LoadState_ConIdValido_RestauraLaSeleccion()
        {
            manager.LoadState(skinB.id);
            Assert.That(manager.Selected, Is.EqualTo(skinB));
        }

        [Test]
        public void LoadState_ConIdDesconocido_CaeALaPrimera()
        {
            manager.Select(skinB);
            manager.LoadState("id_que_no_existe");

            Assert.That(manager.Selected, Is.EqualTo(skinA));
        }
    }
}
