using Train.Architecture.Events;
using NUnit.Framework;

namespace Train.Tests.EditMode.Architecture
{
    public sealed class EventBusTests
    {
        [Test]
        public void Publish_WhenSubscribed_DeliversMessage()
        {
            using var bus = new EventBus();
            var received = 0;
            using var subscription = bus.Subscribe<int>(value => received = value);

            bus.Publish(42);

            Assert.That(received, Is.EqualTo(42));
        }

        [Test]
        public void DisposeSubscription_StopsFutureMessages()
        {
            using var bus = new EventBus();
            var callCount = 0;
            var subscription = bus.Subscribe<int>(_ => callCount++);

            bus.Publish(1);
            subscription.Dispose();
            bus.Publish(2);

            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public void Publish_OnSceneBus_BubblesToGameBus()
        {
            using var gameBus = new EventBus();
            using var sceneBus = new EventBus(gameBus);
            var received = string.Empty;
            using var subscription =
                gameBus.Subscribe<string>(message => received = message);

            sceneBus.Publish("enemy-died");

            Assert.That(received, Is.EqualTo("enemy-died"));
        }
    }
}
