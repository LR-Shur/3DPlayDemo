using System;
using System.Collections.Generic;
using Train.Architecture.Assets;
using Train.Architecture.Events;
using Train.Inventory.Core;
using Train.Inventory.Data;
using Train.Inventory.Events;

namespace Train.Inventory.Application
{
    /// <summary>
    /// 协调背包领域逻辑、策划配置数据和游戏事件。
    /// 这是背包模块的 Server/Service 应用层。
    /// </summary>
    public sealed class InventoryService :
        IInventoryService,
        IItemStackLimitProvider,
        IDisposable
    {
        private readonly Dictionary<string, ItemDefinition> _definitions =
            new(StringComparer.Ordinal);
        private readonly IEventBus _events;
        private readonly IAssetLease<InventorySettings> _settingsLease;
        private readonly InventoryModel _model;
        private bool _disposed;

        /// <summary>
        /// 创建背包服务，并根据配置建立物品目录和初始背包。
        /// </summary>
        public InventoryService(
            InventorySettings settings,
            IEventBus events,
            IAssetLease<InventorySettings> settingsLease = null)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _events = events ?? throw new ArgumentNullException(nameof(events));
            _settingsLease = settingsLease;

            BuildCatalog(settings);
            _model = new InventoryModel(settings.Capacity, this);
            SeedStartingItems(settings);
            _model.Changed += OnModelChanged;
        }

        /// <inheritdoc />
        public InventorySnapshot Snapshot
        {
            get
            {
                ThrowIfDisposed();
                return _model.Snapshot;
            }
        }

        /// <summary>获取当前运行时注册的完整物品目录。</summary>
        public IReadOnlyList<ItemDefinition> Catalog =>
            new List<ItemDefinition>(_definitions.Values);

        /// <inheritdoc />
        public bool TryAdd(string itemId, int quantity)
        {
            ThrowIfDisposed();
            if (!_definitions.ContainsKey(itemId))
            {
                return false;
            }

            return _model.TryAdd(itemId, quantity);
        }

        /// <inheritdoc />
        public bool TryRemove(string itemId, int quantity)
        {
            ThrowIfDisposed();
            if (!_definitions.ContainsKey(itemId))
            {
                return false;
            }

            return _model.TryRemove(itemId, quantity);
        }

        /// <inheritdoc />
        public int GetTotalQuantity(string itemId)
        {
            ThrowIfDisposed();
            if (!_definitions.ContainsKey(itemId))
            {
                return 0;
            }

            return _model.GetTotalQuantity(itemId);
        }

        /// <inheritdoc />
        public bool TryGetDefinition(
            string itemId,
            out ItemDefinition definition)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(itemId))
            {
                definition = null;
                return false;
            }

            return _definitions.TryGetValue(itemId, out definition);
        }

        /// <summary>
        /// 获取指定已登记物品的单槽堆叠上限。
        /// </summary>
        public int GetMaxStack(string itemId)
        {
            if (!_definitions.TryGetValue(itemId, out var definition))
            {
                throw new InvalidOperationException(
                    $"Item '{itemId}' is not registered in the inventory catalog.");
            }

            return definition.MaxStack;
        }

        /// <summary>
        /// 释放配置租约并停止发布背包变化事件。
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _model.Changed -= OnModelChanged;
            _definitions.Clear();
            _settingsLease?.Dispose();
            _disposed = true;
        }

        private void BuildCatalog(InventorySettings settings)
        {
            foreach (var definition in settings.Items)
            {
                if (definition == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(definition.ItemId))
                {
                    throw new InvalidOperationException(
                        $"Inventory settings '{settings.name}' contain an item " +
                        "definition without a stable item id.");
                }

                if (!_definitions.TryAdd(definition.ItemId, definition))
                {
                    throw new InvalidOperationException(
                        $"Inventory settings '{settings.name}' contain duplicate " +
                        $"item id '{definition.ItemId}'.");
                }
            }
        }

        private void SeedStartingItems(InventorySettings settings)
        {
            foreach (var startingItem in settings.StartingItems)
            {
                if (startingItem == null)
                {
                    continue;
                }

                if (!_definitions.ContainsKey(startingItem.ItemId))
                {
                    throw new InvalidOperationException(
                        $"Starting item '{startingItem.ItemId}' is not present " +
                        "in the item catalog.");
                }

                if (!_model.TryAdd(startingItem.ItemId, startingItem.Count))
                {
                    throw new InvalidOperationException(
                        $"Starting inventory does not have enough capacity for " +
                        $"'{startingItem.ItemId}' x{startingItem.Count}.");
                }
            }
        }

        private void OnModelChanged(
            object sender,
            InventoryChangedEventArgs change)
        {
            var snapshot = change.Snapshot;
            var total = snapshot.GetTotalQuantity(change.ItemId);

            if (change.Kind == InventoryChangeKind.Added)
            {
                _events.Publish(
                    new ItemAcquiredEvent(
                        change.ItemId,
                        change.Quantity,
                        total,
                        snapshot.Revision));
            }
            else
            {
                _events.Publish(
                    new ItemRemovedEvent(
                        change.ItemId,
                        change.Quantity,
                        total,
                        snapshot.Revision));
            }

            _events.Publish(new InventoryChangedEvent(snapshot.Revision));
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(InventoryService));
            }
        }
    }
}
