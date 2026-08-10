using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Train.GameFlow.Core;

namespace Train.Tests.EditMode.GameFlow
{
    public sealed class LevelFlowStateMachineTests
    {
        [Test]
        public async Task ClearRoute_RunsActionsInLifecycleOrder()
        {
            var actions = new RecordingActions();
            var machine = new LevelFlowStateMachine(actions);
            var changes = new List<LevelPhaseChanged>();
            machine.PhaseChanged += changes.Add;

            await machine.StartAsync();
            await machine.ShowIntroAsync();
            await machine.BeginCombatAsync();
            await machine.CompleteAsync();
            await machine.ExitAsync();

            Assert.That(
                actions.Calls,
                Is.EqualTo(
                    new[]
                    {
                        "prepare",
                        "intro",
                        "combat:on",
                        "combat:off",
                        "result:Cleared",
                        "exit"
                    }));
            Assert.That(machine.CurrentPhase, Is.EqualTo(LevelPhase.Exiting));
            Assert.That(machine.Outcome, Is.EqualTo(LevelOutcome.Cleared));
            Assert.That(changes.Count, Is.EqualTo(5));
            Assert.That(changes[3].Previous, Is.EqualTo(LevelPhase.Combat));
            Assert.That(changes[3].Current, Is.EqualTo(LevelPhase.Cleared));
        }

        [Test]
        public async Task FailRoute_PreservesFailedOutcomeWhileExiting()
        {
            var actions = new RecordingActions();
            var machine = new LevelFlowStateMachine(actions);

            await machine.StartAsync();
            await machine.ShowIntroAsync();
            await machine.BeginCombatAsync();
            await machine.FailAsync();
            await machine.ExitAsync();

            Assert.That(machine.CurrentPhase, Is.EqualTo(LevelPhase.Exiting));
            Assert.That(machine.Outcome, Is.EqualTo(LevelOutcome.Failed));
            Assert.That(actions.Calls, Does.Contain("result:Failed"));
        }

        [Test]
        public async Task SamePhaseRequest_IsIgnoredWithoutRepeatingSideEffects()
        {
            var actions = new RecordingActions();
            var machine = new LevelFlowStateMachine(actions);
            var eventCount = 0;
            machine.PhaseChanged += _ => eventCount++;

            var first = await machine.StartAsync();
            var duplicate = await machine.StartAsync();

            Assert.That(first, Is.EqualTo(LevelTransitionResult.Completed));
            Assert.That(
                duplicate,
                Is.EqualTo(LevelTransitionResult.IgnoredSamePhase));
            Assert.That(actions.Calls, Is.EqualTo(new[] { "prepare" }));
            Assert.That(eventCount, Is.EqualTo(1));
        }

        [Test]
        public async Task InvalidTransition_ThrowsAndKeepsCurrentPhase()
        {
            var actions = new RecordingActions();
            var machine = new LevelFlowStateMachine(actions);
            await machine.StartAsync();

            var exception =
                Assert.ThrowsAsync<InvalidLevelTransitionException>(
                    async () => await machine.BeginCombatAsync());

            Assert.That(exception.From, Is.EqualTo(LevelPhase.Preparing));
            Assert.That(exception.To, Is.EqualTo(LevelPhase.Combat));
            Assert.That(
                machine.CurrentPhase,
                Is.EqualTo(LevelPhase.Preparing));
            Assert.That(actions.Calls, Is.EqualTo(new[] { "prepare" }));
        }

        [Test]
        public void PreCancelledStart_DoesNotMutateTheSession()
        {
            var actions = new RecordingActions();
            var machine = new LevelFlowStateMachine(actions);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            Assert.ThrowsAsync<TaskCanceledException>(
                async () => await machine.StartAsync(cancellation.Token));

            Assert.That(machine.CurrentPhase, Is.EqualTo(LevelPhase.None));
            Assert.That(actions.Calls, Is.Empty);
        }

        [Test]
        public async Task CancelledEnter_RestoresPreviousStableState()
        {
            using var cancellation = new CancellationTokenSource();
            var actions = new RecordingActions
            {
                IntroCancellation = cancellation
            };
            var machine = new LevelFlowStateMachine(actions);
            var eventCount = 0;
            machine.PhaseChanged += _ => eventCount++;
            await machine.StartAsync();

            Assert.ThrowsAsync<TaskCanceledException>(
                async () =>
                    await machine.ShowIntroAsync(cancellation.Token));

            Assert.That(
                machine.CurrentPhase,
                Is.EqualTo(LevelPhase.Preparing));
            Assert.That(
                actions.Calls,
                Is.EqualTo(new[] { "prepare", "intro", "prepare" }));
            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(machine.IsFaulted, Is.False);
        }

        private sealed class RecordingActions : ILevelFlowActions
        {
            public List<string> Calls { get; } = new List<string>();

            public CancellationTokenSource IntroCancellation { get; set; }

            public Task PrepareAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Calls.Add("prepare");
                return Task.CompletedTask;
            }

            public Task PlayIntroAsync(CancellationToken cancellationToken)
            {
                Calls.Add("intro");
                if (IntroCancellation == null)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return Task.CompletedTask;
                }

                IntroCancellation.Cancel();
                return Task.FromCanceled(cancellationToken);
            }

            public Task SetCombatEnabledAsync(
                bool enabled,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Calls.Add(enabled ? "combat:on" : "combat:off");
                return Task.CompletedTask;
            }

            public Task PresentResultAsync(
                LevelOutcome outcome,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Calls.Add($"result:{outcome}");
                return Task.CompletedTask;
            }

            public Task ExitAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Calls.Add("exit");
                return Task.CompletedTask;
            }
        }
    }
}
