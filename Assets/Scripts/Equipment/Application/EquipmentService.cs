using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Train.Architecture.Assets;
using Train.Architecture.Events;
using Train.Equipment.Core;
using Train.Equipment.Data;
using Train.Equipment.Events;

namespace Train.Equipment.Application
{
    /// <summary>
    /// 负责装备配置转换、领域换装用例、应用事件发布和设置资源生命周期。
    /// 这是装备模块面向其他系统的 Server/Service 应用层实现。
    /// </summary>
    public sealed class EquipmentService : IEquipmentService, IDisposable
    {
        private readonly Dictionary<string, EquipmentItemDefinition>
            _definitions = new(StringComparer.Ordinal);
        private readonly Dictionary<string, EquipmentItemSpec> _itemSpecs =
            new(StringComparer.Ordinal);
        private readonly Dictionary<string, EquipmentSetDefinition>
            _setDefinitions = new(StringComparer.Ordinal);
        private readonly IEventBus _events;
        private readonly IAssetLease<EquipmentSettings> _settingsLease;
        private readonly EquipmentLoadout _loadout;
        private readonly ReadOnlyCollection<EquipmentItemDefinition> _catalog;
        private bool _disposed;

        /// <summary>
        /// 根据设置资源创建装备目录和角色装备栏。
        /// 传入的资源租约所有权转移给服务，并在服务释放时一并释放。
        /// </summary>
        public EquipmentService(
            EquipmentSettings settings,
            IEventBus events,
            IAssetLease<EquipmentSettings> settingsLease = null)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _events = events ?? throw new ArgumentNullException(nameof(events));
            _settingsLease = settingsLease;

            try
            {
                var setSpecs = BuildSetSpecs(settings, out var knownSetIds);
                for (var i = 0; i < settings.Sets.Count; i++)
                {
                    var setDefinition = settings.Sets[i];
                    if (setDefinition != null)
                    {
                        _setDefinitions.Add(
                            setDefinition.SetId,
                            setDefinition);
                    }
                }

                var catalog = BuildCatalog(settings, knownSetIds);
                var baseStats = BuildBaseStats(settings);

                _loadout = new EquipmentLoadout(baseStats, setSpecs);
                SeedStartingEquipment(settings);
                _catalog = Array.AsReadOnly(catalog);
                _loadout.Changed += OnLoadoutChanged;
            }
            catch
            {
                _definitions.Clear();
                _itemSpecs.Clear();
                _setDefinitions.Clear();
                _settingsLease?.Dispose();
                throw;
            }
        }

        /// <inheritdoc />
        public EquipmentSnapshot Snapshot
        {
            get
            {
                ThrowIfDisposed();
                return _loadout.Snapshot;
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<EquipmentItemDefinition> Catalog
        {
            get
            {
                ThrowIfDisposed();
                return _catalog;
            }
        }

        /// <inheritdoc />
        public bool TryGetDefinition(
            string itemId,
            out EquipmentItemDefinition definition)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(itemId))
            {
                definition = null;
                return false;
            }

            return _definitions.TryGetValue(itemId, out definition);
        }

        public bool TryGetSetDefinition(
            string setId,
            out EquipmentSetDefinition definition)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(setId))
            {
                definition = null;
                return false;
            }

            return _setDefinitions.TryGetValue(setId, out definition);
        }

        /// <inheritdoc />
        public bool Equip(string itemId, EquipmentSlot targetSlot)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(itemId) ||
                !Enum.IsDefined(typeof(EquipmentSlot), targetSlot) ||
                !_definitions.TryGetValue(itemId, out var definition) ||
                !definition.CanEquipIn(targetSlot))
            {
                return false;
            }

            return _loadout.Equip(_itemSpecs[itemId], targetSlot);
        }

        /// <inheritdoc />
        public bool Unequip(EquipmentSlot slot)
        {
            ThrowIfDisposed();
            if (!Enum.IsDefined(typeof(EquipmentSlot), slot))
            {
                return false;
            }

            return _loadout.Unequip(slot);
        }

        /// <summary>
        /// 停止发布装备事件并释放设置资源租约。
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _loadout.Changed -= OnLoadoutChanged;
            _definitions.Clear();
            _itemSpecs.Clear();
            _setDefinitions.Clear();
            _settingsLease?.Dispose();
            _disposed = true;
        }

        private EquipmentItemDefinition[] BuildCatalog(
            EquipmentSettings settings,
            ISet<string> knownSetIds)
        {
            var catalog =
                new EquipmentItemDefinition[settings.Items.Count];
            for (var i = 0; i < settings.Items.Count; i++)
            {
                var definition = settings.Items[i] ??
                    throw new InvalidOperationException(
                        $"装备设置 '{settings.name}' 的目录包含 null。");
                var spec = definition.ToCoreSpec();

                if (!_definitions.TryAdd(spec.ItemId, definition))
                {
                    throw new InvalidOperationException(
                        $"装备设置 '{settings.name}' 包含重复物品标识 " +
                        $"'{spec.ItemId}'。");
                }

                if (spec.SetId != null &&
                    !knownSetIds.Contains(spec.SetId))
                {
                    throw new InvalidOperationException(
                        $"装备 '{spec.ItemId}' 引用了未登记套装 " +
                        $"'{spec.SetId}'。");
                }

                _itemSpecs.Add(spec.ItemId, spec);
                catalog[i] = definition;
            }

            return catalog;
        }

        private static EquipmentSetSpec[] BuildSetSpecs(
            EquipmentSettings settings,
            out ISet<string> knownSetIds)
        {
            knownSetIds = new HashSet<string>(StringComparer.Ordinal);
            var specs = new EquipmentSetSpec[settings.Sets.Count];
            for (var i = 0; i < settings.Sets.Count; i++)
            {
                var definition = settings.Sets[i] ??
                    throw new InvalidOperationException(
                        $"装备设置 '{settings.name}' 的套装目录包含 null。");
                var spec = definition.ToCoreSpec();
                if (!knownSetIds.Add(spec.SetId))
                {
                    throw new InvalidOperationException(
                        $"装备设置 '{settings.name}' 包含重复套装标识 " +
                        $"'{spec.SetId}'。");
                }

                specs[i] = spec;
            }

            return specs;
        }

        private static Dictionary<StatType, float> BuildBaseStats(
            EquipmentSettings settings)
        {
            var baseStats = new Dictionary<StatType, float>();
            for (var i = 0; i < settings.BaseStats.Count; i++)
            {
                var definition = settings.BaseStats[i] ??
                    throw new InvalidOperationException(
                        $"装备设置 '{settings.name}' 的基础属性包含 null。");
                if (!baseStats.TryAdd(
                        definition.StatType,
                        definition.Value))
                {
                    throw new InvalidOperationException(
                        $"装备设置 '{settings.name}' 重复配置基础属性 " +
                        $"{definition.StatType}。");
                }
            }

            return baseStats;
        }

        private void SeedStartingEquipment(EquipmentSettings settings)
        {
            for (var i = 0; i < settings.StartingEquipment.Count; i++)
            {
                var starting = settings.StartingEquipment[i] ??
                    throw new InvalidOperationException(
                        $"装备设置 '{settings.name}' 的初始装备包含 null。");
                var definition = starting.Item ??
                    throw new InvalidOperationException(
                        $"装备设置 '{settings.name}' 的初始装备未指定物品。");

                if (!_definitions.TryGetValue(
                        definition.ItemId,
                        out var catalogDefinition) ||
                    !ReferenceEquals(catalogDefinition, definition))
                {
                    throw new InvalidOperationException(
                        $"初始装备 '{definition.ItemId}' 不在装备目录中。");
                }

                var currentSnapshot = _loadout.Snapshot;
                if (!definition.CanEquipIn(starting.TargetSlot) ||
                    currentSnapshot.GetEquippedItem(
                        starting.TargetSlot) != null ||
                    ContainsItem(
                        currentSnapshot,
                        definition.ItemId) ||
                    !_loadout.Equip(
                        _itemSpecs[definition.ItemId],
                        starting.TargetSlot))
                {
                    throw new InvalidOperationException(
                        $"初始装备 '{definition.ItemId}' 无法穿戴到 " +
                        $"{starting.TargetSlot}。");
                }
            }
        }

        private static bool ContainsItem(
            EquipmentSnapshot snapshot,
            string itemId)
        {
            for (var i = 0; i < snapshot.Slots.Count; i++)
            {
                var equippedItem = snapshot.Slots[i].Item;
                if (equippedItem != null &&
                    string.Equals(
                        equippedItem.ItemId,
                        itemId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void OnLoadoutChanged(
            object sender,
            EquipmentChangedEventArgs change)
        {
            _events.Publish(
                new EquipmentChangedEvent(
                    change.Kind,
                    change.Slot,
                    change.SourceSlot,
                    change.PreviousItem?.ItemId,
                    change.CurrentItem?.ItemId,
                    change.Snapshot.Revision));
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(EquipmentService));
            }
        }
    }
}
