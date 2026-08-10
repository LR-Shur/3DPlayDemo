using System;
using System.Collections.Generic;
using NUnit.Framework;
using Train.WorldInteraction.Core;

namespace Train.Tests.EditMode.WorldInteraction
{
    /// <summary>
    /// 地图交互候选、稳定焦点选择和交互结果转发的纯领域测试。
    /// </summary>
    public sealed class InteractionFocusModelTests
    {
        [Test]
        public void InteractResult_DefaultIsNotMistakenForSuccess()
        {
            var result = default(InteractResult);

            Assert.That(
                result.Code,
                Is.EqualTo(InteractResultCode.Unspecified));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(
                InteractResult.Succeeded().IsSuccess,
                Is.True);
            Assert.That(
                InteractResult.InventoryFull().Code,
                Is.EqualTo(
                    InteractResultCode.InventoryFull));
        }

        [Test]
        public void Enter_FirstAvailableCandidateBecomesFocus()
        {
            var model = new InteractionFocusModel();
            var pickup = new FakeInteractable(
                "pickup_apple");

            var entered = model.Enter(
                Candidate(pickup, priority: 10, distance: 3f));

            Assert.That(entered, Is.True);
            Assert.That(model.CandidateCount, Is.EqualTo(1));
            Assert.That(model.HasFocus, Is.True);
            AssertFocus(model, "pickup_apple");
        }

        [Test]
        public void Focus_HigherPriorityWinsEvenWhenFartherAway()
        {
            var model = new InteractionFocusModel();
            var nearbyItem = new FakeInteractable("item");
            var questNpc = new FakeInteractable("quest_npc");

            model.Enter(
                Candidate(nearbyItem, priority: 10, distance: 1f));
            model.Enter(
                Candidate(questNpc, priority: 100, distance: 8f));

            AssertFocus(model, "quest_npc");
        }

        [Test]
        public void Focus_SamePriorityChoosesNearestCandidate()
        {
            var model = new InteractionFocusModel();
            var far = new FakeInteractable("far");
            var near = new FakeInteractable("near");

            model.Enter(
                Candidate(far, priority: 10, distance: 5f));
            model.Enter(
                Candidate(near, priority: 10, distance: 1.5f));

            AssertFocus(model, "near");
        }

        [Test]
        public void Focus_ExactTieUsesStableIdRegardlessOfEnterOrder()
        {
            var firstModel = new InteractionFocusModel();
            firstModel.Enter(
                Candidate(
                    new FakeInteractable("pickup_b"),
                    priority: 10,
                    distance: 2f));
            firstModel.Enter(
                Candidate(
                    new FakeInteractable("pickup_a"),
                    priority: 10,
                    distance: 2f));

            var secondModel = new InteractionFocusModel();
            secondModel.Enter(
                Candidate(
                    new FakeInteractable("pickup_a"),
                    priority: 10,
                    distance: 2f));
            secondModel.Enter(
                Candidate(
                    new FakeInteractable("pickup_b"),
                    priority: 10,
                    distance: 2f));

            AssertFocus(firstModel, "pickup_a");
            AssertFocus(secondModel, "pickup_a");
        }

        [Test]
        public void Focus_UnavailableCandidateIsStoredButSkipped()
        {
            var model = new InteractionFocusModel();
            var unavailable =
                new FakeInteractable("empty_chest")
                {
                    IsAvailable = false
                };

            model.Enter(
                Candidate(
                    unavailable,
                    priority: 999,
                    distance: 0f));

            Assert.That(model.CandidateCount, Is.EqualTo(1));
            Assert.That(model.HasFocus, Is.False);
            Assert.That(
                model.TryGetCandidate(
                    "empty_chest",
                    out var stored),
                Is.True);
            Assert.That(stored.IsAvailable, Is.False);
        }

        [Test]
        public void Refresh_AvailabilityChangeSelectsNextCandidate()
        {
            var model = new InteractionFocusModel();
            var high = new FakeInteractable("high");
            var low = new FakeInteractable("low");
            model.Enter(
                Candidate(high, priority: 100, distance: 2f));
            model.Enter(
                Candidate(low, priority: 10, distance: 2f));
            high.IsAvailable = false;

            var changed = model.Refresh();

            Assert.That(changed, Is.True);
            AssertFocus(model, "low");
            Assert.That(model.Revision, Is.EqualTo(3));
        }

        [Test]
        public void Exit_FocusedCandidateFallsBackToNextBest()
        {
            var model = new InteractionFocusModel();
            model.Enter(
                Candidate(
                    new FakeInteractable("first"),
                    priority: 20,
                    distance: 1f));
            model.Enter(
                Candidate(
                    new FakeInteractable("second"),
                    priority: 10,
                    distance: 1f));

            var exited = model.Exit("first");

            Assert.That(exited, Is.True);
            Assert.That(model.CandidateCount, Is.EqualTo(1));
            AssertFocus(model, "second");
        }

        [Test]
        public void Exit_UnknownCandidateDoesNotChangeRevision()
        {
            var model = new InteractionFocusModel();
            model.Enter(
                Candidate(
                    new FakeInteractable("known"),
                    priority: 10,
                    distance: 1f));
            var revisionBefore = model.Revision;

            var exited = model.Exit("missing");

            Assert.That(exited, Is.False);
            Assert.That(
                model.Revision,
                Is.EqualTo(revisionBefore));
            AssertFocus(model, "known");
        }

        [Test]
        public void Update_NewDistanceCanChangeFocus()
        {
            var model = new InteractionFocusModel();
            var first = new FakeInteractable("first");
            var second = new FakeInteractable("second");
            model.Enter(
                Candidate(first, priority: 10, distance: 2f));
            model.Enter(
                Candidate(second, priority: 10, distance: 4f));
            AssertFocus(model, "first");

            var updated = model.Update(
                Candidate(first, priority: 10, distance: 8f));

            Assert.That(updated, Is.True);
            AssertFocus(model, "second");
            Assert.That(model.Revision, Is.EqualTo(3));
        }

        [Test]
        public void Enter_SameInstanceTwiceDoesNotDuplicateCandidate()
        {
            var model = new InteractionFocusModel();
            var pickup = new FakeInteractable("pickup");
            Assert.That(
                model.Enter(
                    Candidate(
                        pickup,
                        priority: 10,
                        distance: 2f)),
                Is.True);
            var revisionBefore = model.Revision;

            var enteredAgain = model.Enter(
                Candidate(
                    pickup,
                    priority: 20,
                    distance: 1f));

            Assert.That(enteredAgain, Is.False);
            Assert.That(model.CandidateCount, Is.EqualTo(1));
            Assert.That(
                model.Revision,
                Is.EqualTo(revisionBefore));
        }

        [Test]
        public void Enter_DifferentInstanceWithSameIdFailsClearly()
        {
            var model = new InteractionFocusModel();
            model.Enter(
                Candidate(
                    new FakeInteractable("duplicate"),
                    priority: 10,
                    distance: 2f));

            var exception = Assert.Throws<
                InvalidOperationException>(
                () => model.Enter(
                    Candidate(
                        new FakeInteractable("duplicate"),
                        priority: 20,
                        distance: 1f)));

            StringAssert.Contains(
                "duplicate",
                exception.Message);
            Assert.That(model.CandidateCount, Is.EqualTo(1));
        }

        [Test]
        public void InteractFocused_WithoutFocusReturnsNoCandidate()
        {
            var model = new InteractionFocusModel();

            var result = model.InteractFocused();

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(
                result.Code,
                Is.EqualTo(
                    InteractResultCode.NoFocusedCandidate));
        }

        [Test]
        public void InteractFocused_ForwardsSuccessfulResult()
        {
            var model = new InteractionFocusModel();
            var pickup = new FakeInteractable("pickup")
            {
                NextResult =
                    InteractResult.Succeeded("获得训练芯片")
            };
            model.Enter(
                Candidate(pickup, priority: 10, distance: 1f));

            var result = model.InteractFocused();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(
                result.Message,
                Is.EqualTo("获得训练芯片"));
            Assert.That(pickup.InteractCount, Is.EqualTo(1));
        }

        [Test]
        public void InteractFocused_ForwardsInventoryFullAndKeepsFocus()
        {
            var model = new InteractionFocusModel();
            var pickup = new FakeInteractable("pickup")
            {
                NextResult =
                    InteractResult.InventoryFull("背包已满")
            };
            model.Enter(
                Candidate(pickup, priority: 10, distance: 1f));

            var result = model.InteractFocused();

            Assert.That(
                result.Code,
                Is.EqualTo(InteractResultCode.InventoryFull));
            Assert.That(pickup.InteractCount, Is.EqualTo(1));
            AssertFocus(model, "pickup");
        }

        [Test]
        public void InteractFocused_OneShotPickupSwitchesToNextFocus()
        {
            var model = new InteractionFocusModel();
            var oneShot = new FakeInteractable("one_shot")
            {
                BecomesUnavailableOnInteract = true
            };
            var next = new FakeInteractable("next");
            model.Enter(
                Candidate(oneShot, priority: 20, distance: 1f));
            model.Enter(
                Candidate(next, priority: 10, distance: 1f));

            var result = model.InteractFocused();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(oneShot.IsAvailable, Is.False);
            AssertFocus(model, "next");
            Assert.That(model.Revision, Is.EqualTo(3));
        }

        [Test]
        public void Models_ForDifferentPlayersNeverShareCandidates()
        {
            var playerA = new InteractionFocusModel();
            var playerB = new InteractionFocusModel();
            playerA.Enter(
                Candidate(
                    new FakeInteractable("pickup"),
                    priority: 10,
                    distance: 1f));

            Assert.That(playerA.CandidateCount, Is.EqualTo(1));
            Assert.That(playerA.HasFocus, Is.True);
            Assert.That(playerB.CandidateCount, Is.Zero);
            Assert.That(playerB.HasFocus, Is.False);
        }

        [Test]
        public void Candidate_InvalidDistanceAndDefaultAreRejected()
        {
            var pickup = new FakeInteractable("pickup");
            Assert.Throws<ArgumentOutOfRangeException>(
                () => Candidate(
                    pickup,
                    priority: 10,
                    distance: -1f));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => Candidate(
                    pickup,
                    priority: 10,
                    distance: float.PositiveInfinity));

            var model = new InteractionFocusModel();
            Assert.Throws<ArgumentException>(
                () => model.Enter(default));
            Assert.That(model.Revision, Is.Zero);
        }

        [Test]
        public void FocusChangedAndRevision_OnlyReportIdentityChanges()
        {
            var model = new InteractionFocusModel();
            var events = new List<
                InteractionFocusChangedEventArgs>();
            model.FocusChanged +=
                (_, args) => events.Add(args);
            var high = new FakeInteractable("high");
            var low = new FakeInteractable("low");

            model.Enter(
                Candidate(high, priority: 20, distance: 1f));
            model.Enter(
                Candidate(low, priority: 10, distance: 2f));
            model.Update(
                Candidate(low, priority: 10, distance: 3f));
            model.Exit("high");

            Assert.That(model.Revision, Is.EqualTo(4));
            Assert.That(events.Count, Is.EqualTo(2));
            Assert.That(events[0].HadPrevious, Is.False);
            Assert.That(events[0].HasCurrent, Is.True);
            Assert.That(
                events[0].CurrentCandidate.Value.InteractionId,
                Is.EqualTo("high"));
            Assert.That(events[0].Revision, Is.EqualTo(1));
            Assert.That(
                events[1].PreviousCandidate.Value.InteractionId,
                Is.EqualTo("high"));
            Assert.That(
                events[1].CurrentCandidate.Value.InteractionId,
                Is.EqualTo("low"));
            Assert.That(events[1].Revision, Is.EqualTo(4));
        }

        [Test]
        public void CandidateSnapshot_IsPointInTimeAndSelectionOrdered()
        {
            var model = new InteractionFocusModel();
            model.Enter(
                Candidate(
                    new FakeInteractable("low"),
                    priority: 1,
                    distance: 1f));
            model.Enter(
                Candidate(
                    new FakeInteractable("high"),
                    priority: 99,
                    distance: 9f));
            var oldSnapshot =
                model.CreateCandidatesSnapshot();

            model.Exit("high");

            Assert.That(oldSnapshot.Count, Is.EqualTo(2));
            Assert.That(
                oldSnapshot[0].InteractionId,
                Is.EqualTo("high"));
            Assert.That(
                model.CreateCandidatesSnapshot().Count,
                Is.EqualTo(1));
            Assert.That(
                oldSnapshot,
                Is.Not.AssignableTo<
                    InteractionCandidate[]>());
        }

        private static InteractionCandidate Candidate(
            IInteractable interactable,
            int priority,
            float distance)
        {
            return new InteractionCandidate(
                interactable,
                priority,
                distance);
        }

        private static void AssertFocus(
            InteractionFocusModel model,
            string expectedId)
        {
            Assert.That(
                model.TryGetFocusedCandidate(
                    out var candidate),
                Is.True);
            Assert.That(
                candidate.InteractionId,
                Is.EqualTo(expectedId));
        }

        /// <summary>
        /// 测试使用的可交互目标，可模拟成功、背包已满和一次性失效。
        /// </summary>
        private sealed class FakeInteractable :
            IInteractable
        {
            /// <summary>
            /// 创建一个默认可用且交互成功的测试目标。
            /// </summary>
            public FakeInteractable(string interactionId)
            {
                InteractionId = interactionId;
                IsAvailable = true;
                NextResult = InteractResult.Succeeded();
            }

            /// <inheritdoc />
            public string InteractionId { get; }

            /// <inheritdoc />
            public bool IsAvailable { get; set; }

            /// <summary>
            /// 下一次交互返回的预设结果。
            /// </summary>
            public InteractResult NextResult { get; set; }

            /// <summary>
            /// 是否在交互后模拟一次性拾取物失效。
            /// </summary>
            public bool BecomesUnavailableOnInteract { get; set; }

            /// <summary>
            /// 已执行交互的次数。
            /// </summary>
            public int InteractCount { get; private set; }

            /// <inheritdoc />
            public InteractResult Interact()
            {
                if (!IsAvailable)
                {
                    return InteractResult.Unavailable();
                }

                InteractCount++;
                if (BecomesUnavailableOnInteract)
                {
                    IsAvailable = false;
                }

                return NextResult;
            }
        }
    }
}
