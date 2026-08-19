using NUnit.Framework;
using TapLegends.Core;
using TapLegends.Progression;
using UnityEngine;

namespace TapLegends.Tests
{
    public class GoldWalletTests
    {
        private GameObject go;
        private GoldWallet wallet;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("GoldWallet_Test");
            wallet = go.AddComponent<GoldWallet>();
            TestUtils.ForceSingleton(wallet);
        }

        [TearDown]
        public void TearDown()
        {
            TestUtils.ResetSingleton<GoldWallet>();
            Object.DestroyImmediate(go);
            EventBus.ClearAll();
        }

        [Test]
        public void Awake_EmpiezaEnCero()
        {
            Assert.That(wallet.Gold, Is.EqualTo(0d));
        }

        [Test]
        public void Add_SumaAlTotal()
        {
            wallet.Add(100d);
            Assert.That(wallet.Gold, Is.EqualTo(100d));
        }

        [Test]
        public void Add_MontoCeroONegativo_NoHaceNada()
        {
            wallet.Add(50d);
            wallet.Add(0d);
            wallet.Add(-10d);
            Assert.That(wallet.Gold, Is.EqualTo(50d));
        }

        [Test]
        public void Add_PublicaGoldChangedEvent()
        {
            GoldChangedEvent? received = null;
            EventBus.Subscribe<GoldChangedEvent>(evt => received = evt);

            wallet.Add(30d);

            Assert.That(received, Is.Not.Null);
            Assert.That(received.Value.NewTotal, Is.EqualTo(30d));
            Assert.That(received.Value.Delta, Is.EqualTo(30d));
        }

        [Test]
        public void TrySpend_ConFondosSuficientes_DescuentaYDevuelveTrue()
        {
            wallet.Add(100d);
            bool success = wallet.TrySpend(40d);

            Assert.That(success, Is.True);
            Assert.That(wallet.Gold, Is.EqualTo(60d));
        }

        [Test]
        public void TrySpend_ConFondosInsuficientes_NoDescuentaYDevuelveFalse()
        {
            wallet.Add(10d);
            bool success = wallet.TrySpend(50d);

            Assert.That(success, Is.False);
            Assert.That(wallet.Gold, Is.EqualTo(10d));
        }

        [Test]
        public void TrySpend_ExactamenteElSaldo_Permitido()
        {
            wallet.Add(25d);
            bool success = wallet.TrySpend(25d);

            Assert.That(success, Is.True);
            Assert.That(wallet.Gold, Is.EqualTo(0d));
        }

        [Test]
        public void LoadState_RestauraElOroSinPasarPorTrySpendOAdd()
        {
            wallet.LoadState(777d);
            Assert.That(wallet.Gold, Is.EqualTo(777d));
        }
    }
}
