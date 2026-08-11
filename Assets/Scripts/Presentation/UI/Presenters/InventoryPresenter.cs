using System;
using Train.Architecture.Events;
using Train.Equipment.Application;
using Train.Equipment.Events;
using Train.Inventory.Application;
using Train.Inventory.Core;
using Train.Inventory.Data;
using Train.Inventory.Events;
using Train.Presentation.UI.Contracts;
using Train.Presentation.UI.ViewModels;

namespace Train.Presentation.UI.Presenters
{
    /// <summary>
    /// 将背包应用层快照映射为固定 24 槽位的界面模型。
    /// </summary>
    public sealed class InventoryPresenter : IDisposable
    {
        /// <summary>背包页面固定显示的槽位数量。</summary>
        public const int VisibleSlotCount = 24;

        private readonly IInventoryService _inventory;
        private readonly IEquipmentService _equipment;
        private readonly IInventoryView _view;
        private readonly Action _onCloseRequested;
        private readonly IDisposable _inventoryChangedSubscription;
        private readonly IDisposable _equipmentChangedSubscription;
        private int _selectedSlotIndex = -1;
        private int _selectedCategoryIndex;
        private bool _disposed;

        /// <summary>
        /// 创建背包 Presenter、连接视图交互，并立即执行首次渲染。
        /// </summary>
        public InventoryPresenter(
            IInventoryService inventory,
            IEquipmentService equipment,
            IEventBus events,
            IInventoryView view,
            Action onCloseRequested = null)
        {
            _inventory =
                inventory ?? throw new ArgumentNullException(nameof(inventory));
            _equipment = equipment;
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            _view = view ?? throw new ArgumentNullException(nameof(view));
            _onCloseRequested = onCloseRequested;

            _view.SlotSelected += OnSlotSelected;
            _view.CategorySelected += OnCategorySelected;
            _view.DiscardRequested += OnDiscardRequested;
            _view.CloseRequested += OnCloseRequested;
            _inventoryChangedSubscription =
                events.Subscribe<InventoryChangedEvent>(OnInventoryChanged);
            _equipmentChangedSubscription =
                events.Subscribe<EquipmentChangedEvent>(OnEquipmentChanged);

            Render();
        }

        /// <summary>兼容不需要装备标记的旧测试和独立背包预览。</summary>
        public InventoryPresenter(
            IInventoryService inventory,
            IEventBus events,
            IInventoryView view,
            Action onCloseRequested = null)
            : this(inventory, null, events, view, onCloseRequested)
        {
        }

        /// <summary>获取当前选中的槽位索引；尚未选择时为 -1。</summary>
        public int SelectedSlotIndex => _selectedSlotIndex;

        /// <summary>
        /// 断开视图和事件中心订阅，停止后续背包渲染。
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _view.SlotSelected -= OnSlotSelected;
            _view.CategorySelected -= OnCategorySelected;
            _view.DiscardRequested -= OnDiscardRequested;
            _view.CloseRequested -= OnCloseRequested;
            _inventoryChangedSubscription.Dispose();
            _equipmentChangedSubscription.Dispose();
            _disposed = true;
        }

        private void OnInventoryChanged(InventoryChangedEvent message)
        {
            if (_disposed)
            {
                return;
            }

            Render();
        }

        /// <summary>装备变化后刷新背包中的装备标记。</summary>
        private void OnEquipmentChanged(EquipmentChangedEvent message)
        {
            if (!_disposed)
            {
                Render();
            }
        }

        private void OnSlotSelected(int slotIndex)
        {
            if (_disposed ||
                slotIndex < 0 ||
                slotIndex >= VisibleSlotCount)
            {
                return;
            }

            _selectedSlotIndex = slotIndex;
            Render();
        }

        private void OnCloseRequested()
        {
            if (!_disposed)
            {
                _onCloseRequested?.Invoke();
            }
        }

        private void OnCategorySelected(int categoryIndex)
        {
            if (_disposed || categoryIndex < 0 || categoryIndex > 5)
            {
                return;
            }

            _selectedCategoryIndex = categoryIndex;
            _selectedSlotIndex = -1;
            Render();
        }

        /// <summary>删除当前选中的整叠物品，已装备物品由详情模型禁止删除。</summary>
        private void OnDiscardRequested()
        {
            if (_disposed || _selectedSlotIndex < 0)
            {
                return;
            }

            var detail = BuildSelectedDetail();
            if (detail == null || !detail.CanDiscard)
            {
                return;
            }

            _inventory.TryRemove(detail.ItemId, detail.Quantity);
        }

        private void Render()
        {
            var snapshot = _inventory.Snapshot;
            var equippedIds = GetEquippedItemIds();
            var filteredSlots = new System.Collections.Generic.List<InventorySlotSnapshot>();
            var occupiedCount = 0;
            for (var index = 0; index < snapshot.Slots.Count; index++)
            {
                var slot = snapshot.Slots[index];
                if (slot.IsEmpty)
                {
                    continue;
                }

                occupiedCount++;
                _inventory.TryGetDefinition(slot.ItemId, out var definition);
                var category = definition != null
                    ? definition.Category
                    : ItemCategory.Material;
                if (MatchesCategory(_selectedCategoryIndex, category))
                {
                    filteredSlots.Add(slot);
                }
            }

            EnsureSelection(filteredSlots.Count);

            var slots = new InventorySlotViewModel[VisibleSlotCount];
            var detail = InventoryItemDetailViewModel.Empty;

            for (var slotIndex = 0;
                 slotIndex < VisibleSlotCount;
                 slotIndex++)
            {
                var slot = slotIndex < filteredSlots.Count
                    ? filteredSlots[slotIndex]
                    : new InventorySlotSnapshot(slotIndex, null, 0);
                var occupied = !slot.IsEmpty;
                var selected = slotIndex == _selectedSlotIndex;
                ItemDefinition definition = null;

                if (occupied)
                {
                    _inventory.TryGetDefinition(
                        slot.ItemId,
                        out definition);
                }

                var displayName =
                    definition != null
                        ? definition.DisplayName
                        : occupied
                            ? slot.ItemId
                            : string.Empty;
                var maxStack = definition != null
                    ? definition.MaxStack
                    : occupied
                        ? slot.Quantity
                        : 0;
                var category = definition != null
                    ? definition.Category
                    : ItemCategory.Material;
                var rarity = definition != null
                    ? definition.Rarity
                    : ItemRarity.Common;
                var iconLocation = definition != null
                    ? definition.IconLocation
                    : string.Empty;

                var isEquipped = occupied && equippedIds.Contains(slot.ItemId);
                slots[slotIndex] = new InventorySlotViewModel(
                    slotIndex,
                    occupied,
                    slot.ItemId,
                    displayName,
                    slot.Quantity,
                    maxStack,
                    category,
                    rarity,
                    iconLocation,
                    selected,
                    isEquipped);

                if (selected && occupied)
                {
                    detail = new InventoryItemDetailViewModel(
                        true,
                        slotIndex,
                        slot.ItemId,
                        displayName,
                        definition != null
                            ? definition.Description
                            : string.Empty,
                        slot.Quantity,
                        maxStack,
                        category,
                        rarity,
                        iconLocation,
                        isEquipped);
                }
            }

            _view.Render(
                new InventoryScreenViewModel(
                    snapshot.Revision,
                    VisibleSlotCount,
                    occupiedCount,
                    _selectedSlotIndex,
                    _selectedCategoryIndex,
                    slots,
                    detail));
        }

        /// <summary>重新解析当前筛选页的选中物品，供丢弃操作使用。</summary>
        private InventoryItemDetailViewModel BuildSelectedDetail()
        {
            var snapshot = _inventory.Snapshot;
            var filteredSlots = new System.Collections.Generic.List<InventorySlotSnapshot>();
            foreach (var slot in snapshot.Slots)
            {
                if (slot.IsEmpty)
                {
                    continue;
                }

                _inventory.TryGetDefinition(slot.ItemId, out var definition);
                var category = definition != null
                    ? definition.Category
                    : ItemCategory.Material;
                if (MatchesCategory(_selectedCategoryIndex, category))
                {
                    filteredSlots.Add(slot);
                }
            }

            if (_selectedSlotIndex < 0 || _selectedSlotIndex >= filteredSlots.Count)
            {
                return null;
            }

            var slotSnapshot = filteredSlots[_selectedSlotIndex];
            _inventory.TryGetDefinition(slotSnapshot.ItemId, out var itemDefinition);
            var equipped = GetEquippedItemIds().Contains(slotSnapshot.ItemId);
            return new InventoryItemDetailViewModel(
                true,
                _selectedSlotIndex,
                slotSnapshot.ItemId,
                itemDefinition != null ? itemDefinition.DisplayName : slotSnapshot.ItemId,
                itemDefinition != null ? itemDefinition.Description : string.Empty,
                slotSnapshot.Quantity,
                itemDefinition != null ? itemDefinition.MaxStack : slotSnapshot.Quantity,
                itemDefinition != null ? itemDefinition.Category : ItemCategory.Material,
                itemDefinition != null ? itemDefinition.Rarity : ItemRarity.Common,
                itemDefinition != null ? itemDefinition.IconLocation : string.Empty,
                equipped);
        }

        /// <summary>收集装备栏中已使用的物品 ID，供背包角标和删除保护复用。</summary>
        private System.Collections.Generic.HashSet<string> GetEquippedItemIds()
        {
            var result = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            if (_equipment == null)
            {
                return result;
            }

            foreach (var slot in _equipment.Snapshot.Slots)
            {
                if (!slot.IsEmpty && slot.Item != null)
                {
                    result.Add(slot.Item.ItemId);
                }
            }

            return result;
        }

        private void EnsureSelection(int filteredCount)
        {
            if (_selectedSlotIndex >= 0 &&
                _selectedSlotIndex < filteredCount)
            {
                return;
            }

            _selectedSlotIndex = filteredCount > 0 ? 0 : -1;
        }

        private static bool MatchesCategory(
            int categoryIndex,
            ItemCategory category)
        {
            return categoryIndex switch
            {
                0 => true,
                1 => category == ItemCategory.Material,
                2 => category == ItemCategory.Consumable,
                3 => category == ItemCategory.Equipment,
                4 => category == ItemCategory.Currency,
                5 => category == ItemCategory.Quest,
                _ => true
            };
        }
    }
}
