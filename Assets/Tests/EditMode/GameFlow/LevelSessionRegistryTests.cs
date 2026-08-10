using NUnit.Framework;
using Train.Architecture.Events;
using Train.GameFlow.Application;
using Train.GameFlow.Application.Events;

namespace Train.Tests.EditMode.GameFlow
{
    public sealed class LevelSessionRegistryTests
    {
        [Test]
        public void AttachAndDetach_PublishSessionFacts()
        {
            using var events = new EventBus();
            var registry = new LevelSessionRegistry(events);
            var stateChanges = 0;
            var lastState = false;
            using var subscription =
                events.Subscribe<LevelSessionChangedEvent>(message =>
                {
                    stateChanges++;
                    lastState = message.HasActiveSession;
                });
            var readModel = new FakeReadModel();

            registry.Attach(readModel);

            Assert.That(registry.Current, Is.SameAs(readModel));
            Assert.That(lastState, Is.True);

            registry.Detach(readModel);

            Assert.That(registry.Current, Is.Null);
            Assert.That(lastState, Is.False);
            Assert.That(stateChanges, Is.EqualTo(2));
        }

        private sealed class FakeReadModel : ILevelReadModel
        {
            public LevelReadSnapshot Snapshot => default;
        }
    }
}
