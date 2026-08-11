using System;
using System.Collections.Generic;
using NUnit.Framework;
using Train.Buffs.Core;

namespace Train.Tests.EditMode.Buffs
{
    /// <summary>
    /// Buff 工厂、句柄生命周期、叠层和雷易伤计算的领域测试。
    /// </summary>
    public sealed class BuffHandleTests
    {
        private const string SourceA = "player_ellen";
        private const string SourceB = "player_anby";

        [Test]
        public void FactoryRegistry_CreatesRegisteredLightningBuff()
        {
            var registry =
                BuffFactoryRegistry.CreateWithBuiltIns();
            var info = CreateLightningInfo(
                sourceId: SourceA);

            var created = registry.Create(info);

            Assert.That(registry.Count, Is.GreaterThanOrEqualTo(9));
            Assert.That(created, Is.TypeOf<
                LightningVulnerabilityBuff>());
            Assert.That(
                created.BuffId,
                Is.EqualTo(
                    LightningVulnerabilityBuff.StableId));
            Assert.That(
                created.SourceId,
                Is.EqualTo(SourceA));
        }

        [Test]
        public void FactoryRegistry_RejectsDuplicateFactory()
        {
            var registry = new BuffFactoryRegistry();
            registry.Register(
                new LightningVulnerabilityBuffFactory());

            var exception = Assert.Throws<
                InvalidOperationException>(
                () => registry.Register(
                    new LightningVulnerabilityBuffFactory()));

            StringAssert.Contains(
                LightningVulnerabilityBuff.StableId,
                exception.Message);
        }

        [Test]
        public void FactoryRegistry_UnknownIdFailsClearly()
        {
            var registry = new BuffFactoryRegistry();
            var info = new BuffInfo(
                "missing_buff",
                SourceA,
                3f,
                0.1f,
                1,
                1);

            var exception = Assert.Throws<
                KeyNotFoundException>(
                () => registry.Create(info));

            StringAssert.Contains(
                "missing_buff",
                exception.Message);
            Assert.That(
                registry.TryCreate(info, out _),
                Is.False);
        }

        [Test]
        public void Apply_FirstApplicationCreatesOneStack()
        {
            var handle = CreateHandle();

            var applied = handle.Apply(
                CreateLightningInfo(
                    sourceId: SourceA));

            Assert.That(handle.Count, Is.EqualTo(1));
            Assert.That(handle.Revision, Is.EqualTo(1));
            Assert.That(applied.StackCount, Is.EqualTo(1));
            Assert.That(
                applied.RemainingDuration,
                Is.EqualTo(5f));
            Assert.That(
                handle.Snapshot.TryGet(
                    LightningVulnerabilityBuff.StableId,
                    SourceA,
                    out var snapshot),
                Is.True);
            Assert.That(snapshot.StackCount, Is.EqualTo(1));
        }

        [Test]
        public void Apply_RepeatedHitAddsStacks()
        {
            var handle = CreateHandle();
            handle.Apply(
                CreateLightningInfo(
                    sourceId: SourceA));

            var stacked = handle.Apply(
                CreateLightningInfo(
                    sourceId: SourceA,
                    stackAmount: 2));

            Assert.That(stacked.StackCount, Is.EqualTo(3));
            Assert.That(handle.Count, Is.EqualTo(1));
            Assert.That(handle.Revision, Is.EqualTo(2));
        }

        [Test]
        public void Apply_RepeatedHitNeverExceedsMaxStacks()
        {
            var handle = CreateHandle();
            handle.Apply(
                CreateLightningInfo(
                    sourceId: SourceA,
                    stackAmount: 4));

            var capped = handle.Apply(
                CreateLightningInfo(
                    sourceId: SourceA,
                    stackAmount: 4));

            Assert.That(capped.StackCount, Is.EqualTo(5));
            Assert.That(capped.MaxStacks, Is.EqualTo(5));
        }

        [Test]
        public void Apply_RepeatedHitRefreshesRemainingDuration()
        {
            var handle = CreateHandle();
            handle.Apply(
                CreateLightningInfo(
                    sourceId: SourceA,
                    duration: 5f));
            handle.Tick(4f);

            var refreshed = handle.Apply(
                CreateLightningInfo(
                    sourceId: SourceA,
                    duration: 8f));

            Assert.That(
                refreshed.RemainingDuration,
                Is.EqualTo(8f));
            Assert.That(refreshed.StackCount, Is.EqualTo(2));
        }

        [Test]
        public void Tick_WhenDurationEndsRemovesBuff()
        {
            var handle = CreateHandle();
            handle.Apply(
                CreateLightningInfo(
                    sourceId: SourceA,
                    duration: 2f));

            handle.Tick(2f);

            Assert.That(handle.Count, Is.Zero);
            Assert.That(handle.Revision, Is.EqualTo(2));
            Assert.That(
                handle.TryGetSnapshot(
                    LightningVulnerabilityBuff.StableId,
                    SourceA,
                    out _),
                Is.False);
        }

        [Test]
        public void ModifyIncomingDamage_PhysicalDamageIsUnchanged()
        {
            var handle = CreateHandle();
            ApplyFiveLightningStacks(handle, SourceA);
            var original = new DamageContext(
                100f,
                DamageElement.Physical);

            var modified =
                handle.ModifyIncomingDamage(original);

            Assert.That(modified.Amount, Is.EqualTo(100f));
            Assert.That(
                modified.Element,
                Is.EqualTo(DamageElement.Physical));
        }

        [Test]
        public void ModifyIncomingDamage_FiveTwelvePercentStacksAddSixtyPercent()
        {
            var handle = CreateHandle();
            ApplyFiveLightningStacks(handle, SourceA);
            var original = new DamageContext(
                100f,
                DamageElement.Electric);

            var modified =
                handle.ModifyIncomingDamage(original);

            Assert.That(
                modified.Amount,
                Is.EqualTo(160f).Within(0.0001f));
            Assert.That(
                modified.Element,
                Is.EqualTo(DamageElement.Electric));
        }

        [Test]
        public void Handles_OnDifferentEntitiesNeverShareState()
        {
            var enemyA = CreateHandle();
            var enemyB = CreateHandle();
            ApplyFiveLightningStacks(enemyA, SourceA);

            var damage = new DamageContext(
                100f,
                DamageElement.Electric);

            Assert.That(
                enemyA.ModifyIncomingDamage(damage).Amount,
                Is.EqualTo(160f).Within(0.0001f));
            Assert.That(
                enemyB.ModifyIncomingDamage(damage).Amount,
                Is.EqualTo(100f));
            Assert.That(enemyA.Count, Is.EqualTo(1));
            Assert.That(enemyB.Count, Is.Zero);
        }

        [Test]
        public void Remove_UsesBothBuffAndSourceIdentity()
        {
            var handle = CreateHandle();
            handle.Apply(
                CreateLightningInfo(SourceA));
            handle.Apply(
                CreateLightningInfo(SourceB));

            var removed = handle.Remove(
                LightningVulnerabilityBuff.StableId,
                SourceA);

            Assert.That(removed, Is.True);
            Assert.That(handle.Count, Is.EqualTo(1));
            Assert.That(
                handle.TryGetSnapshot(
                    LightningVulnerabilityBuff.StableId,
                    SourceA,
                    out _),
                Is.False);
            Assert.That(
                handle.TryGetSnapshot(
                    LightningVulnerabilityBuff.StableId,
                    SourceB,
                    out var remaining),
                Is.True);
            Assert.That(remaining.SourceId, Is.EqualTo(SourceB));
            Assert.That(
                handle.Remove(
                    LightningVulnerabilityBuff.StableId,
                    SourceA),
                Is.False);
        }

        [Test]
        public void RemoveAllFromSource_OnlyRemovesMatchingSource()
        {
            var handle = CreateHandle();
            handle.Apply(CreateLightningInfo(SourceA));
            handle.Apply(CreateLightningInfo(SourceB));

            var removedCount =
                handle.RemoveAllFromSource(SourceA);

            Assert.That(removedCount, Is.EqualTo(1));
            Assert.That(handle.Count, Is.EqualTo(1));
            Assert.That(
                handle.Snapshot.Buffs[0].SourceId,
                Is.EqualTo(SourceB));
        }

        [Test]
        public void ChangedAndRevision_TrackSemanticChangesOnly()
        {
            var handle = CreateHandle();
            var events =
                new List<BuffChangedEventArgs>();
            handle.Changed +=
                (_, args) => events.Add(args);

            handle.Apply(CreateLightningInfo(SourceA));
            handle.Tick(1f);
            handle.Apply(CreateLightningInfo(SourceA));
            handle.Remove(
                LightningVulnerabilityBuff.StableId,
                SourceA);

            Assert.That(handle.Revision, Is.EqualTo(3));
            Assert.That(events.Count, Is.EqualTo(3));
            Assert.That(
                events[0].Kind,
                Is.EqualTo(BuffChangeKind.Applied));
            Assert.That(
                events[0].Revision,
                Is.EqualTo(1));
            Assert.That(
                events[1].Kind,
                Is.EqualTo(BuffChangeKind.Stacked));
            Assert.That(
                events[2].Kind,
                Is.EqualTo(BuffChangeKind.Removed));
            Assert.That(
                events[2].HandleSnapshot.Count,
                Is.Zero);
        }

        [Test]
        public void Changed_ExpirationPublishesExpiredState()
        {
            var handle = CreateHandle();
            BuffChangedEventArgs lastEvent = null;
            handle.Changed += (_, args) =>
                lastEvent = args;
            handle.Apply(
                CreateLightningInfo(
                    SourceA,
                    duration: 1f));

            handle.Tick(1f);

            Assert.That(lastEvent, Is.Not.Null);
            Assert.That(
                lastEvent.Kind,
                Is.EqualTo(BuffChangeKind.Expired));
            Assert.That(
                lastEvent.ChangedBuff.RemainingDuration,
                Is.Zero);
            Assert.That(
                lastEvent.HandleSnapshot.Count,
                Is.Zero);
        }

        [Test]
        public void Snapshot_IsPointInTime()
        {
            var handle = CreateHandle();
            handle.Apply(CreateLightningInfo(SourceA));
            var beforeTick = handle.Snapshot;

            handle.Tick(2f);
            var afterTick = handle.Snapshot;

            Assert.That(
                beforeTick.Buffs[0].RemainingDuration,
                Is.EqualTo(5f));
            Assert.That(
                afterTick.Buffs[0].RemainingDuration,
                Is.EqualTo(3f));
            Assert.That(
                beforeTick.Revision,
                Is.EqualTo(afterTick.Revision));
        }

        [Test]
        public void InvalidInfoAndTickAreRejectedBeforeMutation()
        {
            var handle = CreateHandle();

            Assert.Throws<ArgumentException>(
                () => handle.Apply(default));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => handle.Tick(-0.1f));

            Assert.That(handle.Count, Is.Zero);
            Assert.That(handle.Revision, Is.Zero);
        }

        private static BuffHandle CreateHandle()
        {
            return new BuffHandle(
                BuffFactoryRegistry.CreateWithBuiltIns());
        }

        private static BuffInfo CreateLightningInfo(
            string sourceId,
            float duration = 5f,
            float magnitude = 0.12f,
            int stackAmount = 1,
            int maxStacks = 5)
        {
            return new BuffInfo(
                LightningVulnerabilityBuff.StableId,
                sourceId,
                duration,
                magnitude,
                stackAmount,
                maxStacks);
        }

        private static void ApplyFiveLightningStacks(
            BuffHandle handle,
            string sourceId)
        {
            handle.Apply(
                CreateLightningInfo(
                    sourceId,
                    stackAmount: 5));
        }
    }
}
