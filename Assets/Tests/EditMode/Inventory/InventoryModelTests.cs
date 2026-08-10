using System;
using System.Collections.Generic;
using NUnit.Framework;
using Train.Inventory.Core;

namespace Train.Tests.EditMode.Inventory
{
    public sealed class InventoryModelTests
    {
        [Test]
        public void Add_CrossesStacksAndKeepsAscendingSlotOrder()
        {
            var inventory = CreateInventory(capacity: 3, maxStack: 5);

            Assert.That(inventory.TryAdd("potion", 12), Is.True);

            var snapshot = inventory.Snapshot;
            Assert.That(snapshot.Capacity, Is.EqualTo(3));
            Assert.That(snapshot.OccupiedSlotCount, Is.EqualTo(3));
            AssertSlot(snapshot, 0, "potion", 5);
            AssertSlot(snapshot, 1, "potion", 5);
            AssertSlot(snapshot, 2, "potion", 2);
        }

        [Test]
        public void Add_WhenCapacityIsInsufficient_RollsBackEverySlot()
        {
            var inventory = CreateInventory(capacity: 2, maxStack: 5);
            Assert.That(inventory.TryAdd("potion", 5), Is.True);
            Assert.That(inventory.TryAdd("coin", 4), Is.True);
            var before = inventory.Snapshot;
            var eventCount = 0;
            inventory.Changed += (_, __) => eventCount++;

            Assert.That(inventory.TryAdd("coin", 2), Is.False);

            var after = inventory.Snapshot;
            AssertSnapshotsEqual(before, after);
            Assert.That(after.Revision, Is.EqualTo(2));
            Assert.That(eventCount, Is.Zero);
        }

        [Test]
        public void Remove_CrossesStacksFromTheLastSlotFirst()
        {
            var inventory = CreateInventory(capacity: 3, maxStack: 5);
            Assert.That(inventory.TryAdd("potion", 12), Is.True);

            Assert.That(inventory.TryRemove("potion", 7), Is.True);

            var snapshot = inventory.Snapshot;
            AssertSlot(snapshot, 0, "potion", 5);
            AssertEmpty(snapshot, 1);
            AssertEmpty(snapshot, 2);
            Assert.That(snapshot.GetTotalQuantity("potion"), Is.EqualTo(5));
        }

        [Test]
        public void Remove_WhenQuantityIsInsufficient_RollsBack()
        {
            var inventory = CreateInventory(capacity: 2, maxStack: 5);
            Assert.That(inventory.TryAdd("potion", 4), Is.True);
            var before = inventory.Snapshot;

            Assert.That(inventory.TryRemove("potion", 5), Is.False);

            AssertSnapshotsEqual(before, inventory.Snapshot);
        }

        [Test]
        public void Add_ReusesTheLowestEmptySlot()
        {
            var inventory = CreateInventory(capacity: 2, maxStack: 2);
            Assert.That(inventory.TryAdd("potion", 2), Is.True);
            Assert.That(inventory.TryAdd("ore", 2), Is.True);
            Assert.That(inventory.TryRemove("potion", 2), Is.True);

            Assert.That(inventory.TryAdd("herb", 1), Is.True);

            var snapshot = inventory.Snapshot;
            AssertSlot(snapshot, 0, "herb", 1);
            AssertSlot(snapshot, 1, "ore", 2);
        }

        [Test]
        public void RepeatedOperations_UpdateQuantityRevisionAndEventsOnceEach()
        {
            var inventory = CreateInventory(capacity: 2, maxStack: 5);
            var events = new List<InventoryChangedEventArgs>();
            inventory.Changed += (_, args) => events.Add(args);

            Assert.That(inventory.TryAdd("coin", 2), Is.True);
            Assert.That(inventory.TryAdd("coin", 3), Is.True);
            Assert.That(inventory.TryRemove("coin", 1), Is.True);
            Assert.That(inventory.TryRemove("coin", 99), Is.False);

            Assert.That(inventory.GetTotalQuantity("coin"), Is.EqualTo(4));
            Assert.That(inventory.Revision, Is.EqualTo(3));
            Assert.That(events.Count, Is.EqualTo(3));
            Assert.That(events[0].Kind, Is.EqualTo(InventoryChangeKind.Added));
            Assert.That(events[1].Snapshot.Revision, Is.EqualTo(2));
            Assert.That(events[2].Kind, Is.EqualTo(InventoryChangeKind.Removed));
            Assert.That(events[2].Quantity, Is.EqualTo(1));
        }

        [Test]
        public void Snapshot_IsPointInTimeAndCannotBeMutatedByLaterOperations()
        {
            var inventory = CreateInventory(capacity: 2, maxStack: 5);
            Assert.That(inventory.TryAdd("coin", 2), Is.True);
            var oldSnapshot = inventory.Snapshot;

            Assert.That(inventory.TryAdd("coin", 3), Is.True);

            Assert.That(oldSnapshot.GetTotalQuantity("coin"), Is.EqualTo(2));
            Assert.That(inventory.Snapshot.GetTotalQuantity("coin"), Is.EqualTo(5));
            Assert.That(
                oldSnapshot.Slots,
                Is.AssignableTo<IReadOnlyList<InventorySlotSnapshot>>());
            Assert.That(
                oldSnapshot.Slots,
                Is.Not.AssignableTo<InventorySlotSnapshot[]>());
        }

        [Test]
        public void InvalidArguments_AreRejectedBeforeStateChanges()
        {
            var inventory = CreateInventory(capacity: 2, maxStack: 5);

            Assert.Throws<ArgumentException>(
                () => inventory.TryAdd(" ", 1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => inventory.TryAdd("coin", 0));
            Assert.Throws<ArgumentException>(
                () => inventory.TryRemove(null, 1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => inventory.TryRemove("coin", -1));
            Assert.That(inventory.Revision, Is.Zero);
            Assert.That(inventory.Snapshot.OccupiedSlotCount, Is.Zero);
        }

        [Test]
        public void ConstructorAndStackRule_ValidateTheirContracts()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new InventoryModel(0, new ConstantLimitProvider(5)));
            Assert.Throws<ArgumentNullException>(
                () => new InventoryModel(1, null));

            var invalidRuleInventory =
                CreateInventory(capacity: 1, maxStack: 0);
            Assert.Throws<InvalidOperationException>(
                () => invalidRuleInventory.TryAdd("coin", 1));
            Assert.That(invalidRuleInventory.Revision, Is.Zero);
            Assert.That(
                invalidRuleInventory.Snapshot.OccupiedSlotCount,
                Is.Zero);
        }

        [Test]
        public void PerItemStackRule_IsInjected()
        {
            var provider = new DictionaryLimitProvider(
                new Dictionary<string, int>
                {
                    ["potion"] = 3,
                    ["coin"] = 99
                });
            var inventory = new InventoryModel(3, provider);

            Assert.That(inventory.TryAdd("potion", 4), Is.True);
            Assert.That(inventory.TryAdd("coin", 50), Is.True);

            var snapshot = inventory.Snapshot;
            AssertSlot(snapshot, 0, "potion", 3);
            AssertSlot(snapshot, 1, "potion", 1);
            AssertSlot(snapshot, 2, "coin", 50);
        }

        private static InventoryModel CreateInventory(
            int capacity,
            int maxStack)
        {
            return new InventoryModel(
                capacity,
                new ConstantLimitProvider(maxStack));
        }

        private static void AssertSlot(
            InventorySnapshot snapshot,
            int index,
            string itemId,
            int quantity)
        {
            var slot = snapshot.Slots[index];
            Assert.That(slot.SlotIndex, Is.EqualTo(index));
            Assert.That(slot.IsEmpty, Is.False);
            Assert.That(slot.ItemId, Is.EqualTo(itemId));
            Assert.That(slot.Quantity, Is.EqualTo(quantity));
        }

        private static void AssertEmpty(
            InventorySnapshot snapshot,
            int index)
        {
            var slot = snapshot.Slots[index];
            Assert.That(slot.SlotIndex, Is.EqualTo(index));
            Assert.That(slot.IsEmpty, Is.True);
            Assert.That(slot.ItemId, Is.Null);
            Assert.That(slot.Quantity, Is.Zero);
        }

        private static void AssertSnapshotsEqual(
            InventorySnapshot expected,
            InventorySnapshot actual)
        {
            Assert.That(actual.Revision, Is.EqualTo(expected.Revision));
            Assert.That(actual.Capacity, Is.EqualTo(expected.Capacity));
            for (var i = 0; i < expected.Capacity; i++)
            {
                Assert.That(
                    actual.Slots[i].SlotIndex,
                    Is.EqualTo(expected.Slots[i].SlotIndex));
                Assert.That(
                    actual.Slots[i].ItemId,
                    Is.EqualTo(expected.Slots[i].ItemId));
                Assert.That(
                    actual.Slots[i].Quantity,
                    Is.EqualTo(expected.Slots[i].Quantity));
            }
        }

        private sealed class ConstantLimitProvider :
            IItemStackLimitProvider
        {
            private readonly int _maxStack;

            public ConstantLimitProvider(int maxStack)
            {
                _maxStack = maxStack;
            }

            public int GetMaxStack(string itemId)
            {
                return _maxStack;
            }
        }

        private sealed class DictionaryLimitProvider :
            IItemStackLimitProvider
        {
            private readonly IReadOnlyDictionary<string, int> _limits;

            public DictionaryLimitProvider(
                IReadOnlyDictionary<string, int> limits)
            {
                _limits = limits;
            }

            public int GetMaxStack(string itemId)
            {
                return _limits[itemId];
            }
        }
    }
}
