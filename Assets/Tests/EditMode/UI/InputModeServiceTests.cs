using System.Collections.Generic;
using NUnit.Framework;
using Train.Architecture.Events;
using Train.Architecture.Input;
using Train.Presentation.UI.Runtime;

namespace Train.Tests.EditMode.UI
{
    public sealed class InputModeServiceTests
    {
        [Test]
        public void ModalLeases_KeepGameplayBlockedUntilLastLeaseCloses()
        {
            using var events = new EventBus();
            using var service = new InputModeService(events);
            var states = new List<InputModeChangedEvent>();
            using var subscription =
                events.Subscribe<InputModeChangedEvent>(states.Add);

            var inventory = service.AcquireModal("Inventory");
            var dialogue = service.AcquireModal("Dialogue");
            inventory.Dispose();

            Assert.That(service.IsModalActive, Is.True);

            dialogue.Dispose();

            Assert.That(service.IsModalActive, Is.False);
            Assert.That(states.Count, Is.EqualTo(4));
            Assert.That(states[0].IsModalActive, Is.True);
            Assert.That(states[0].ModalCount, Is.EqualTo(1));
            Assert.That(states[1].ModalCount, Is.EqualTo(2));
            Assert.That(states[2].ModalCount, Is.EqualTo(1));
            Assert.That(states[3].IsModalActive, Is.False);
            Assert.That(states[3].ModalCount, Is.Zero);
        }

        [Test]
        public void ModalLease_DisposeIsIdempotent()
        {
            using var events = new EventBus();
            using var service = new InputModeService(events);
            var eventCount = 0;
            using var subscription =
                events.Subscribe<InputModeChangedEvent>(_ => eventCount++);
            var lease = service.AcquireModal("Inventory");

            lease.Dispose();
            lease.Dispose();

            Assert.That(eventCount, Is.EqualTo(2));
            Assert.That(service.IsModalActive, Is.False);
        }
    }
}
