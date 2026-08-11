using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Train.Architecture.Events;
using Train.Inventory.Application;
using Train.Inventory.Core;
using Train.Inventory.Data;
using Train.Inventory.Events;
using Train.Presentation.UI.Contracts;
using Train.Presentation.UI.Presenters;
using Train.Presentation.UI.ViewModels;
using UnityEngine;

namespace Train.Tests.UI
{
    public sealed class InventoryPresenterTests
    {
        private readonly List<ItemDefinition> _createdDefinitions = new();
        private EventBus _events;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
        }

        [TearDown]
        public void TearDown()
        {
            _events.Dispose();

            foreach (var definition in _createdDefinitions)
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }

            _createdDefinitions.Clear();
        }

        [Test]
        public void Constructor_RendersFixedGridAndSelectsFirstOccupiedSlot()
        {
            var inventory = new FakeInventoryService(4);
            inventory.Register(
                CreateDefinition(
                    "training_chip",
                    "训练芯片",
                    "用于代理人训练。",
                    99,
                    ItemCategory.Material,
                    ItemRarity.Rare));
            Assert.That(inventory.TryAdd("training_chip", 3), Is.True);
            var view = new FakeInventoryView();

            using var presenter =
                new InventoryPresenter(inventory, _events, view);

            Assert.That(view.RenderCount, Is.EqualTo(1));
            Assert.That(
                view.LastModel.Slots,
                Has.Count.EqualTo(InventoryPresenter.VisibleSlotCount));
            Assert.That(view.LastModel.Capacity, Is.EqualTo(24));
            Assert.That(view.LastModel.OccupiedCount, Is.EqualTo(1));
            Assert.That(view.LastModel.SelectedSlotIndex, Is.EqualTo(0));
            Assert.That(view.LastModel.Slots[0].IsSelected, Is.True);
            Assert.That(
                view.LastModel.Slots[0].DisplayName,
                Is.EqualTo("训练芯片"));
            Assert.That(view.LastModel.Detail.HasItem, Is.True);
            Assert.That(
                view.LastModel.Detail.Description,
                Is.EqualTo("用于代理人训练。"));
        }

        [Test]
        public void SlotSelectionAndClose_AreHandledByPresenter()
        {
            var inventory = new FakeInventoryService(4);
            inventory.Register(
                CreateDefinition(
                    "first",
                    "第一件",
                    string.Empty,
                    1,
                    ItemCategory.Material,
                    ItemRarity.Common));
            inventory.Register(
                CreateDefinition(
                    "second",
                    "第二件",
                    string.Empty,
                    1,
                    ItemCategory.Consumable,
                    ItemRarity.Epic));
            inventory.TryAdd("first", 1);
            inventory.TryAdd("second", 1);
            var view = new FakeInventoryView();
            var closeCount = 0;

            using var presenter =
                new InventoryPresenter(
                    inventory,
                    _events,
                    view,
                    () => closeCount++);

            view.SelectSlot(1);
            view.RequestClose();

            Assert.That(presenter.SelectedSlotIndex, Is.EqualTo(1));
            Assert.That(view.LastModel.Detail.ItemId, Is.EqualTo("second"));
            Assert.That(view.LastModel.Detail.Rarity, Is.EqualTo(ItemRarity.Epic));
            Assert.That(closeCount, Is.EqualTo(1));
        }

        [Test]
        public void InventoryEvent_RerendersUntilPresenterIsDisposed()
        {
            var inventory = new FakeInventoryService(4);
            inventory.Register(
                CreateDefinition(
                    "chip",
                    "芯片",
                    string.Empty,
                    99,
                    ItemCategory.Material,
                    ItemRarity.Common));
            var view = new FakeInventoryView();
            var presenter =
                new InventoryPresenter(inventory, _events, view);

            inventory.TryAdd("chip", 2);
            _events.Publish(
                new InventoryChangedEvent(
                    inventory.Snapshot.Revision));

            Assert.That(view.RenderCount, Is.EqualTo(2));
            Assert.That(view.LastModel.Detail.ItemId, Is.EqualTo("chip"));

            presenter.Dispose();
            _events.Publish(
                new InventoryChangedEvent(
                    inventory.Snapshot.Revision));
            view.SelectSlot(3);

            Assert.That(view.RenderCount, Is.EqualTo(2));
        }

        private ItemDefinition CreateDefinition(
            string itemId,
            string displayName,
            string description,
            int maxStack,
            ItemCategory category,
            ItemRarity rarity)
        {
            var definition =
                ScriptableObject.CreateInstance<ItemDefinition>();
            SetField(definition, "_itemId", itemId);
            SetField(definition, "_displayName", displayName);
            SetField(definition, "_description", description);
            SetField(definition, "_maxStack", maxStack);
            SetField(definition, "_category", category);
            SetField(definition, "_rarity", rarity);
            _createdDefinitions.Add(definition);
            return definition;
        }

        private static void SetField<T>(
            ItemDefinition definition,
            string fieldName,
            T value)
        {
            typeof(ItemDefinition)
                .GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(definition, value);
        }

        private sealed class FakeInventoryView : IInventoryView
        {
            public event Action<int> SlotSelected;
            public event Action<int> CategorySelected;
            public event Action CloseRequested;

            public int RenderCount { get; private set; }
            public InventoryScreenViewModel LastModel { get; private set; }

            public void Render(InventoryScreenViewModel viewModel)
            {
                RenderCount++;
                LastModel = viewModel;
            }

            public void SelectSlot(int slotIndex)
            {
                SlotSelected?.Invoke(slotIndex);
            }

            public void SelectCategory(int categoryIndex)
            {
                CategorySelected?.Invoke(categoryIndex);
            }

            public void RequestClose()
            {
                CloseRequested?.Invoke();
            }
        }

        private sealed class FakeInventoryService :
            IInventoryService,
            IItemStackLimitProvider
        {
            private readonly Dictionary<string, ItemDefinition> _definitions =
                new(StringComparer.Ordinal);
            private readonly InventoryModel _model;

            public FakeInventoryService(int capacity)
            {
                _model = new InventoryModel(capacity, this);
            }

            public InventorySnapshot Snapshot => _model.Snapshot;

            public void Register(ItemDefinition definition)
            {
                _definitions.Add(definition.ItemId, definition);
            }

            public bool TryAdd(string itemId, int quantity)
            {
                return _definitions.ContainsKey(itemId) &&
                       _model.TryAdd(itemId, quantity);
            }

            public bool TryRemove(string itemId, int quantity)
            {
                return _definitions.ContainsKey(itemId) &&
                       _model.TryRemove(itemId, quantity);
            }

            public int GetTotalQuantity(string itemId)
            {
                return _model.GetTotalQuantity(itemId);
            }

            public bool TryGetDefinition(
                string itemId,
                out ItemDefinition definition)
            {
                return _definitions.TryGetValue(itemId, out definition);
            }

            public int GetMaxStack(string itemId)
            {
                return _definitions[itemId].MaxStack;
            }
        }
    }
}
