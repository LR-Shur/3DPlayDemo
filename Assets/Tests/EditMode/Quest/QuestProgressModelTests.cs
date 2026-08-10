using System;
using System.Collections.Generic;
using NUnit.Framework;
using Train.Quest.Core;

namespace Train.Tests.EditMode.Quest
{
    /// <summary>
    /// 验证任务纯领域模型的状态转换、进度上限和不可变快照。
    /// </summary>
    public sealed class QuestProgressModelTests
    {
        [Test]
        public void NewQuest_IsInactiveWithZeroProgress()
        {
            var quest = CreateSingleObjectiveQuest(requiredAmount: 2);

            Assert.That(quest.Status, Is.EqualTo(QuestStatus.Inactive));
            Assert.That(quest.Revision, Is.Zero);
            Assert.That(quest.CanClaim, Is.False);
            Assert.That(
                quest.Snapshot.GetObjective("defeat_knights").CurrentAmount,
                Is.Zero);
        }

        [Test]
        public void Activate_ChangesStatusRevisionAndRaisesOneEvent()
        {
            var quest = CreateSingleObjectiveQuest();
            var changes = new List<QuestProgressChangedEventArgs>();
            quest.Changed += (_, args) => changes.Add(args);

            quest.Activate();

            Assert.That(quest.Status, Is.EqualTo(QuestStatus.Active));
            Assert.That(quest.Revision, Is.EqualTo(1));
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(
                changes[0].OldSnapshot.Status,
                Is.EqualTo(QuestStatus.Inactive));
            Assert.That(
                changes[0].NewSnapshot.Status,
                Is.EqualTo(QuestStatus.Active));
            Assert.That(changes[0].OldSnapshot.Revision, Is.Zero);
            Assert.That(changes[0].NewSnapshot.Revision, Is.EqualTo(1));
        }

        [Test]
        public void Activate_WhenAlreadyActivated_ThrowsWithoutChangingState()
        {
            var quest = CreateSingleObjectiveQuest();
            quest.Activate();
            var before = quest.Snapshot;
            var changeCount = 0;
            quest.Changed += (_, __) => changeCount++;

            var exception = Assert.Throws<InvalidOperationException>(
                quest.Activate);

            Assert.That(exception.Message, Does.Contain("only be activated once"));
            AssertSnapshotsEqual(before, quest.Snapshot);
            Assert.That(changeCount, Is.Zero);
        }

        [Test]
        public void ApplyFact_WithWrongTypeOrExactTarget_DoesNotChangeQuest()
        {
            var quest = CreateSingleObjectiveQuest(requiredAmount: 2);
            quest.Activate();
            var before = quest.Snapshot;
            var changeCount = 0;
            quest.Changed += (_, __) => changeCount++;

            Assert.That(
                quest.ApplyFact(
                    new QuestFact(
                        QuestFactType.ItemAcquired,
                        "enemy_knight")),
                Is.False);
            Assert.That(
                quest.ApplyFact(
                    new QuestFact(
                        QuestFactType.EnemyDefeated,
                        "enemy_knight_elite")),
                Is.False);

            AssertSnapshotsEqual(before, quest.Snapshot);
            Assert.That(changeCount, Is.Zero);
        }

        [Test]
        public void ApplyFact_AdvancesEveryMatchingObjectiveButOnlyOneRevision()
        {
            var spec = new QuestSpec(
                "quest_shared_target",
                "Shared Target",
                new[]
                {
                    new QuestObjectiveSpec(
                        "first",
                        QuestFactType.EnemyDefeated,
                        "enemy_knight",
                        2),
                    new QuestObjectiveSpec(
                        "second",
                        QuestFactType.EnemyDefeated,
                        "enemy_knight",
                        3)
                });
            var quest = new QuestProgressModel(spec);
            quest.Activate();
            var changes = new List<QuestProgressChangedEventArgs>();
            quest.Changed += (_, args) => changes.Add(args);

            Assert.That(
                quest.ApplyFact(
                    new QuestFact(
                        QuestFactType.EnemyDefeated,
                        "enemy_knight",
                        1)),
                Is.True);

            Assert.That(
                quest.Snapshot.GetObjective("first").CurrentAmount,
                Is.EqualTo(1));
            Assert.That(
                quest.Snapshot.GetObjective("second").CurrentAmount,
                Is.EqualTo(1));
            Assert.That(quest.Revision, Is.EqualTo(2));
            Assert.That(changes, Has.Count.EqualTo(1));
        }

        [Test]
        public void MultipleObjectives_MustAllCompleteBeforeQuestCompletes()
        {
            var spec = new QuestSpec(
                "quest_patrol",
                "Patrol",
                new[]
                {
                    new QuestObjectiveSpec(
                        "defeat",
                        QuestFactType.EnemyDefeated,
                        "enemy_knight",
                        2),
                    new QuestObjectiveSpec(
                        "finish",
                        QuestFactType.LevelCompleted,
                        "level_combat_001",
                        1)
                });
            var quest = new QuestProgressModel(spec);
            quest.Activate();

            Assert.That(
                quest.ApplyFact(
                    new QuestFact(
                        QuestFactType.EnemyDefeated,
                        "enemy_knight",
                        2)),
                Is.True);
            Assert.That(quest.Status, Is.EqualTo(QuestStatus.Active));
            Assert.That(quest.CanClaim, Is.False);

            Assert.That(
                quest.ApplyFact(
                    new QuestFact(
                        QuestFactType.LevelCompleted,
                        "level_combat_001")),
                Is.True);

            Assert.That(quest.Status, Is.EqualTo(QuestStatus.Completed));
            Assert.That(quest.CanClaim, Is.True);
        }

        [Test]
        public void ApplyFact_CapsProgressAtRequiredAmount()
        {
            var quest = CreateSingleObjectiveQuest(requiredAmount: 3);
            quest.Activate();

            Assert.That(
                quest.ApplyFact(
                    new QuestFact(
                        QuestFactType.EnemyDefeated,
                        "enemy_knight",
                        int.MaxValue)),
                Is.True);

            var objective = quest.Snapshot.GetObjective("defeat_knights");
            Assert.That(objective.CurrentAmount, Is.EqualTo(3));
            Assert.That(objective.IsCompleted, Is.True);
            Assert.That(quest.Status, Is.EqualTo(QuestStatus.Completed));
        }

        [Test]
        public void CompleteAndClaim_AreSingleTransitionsWithImmutableSnapshots()
        {
            var quest = CreateSingleObjectiveQuest(requiredAmount: 1);
            quest.Activate();
            var activeSnapshot = quest.Snapshot;
            var changes = new List<QuestProgressChangedEventArgs>();
            quest.Changed += (_, args) => changes.Add(args);

            quest.ApplyFact(
                new QuestFact(
                    QuestFactType.EnemyDefeated,
                    "enemy_knight"));
            var completedSnapshot = quest.Snapshot;
            quest.Claim();

            Assert.That(
                activeSnapshot.GetObjective("defeat_knights").CurrentAmount,
                Is.Zero);
            Assert.That(
                completedSnapshot.Status,
                Is.EqualTo(QuestStatus.Completed));
            Assert.That(quest.Status, Is.EqualTo(QuestStatus.Claimed));
            Assert.That(quest.CanClaim, Is.False);
            Assert.That(quest.Revision, Is.EqualTo(3));
            Assert.That(changes, Has.Count.EqualTo(2));
            Assert.That(
                changes[1].OldSnapshot.Status,
                Is.EqualTo(QuestStatus.Completed));
            Assert.That(
                changes[1].NewSnapshot.Status,
                Is.EqualTo(QuestStatus.Claimed));
        }

        [Test]
        public void Claim_BeforeCompletionOrTwice_ThrowsWithoutExtraEvent()
        {
            var quest = CreateSingleObjectiveQuest();
            quest.Activate();
            var changeCount = 0;
            quest.Changed += (_, __) => changeCount++;

            var earlyException = Assert.Throws<InvalidOperationException>(
                quest.Claim);
            Assert.That(
                earlyException.Message,
                Does.Contain("Complete every objective first"));
            Assert.That(changeCount, Is.Zero);
            Assert.That(quest.Revision, Is.EqualTo(1));

            quest.ApplyFact(
                new QuestFact(
                    QuestFactType.EnemyDefeated,
                    "enemy_knight"));
            quest.Claim();
            var claimed = quest.Snapshot;
            var eventsBeforeDuplicateClaim = changeCount;

            var duplicateException = Assert.Throws<InvalidOperationException>(
                quest.Claim);

            Assert.That(
                duplicateException.Message,
                Does.Contain("already been claimed"));
            AssertSnapshotsEqual(claimed, quest.Snapshot);
            Assert.That(changeCount, Is.EqualTo(eventsBeforeDuplicateClaim));
        }

        [Test]
        public void ApplyFact_WhenQuestIsNotActive_IsAnExplicitNoOp()
        {
            var quest = CreateSingleObjectiveQuest();
            var fact = new QuestFact(
                QuestFactType.EnemyDefeated,
                "enemy_knight");

            Assert.That(quest.ApplyFact(fact), Is.False);
            Assert.That(quest.Revision, Is.Zero);

            quest.Activate();
            quest.ApplyFact(fact);
            quest.Claim();
            var claimed = quest.Snapshot;

            Assert.That(quest.ApplyFact(fact), Is.False);
            AssertSnapshotsEqual(claimed, quest.Snapshot);
        }

        [Test]
        public void DefinitionsAndFacts_RejectInvalidContracts()
        {
            Assert.Throws<ArgumentException>(
                () => new QuestFact(
                    QuestFactType.EnemyDefeated,
                    " "));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new QuestFact(
                    QuestFactType.EnemyDefeated,
                    "enemy_knight",
                    0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new QuestObjectiveSpec(
                    "objective",
                    QuestFactType.EnemyDefeated,
                    "enemy_knight",
                    0));
            Assert.Throws<ArgumentException>(
                () => new QuestRewardSpec(" ", 1));
            Assert.Throws<ArgumentException>(
                () => new QuestSpec(
                    "quest",
                    "Quest",
                    Array.Empty<QuestObjectiveSpec>()));

            var quest = CreateSingleObjectiveQuest();
            Assert.Throws<ArgumentException>(
                () => quest.ApplyFact(default));

            var duplicated = new[]
            {
                new QuestObjectiveSpec(
                    "same",
                    QuestFactType.EnemyDefeated,
                    "a",
                    1),
                new QuestObjectiveSpec(
                    "same",
                    QuestFactType.EnemyDefeated,
                    "b",
                    1)
            };
            Assert.Throws<ArgumentException>(
                () => new QuestSpec("quest", "Quest", duplicated));
        }

        [Test]
        public void SpecAndSnapshotCollections_AreDefensiveReadOnlyCopies()
        {
            var objectives = new[]
            {
                new QuestObjectiveSpec(
                    "defeat_knights",
                    QuestFactType.EnemyDefeated,
                    "enemy_knight",
                    1)
            };
            var spec = new QuestSpec("quest", "Quest", objectives);
            objectives[0] = new QuestObjectiveSpec(
                "replacement",
                QuestFactType.ItemAcquired,
                "coin",
                1);
            var quest = new QuestProgressModel(spec);
            var snapshot = quest.Snapshot;

            Assert.That(spec.Objectives[0].Id, Is.EqualTo("defeat_knights"));
            Assert.That(
                snapshot.Objectives,
                Is.AssignableTo<
                    IReadOnlyList<QuestObjectiveProgressSnapshot>>());
            Assert.That(
                snapshot.Objectives,
                Is.Not.AssignableTo<
                    QuestObjectiveProgressSnapshot[]>());
        }

        private static QuestProgressModel CreateSingleObjectiveQuest(
            int requiredAmount = 1)
        {
            var spec = new QuestSpec(
                "quest_first_combat",
                "First Combat",
                new[]
                {
                    new QuestObjectiveSpec(
                        "defeat_knights",
                        QuestFactType.EnemyDefeated,
                        "enemy_knight",
                        requiredAmount)
                },
                new[]
                {
                    new QuestRewardSpec("city_token", 50)
                });

            return new QuestProgressModel(spec);
        }

        private static void AssertSnapshotsEqual(
            QuestProgressSnapshot expected,
            QuestProgressSnapshot actual)
        {
            Assert.That(actual.QuestId, Is.EqualTo(expected.QuestId));
            Assert.That(actual.Status, Is.EqualTo(expected.Status));
            Assert.That(actual.Revision, Is.EqualTo(expected.Revision));
            Assert.That(
                actual.Objectives.Count,
                Is.EqualTo(expected.Objectives.Count));

            for (var i = 0; i < expected.Objectives.Count; i++)
            {
                Assert.That(
                    actual.Objectives[i].ObjectiveId,
                    Is.EqualTo(expected.Objectives[i].ObjectiveId));
                Assert.That(
                    actual.Objectives[i].CurrentAmount,
                    Is.EqualTo(expected.Objectives[i].CurrentAmount));
                Assert.That(
                    actual.Objectives[i].RequiredAmount,
                    Is.EqualTo(expected.Objectives[i].RequiredAmount));
            }
        }
    }
}
