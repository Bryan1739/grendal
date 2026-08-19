using System.Collections.Generic;
using NUnit.Framework;
using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;

namespace TapLegends.Tests
{
    public class ProfileManagerTests
    {
        private GameObject profileGo;
        private ProfileManager profile;

        [SetUp]
        public void SetUp()
        {
            profileGo = new GameObject("ProfileManager_Test");
            profile = profileGo.AddComponent<ProfileManager>();
            TestUtils.ForceSingleton(profile);
        }

        [TearDown]
        public void TearDown()
        {
            TestUtils.ResetSingleton<ProfileManager>();
            Object.DestroyImmediate(profileGo);
            EventBus.ClearAll();
        }

        [Test]
        public void Username_AlInicio_EsVacio()
        {
            Assert.That(profile.Username, Is.EqualTo(""));
        }

        [Test]
        public void SetUsername_GuardaElValor()
        {
            profile.SetUsername("Bryan");
            Assert.That(profile.Username, Is.EqualTo("Bryan"));
        }

        [Test]
        public void SetUsername_ConNull_SeAplanaAVacio()
        {
            profile.SetUsername(null);
            Assert.That(profile.Username, Is.EqualTo(""));
        }

        [Test]
        public void SetBio_GuardaElValor()
        {
            profile.SetBio("Hola, soy un jugador.");
            Assert.That(profile.Bio, Is.EqualTo("Hola, soy un jugador."));
        }

        [Test]
        public void GetSkin_SinElegir_EsCero()
        {
            Assert.That(profile.GetSkin("player_hero"), Is.EqualTo(0));
        }

        [Test]
        public void SetSkin_GuardaElIndiceElegido()
        {
            profile.SetSkin("player_hero", 2);
            Assert.That(profile.GetSkin("player_hero"), Is.EqualTo(2));
        }

        [Test]
        public void SetSkin_SeAplanaAlRangoValido()
        {
            profile.SetSkin("player_hero", 999);
            Assert.That(profile.GetSkin("player_hero"), Is.EqualTo(ProfileManager.SkinCount - 1));

            profile.SetSkin("player_hero", -5);
            Assert.That(profile.GetSkin("player_hero"), Is.EqualTo(0));
        }

        [Test]
        public void ApplySkin_ConSkin0_DevuelveElIdSinCambios()
        {
            Assert.That(profile.ApplySkin("player_hero"), Is.EqualTo("player_hero"));
        }

        [Test]
        public void ApplySkin_ConSkinDistintaDeCero_AgregaElSufijo()
        {
            profile.SetSkin("player_hero", 2);
            Assert.That(profile.ApplySkin("player_hero"), Is.EqualTo("player_hero#skin2"));
        }

        [Test]
        public void SkinnedId_Estatico_ConSkin0_DevuelveElIdTalCual()
        {
            Assert.That(ProfileManager.SkinnedId("hero_1", 0), Is.EqualTo("hero_1"));
        }

        [Test]
        public void SkinnedId_Estatico_ConCharacterIdVacio_DevuelveVacio()
        {
            Assert.That(ProfileManager.SkinnedId("", 3), Is.EqualTo(""));
        }

        [Test]
        public void GetSaveEntries_SoloIncluyeSkinsDistintasDeCero()
        {
            profile.SetSkin("player_hero", 2);
            profile.SetSkin("hero_1", 0); // default, no deberia aparecer

            var entries = profile.GetSaveEntries();

            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(entries[0].characterId, Is.EqualTo("player_hero"));
            Assert.That(entries[0].skinIndex, Is.EqualTo(2));
        }

        [Test]
        public void LoadState_RestauraUsernameBioYSkins()
        {
            profile.LoadState("Ana", "Presentacion", new List<SkinSaveEntry>
            {
                new SkinSaveEntry { characterId = "player_hero", skinIndex = 3 },
            });

            Assert.That(profile.Username, Is.EqualTo("Ana"));
            Assert.That(profile.Bio, Is.EqualTo("Presentacion"));
            Assert.That(profile.GetSkin("player_hero"), Is.EqualTo(3));
        }

        [Test]
        public void LoadState_PublicaProfileChangedEventYSkinChangedEvent()
        {
            bool profileChanged = false;
            bool skinChanged = false;
            EventBus.Subscribe<ProfileChangedEvent>(evt => profileChanged = true);
            EventBus.Subscribe<SkinChangedEvent>(evt => skinChanged = evt.CharacterId == "");

            profile.LoadState("Ana", "", null);

            Assert.That(profileChanged, Is.True);
            Assert.That(skinChanged, Is.True);
        }
    }
}
