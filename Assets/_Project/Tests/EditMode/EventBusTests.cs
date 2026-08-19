using NUnit.Framework;
using TapLegends.Core;

namespace TapLegends.Tests
{
    /// <summary>EventBus es estado estatico global — cada test se hace cargo de sacar sus
    /// propios listeners (o llama ClearAll) para no dejar nada suscrito que contamine el
    /// siguiente test.</summary>
    public class EventBusTests
    {
        private struct TestEvent
        {
            public int Value;
        }

        [TearDown]
        public void TearDown() => EventBus.ClearAll();

        [Test]
        public void Publish_LlamaAlListenerSuscripto()
        {
            int received = -1;
            void Handler(TestEvent evt) => received = evt.Value;

            EventBus.Subscribe<TestEvent>(Handler);
            EventBus.Publish(new TestEvent { Value = 42 });

            Assert.That(received, Is.EqualTo(42));
        }

        [Test]
        public void Publish_SinListeners_NoTiraExcepcion()
        {
            Assert.DoesNotThrow(() => EventBus.Publish(new TestEvent { Value = 1 }));
        }

        [Test]
        public void Unsubscribe_DejaDeRecibirEventos()
        {
            int callCount = 0;
            void Handler(TestEvent evt) => callCount++;

            EventBus.Subscribe<TestEvent>(Handler);
            EventBus.Publish(new TestEvent());
            EventBus.Unsubscribe<TestEvent>(Handler);
            EventBus.Publish(new TestEvent());

            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public void Publish_NotificaATodosLosListenersSuscriptos()
        {
            int calls = 0;
            void HandlerA(TestEvent evt) => calls++;
            void HandlerB(TestEvent evt) => calls++;

            EventBus.Subscribe<TestEvent>(HandlerA);
            EventBus.Subscribe<TestEvent>(HandlerB);
            EventBus.Publish(new TestEvent());

            Assert.That(calls, Is.EqualTo(2));
        }

        [Test]
        public void ClearAll_SacaTodosLosListeners()
        {
            int callCount = 0;
            EventBus.Subscribe<TestEvent>(evt => callCount++);

            EventBus.ClearAll();
            EventBus.Publish(new TestEvent());

            Assert.That(callCount, Is.EqualTo(0));
        }
    }
}
