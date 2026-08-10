using System;
using NUnit.Framework;
using Train.Architecture.Events;
using Train.Inventory.Application;
using Train.Inventory.Data;
using Train.Inventory.Events;
using UnityEditor;
using UnityEngine;

namespace Train.Tests.EditMode.Inventory
{
    public sealed class InventoryServiceTests
    {
        private ItemDefinition _item;
        private InventorySettings _settings;
        private EventBus _events;
        private InventoryService _service;

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
            _events?.Dispose();
            if (_settings != null)
            {
                UnityEngine.Object.DestroyImmediate(_settings);
            }

            if (_item != null)
            {
                UnityEngine.Object.DestroyImmediate(_item);
            }
        }

        [Test]
        public void Constructor_SeedsInventoryWithoutPublishingRewardEvents()
        {
            CreateService(capacity: 2, maxStack: 5, startingCount: 2);
            var acquiredCount = 0;

            // Starting data is restored before service notifications are wired.
            using var subscription =
                _events.Subscribe<ItemAcquiredEvent>(_ => acquiredCount++);

            Assert.That(_service.GetTotalQuantity("test_item"), Is.EqualTo(2));
            Assert.That(acquiredCount, Is.Zero);
        }

        [Test]
        public void TryAdd_PublishesSemanticAndSnapshotEvents()
        {
            CreateService(capacity: 2, maxStack: 5, startingCount: 1);
            ItemAcquiredEvent acquired = default;
            InventoryChangedEvent changed = default;
            using var acquiredSubscription =
                _events.Subscribe<ItemAcquiredEvent>(value => acquired = value);
            using var changedSubscription =
                _events.Subscribe<InventoryChangedEvent>(value => changed = value);

            var result = _service.TryAdd("test_item", 3);

            Assert.That(result, Is.True);
            Assert.That(acquired.ItemId, Is.EqualTo("test_item"));
            Assert.That(acquired.Quantity, Is.EqualTo(3));
            Assert.That(acquired.TotalQuantity, Is.EqualTo(4));
            Assert.That(changed.Revision, Is.EqualTo(acquired.Revision));
        }

        [Test]
        public void TryAdd_WhenCapacityIsInsufficient_IsAtomicAndSilent()
        {
            CreateService(capacity: 1, maxStack: 2, startingCount: 2);
            var eventCount = 0;
            using var subscription =
                _events.Subscribe<InventoryChangedEvent>(_ => eventCount++);
            var revision = _service.Snapshot.Revision;

            var result = _service.TryAdd("test_item", 1);

            Assert.That(result, Is.False);
            Assert.That(_service.GetTotalQuantity("test_item"), Is.EqualTo(2));
            Assert.That(_service.Snapshot.Revision, Is.EqualTo(revision));
            Assert.That(eventCount, Is.Zero);
        }

        [Test]
        public void UnknownItem_IsRejectedWithoutMutatingInventory()
        {
            CreateService(capacity: 2, maxStack: 5, startingCount: 1);
            var revision = _service.Snapshot.Revision;

            Assert.That(_service.TryAdd("missing_item", 1), Is.False);
            Assert.That(_service.TryRemove("missing_item", 1), Is.False);
            Assert.That(_service.GetTotalQuantity("missing_item"), Is.Zero);
            Assert.That(_service.Snapshot.Revision, Is.EqualTo(revision));
        }

        private void CreateService(
            int capacity,
            int maxStack,
            int startingCount)
        {
            _item = ScriptableObject.CreateInstance<ItemDefinition>();
            var itemSerialized = new SerializedObject(_item);
            itemSerialized.FindProperty("_itemId").stringValue = "test_item";
            itemSerialized.FindProperty("_displayName").stringValue =
                "Test Item";
            itemSerialized.FindProperty("_maxStack").intValue = maxStack;
            itemSerialized.ApplyModifiedPropertiesWithoutUndo();

            _settings = ScriptableObject.CreateInstance<InventorySettings>();
            var settingsSerialized = new SerializedObject(_settings);
            settingsSerialized.FindProperty("_capacity").intValue = capacity;

            var items = settingsSerialized.FindProperty("_items");
            items.arraySize = 1;
            items.GetArrayElementAtIndex(0).objectReferenceValue = _item;

            var startingItems =
                settingsSerialized.FindProperty("_startingItems");
            startingItems.arraySize = startingCount > 0 ? 1 : 0;
            if (startingCount > 0)
            {
                var startingItem = startingItems.GetArrayElementAtIndex(0);
                startingItem.FindPropertyRelative("_itemId").stringValue =
                    "test_item";
                startingItem.FindPropertyRelative("_count").intValue =
                    startingCount;
            }

            settingsSerialized.ApplyModifiedPropertiesWithoutUndo();
            _events = new EventBus();
            _service = new InventoryService(_settings, _events);
        }
    }
}
