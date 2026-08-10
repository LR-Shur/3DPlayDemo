using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Train.Architecture.Events;
using Train.Equipment.Application;
using Train.Equipment.Core;
using Train.Equipment.Data;
using Train.Equipment.Events;
using Train.Inventory.Application;
using Train.Inventory.Events;
using Train.Presentation.UI.Contracts;
using Train.Presentation.UI.ViewModels;

namespace Train.Presentation.UI.Presenters
{
    /// <summary>
    /// 连接装备 Server、背包只读数据和装备被动视图。
    /// 选择状态只存在于 Presenter，领域服务仍是装备与最终属性的唯一权威来源。
    /// </summary>
    public sealed class EquipmentPresenter : IDisposable
    {
        private readonly IEquipmentService _equipment;
        private readonly IInventoryService _inventory;
        private readonly IEquipmentView _view;
        private readonly Action _closeRequested;
        private readonly List<IDisposable> _subscriptions = new();
        private readonly List<EquipmentItemDefinition> _visibleItems = new();

        private string _selectedItemId;
        private EquipmentSlot _selectedSlot = EquipmentSlot.Weapon;
        private bool _disposed;

        /// <summary>
        /// 创建 Presenter、订阅装备与背包事件，并立即渲染初始状态。
        /// </summary>
        public EquipmentPresenter(
            IEquipmentService equipment,
            IInventoryService inventory,
            IEventBus events,
            IEquipmentView view,
            Action closeRequested)
        {
            _equipment = equipment ??
                throw new ArgumentNullException(nameof(equipment));
            _inventory = inventory ??
                throw new ArgumentNullException(nameof(inventory));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _closeRequested = closeRequested ??
                throw new ArgumentNullException(nameof(closeRequested));
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            _view.ItemSelected += OnItemSelected;
            _view.SlotSelected += OnSlotSelected;
            _view.EquipRequested += OnEquipRequested;
            _view.UnequipRequested += OnUnequipRequested;
            _view.CloseRequested += OnCloseRequested;
            _subscriptions.Add(
                events.Subscribe<EquipmentChangedEvent>(
                    OnEquipmentChanged));
            _subscriptions.Add(
                events.Subscribe<InventoryChangedEvent>(
                    OnInventoryChanged));

            SelectFirstOwnedItem();
            Render();
        }

        /// <summary>
        /// 解除视图回调与事件中心订阅。
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _view.ItemSelected -= OnItemSelected;
            _view.SlotSelected -= OnSlotSelected;
            _view.EquipRequested -= OnEquipRequested;
            _view.UnequipRequested -= OnUnequipRequested;
            _view.CloseRequested -= OnCloseRequested;
            for (var index = _subscriptions.Count - 1; index >= 0; index--)
            {
                _subscriptions[index].Dispose();
            }

            _subscriptions.Clear();
            _disposed = true;
        }

        /// <summary>
        /// 依据当前背包数量重建可见装备目录。
        /// </summary>
        private void RebuildVisibleItems()
        {
            _visibleItems.Clear();
            foreach (var definition in _equipment.Catalog)
            {
                if (definition != null &&
                    _inventory.GetTotalQuantity(definition.ItemId) > 0)
                {
                    _visibleItems.Add(definition);
                }
            }
        }

        /// <summary>
        /// 初次进入页面时选择背包中的第一件装备。
        /// </summary>
        private void SelectFirstOwnedItem()
        {
            RebuildVisibleItems();
            if (_visibleItems.Count == 0)
            {
                return;
            }

            _selectedItemId = _visibleItems[0].ItemId;
            _selectedSlot = ResolvePreferredSlot(
                _visibleItems[0],
                _equipment.Snapshot);
        }

        private void OnItemSelected(int index)
        {
            RebuildVisibleItems();
            if (index < 0 || index >= _visibleItems.Count)
            {
                return;
            }

            var definition = _visibleItems[index];
            _selectedItemId = definition.ItemId;
            _selectedSlot = ResolvePreferredSlot(
                definition,
                _equipment.Snapshot);
            Render();
        }

        private void OnSlotSelected(EquipmentSlot slot)
        {
            if (!Enum.IsDefined(typeof(EquipmentSlot), slot))
            {
                return;
            }

            _selectedSlot = slot;
            var equipped = _equipment.Snapshot.GetEquippedItem(slot);
            if (equipped != null)
            {
                _selectedItemId = equipped.ItemId;
            }

            Render();
        }

        private void OnEquipRequested()
        {
            if (!string.IsNullOrWhiteSpace(_selectedItemId))
            {
                _equipment.Equip(_selectedItemId, _selectedSlot);
                Render();
            }
        }

        private void OnUnequipRequested()
        {
            _equipment.Unequip(_selectedSlot);
            Render();
        }

        private void OnCloseRequested()
        {
            _closeRequested();
        }

        private void OnEquipmentChanged(EquipmentChangedEvent message)
        {
            Render();
        }

        private void OnInventoryChanged(InventoryChangedEvent message)
        {
            if (!string.IsNullOrWhiteSpace(_selectedItemId) &&
                _inventory.GetTotalQuantity(_selectedItemId) <= 0)
            {
                _selectedItemId = null;
                SelectFirstOwnedItem();
            }

            Render();
        }

        /// <summary>
        /// 从权威快照构建页面的全部槽位、候选卡片、属性与套装说明。
        /// </summary>
        private void Render()
        {
            if (_disposed)
            {
                return;
            }

            var snapshot = _equipment.Snapshot;
            RebuildVisibleItems();
            var selectedDefinition = FindSelectedDefinition();
            var slots = CreateSlotViewModels(snapshot);
            var items = CreateItemViewModels(snapshot);
            var stats = CreateStatViewModels(snapshot);
            var canEquip =
                selectedDefinition != null &&
                selectedDefinition.CanEquipIn(_selectedSlot) &&
                _inventory.GetTotalQuantity(selectedDefinition.ItemId) > 0;

            _view.Render(
                new EquipmentScreenViewModel(
                    snapshot.Revision,
                    slots,
                    items,
                    stats,
                    _selectedSlot,
                    selectedDefinition != null
                        ? selectedDefinition.DisplayName
                        : "未选择装备",
                    selectedDefinition != null
                        ? selectedDefinition.Description
                        : "从中间目录选择一件装备以查看详情。",
                    selectedDefinition != null
                        ? selectedDefinition.Rarity
                        : EquipmentRarity.Common,
                    selectedDefinition != null
                        ? selectedDefinition.Icon
                        : null,
                    CreateModifiersText(selectedDefinition),
                    CreateActiveSetsText(snapshot),
                    canEquip,
                    snapshot.GetEquippedItem(_selectedSlot) != null));
        }

        private EquipmentItemDefinition FindSelectedDefinition()
        {
            return !string.IsNullOrWhiteSpace(_selectedItemId) &&
                   _equipment.TryGetDefinition(
                       _selectedItemId,
                       out var definition)
                ? definition
                : null;
        }

        private IReadOnlyList<EquipmentSlotViewModel> CreateSlotViewModels(
            EquipmentSnapshot snapshot)
        {
            var result = new List<EquipmentSlotViewModel>(snapshot.Slots.Count);
            foreach (var slotSnapshot in snapshot.Slots)
            {
                EquipmentItemDefinition definition = null;
                if (slotSnapshot.Item != null)
                {
                    _equipment.TryGetDefinition(
                        slotSnapshot.Item.ItemId,
                        out definition);
                }

                result.Add(
                    new EquipmentSlotViewModel(
                        slotSnapshot.Slot,
                        FormatSlot(slotSnapshot.Slot),
                        definition != null
                            ? definition.DisplayName
                            : "未装备",
                        definition != null ? definition.Icon : null,
                        definition != null,
                        slotSnapshot.Slot == _selectedSlot));
            }

            return result;
        }

        private IReadOnlyList<EquipmentItemViewModel> CreateItemViewModels(
            EquipmentSnapshot snapshot)
        {
            var equippedIds = new HashSet<string>(
                snapshot.Slots
                    .Where(slot => slot.Item != null)
                    .Select(slot => slot.Item.ItemId),
                StringComparer.Ordinal);
            var result = new List<EquipmentItemViewModel>(_visibleItems.Count);
            foreach (var definition in _visibleItems)
            {
                result.Add(
                    new EquipmentItemViewModel(
                        definition.ItemId,
                        definition.DisplayName,
                        definition.Rarity,
                        definition.Icon,
                        _inventory.GetTotalQuantity(definition.ItemId),
                        equippedIds.Contains(definition.ItemId),
                        string.Equals(
                            definition.ItemId,
                            _selectedItemId,
                            StringComparison.Ordinal)));
            }

            return result;
        }

        private static IReadOnlyList<EquipmentStatViewModel>
            CreateStatViewModels(EquipmentSnapshot snapshot)
        {
            var result = new List<EquipmentStatViewModel>();
            foreach (StatType statType in Enum.GetValues(typeof(StatType)))
            {
                var baseValue = snapshot.GetBaseStat(statType);
                var finalValue = snapshot.GetFinalStat(statType);
                result.Add(
                    new EquipmentStatViewModel(
                        FormatStat(statType),
                        FormatStatValue(statType, baseValue),
                        FormatStatValue(statType, finalValue),
                        finalValue > baseValue + 0.0001f));
            }

            return result;
        }

        private static EquipmentSlot ResolvePreferredSlot(
            EquipmentItemDefinition definition,
            EquipmentSnapshot snapshot)
        {
            if (definition.Category != EquipmentItemCategory.Accessory)
            {
                return definition.GetDefaultSlot();
            }

            for (var slot = EquipmentSlot.Accessory1;
                 slot <= EquipmentSlot.Accessory5;
                 slot++)
            {
                if (snapshot.GetEquippedItem(slot) == null)
                {
                    return slot;
                }
            }

            return EquipmentSlot.Accessory1;
        }

        private static string CreateModifiersText(
            EquipmentItemDefinition definition)
        {
            if (definition == null || definition.Modifiers.Count == 0)
            {
                return "无属性词条";
            }

            var builder = new StringBuilder();
            foreach (var modifier in definition.Modifiers)
            {
                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(FormatStat(modifier.StatType));
                builder.Append("  ");
                builder.Append(FormatModifierValue(modifier));
            }

            return builder.ToString();
        }

        private static string CreateActiveSetsText(EquipmentSnapshot snapshot)
        {
            if (snapshot.ActivatedSets.Count == 0)
            {
                return "套装效果：尚未激活";
            }

            return string.Join(
                "\n",
                snapshot.ActivatedSets.Select(
                    set =>
                        $"{set.DisplayName}  {set.EquippedPieceCount}件 " +
                        $"// 已激活 {string.Join("/", set.ActiveBonusPieceCounts)}件效果"));
        }

        private static string FormatModifierValue(
            EquipmentStatModifierDefinition modifier)
        {
            return modifier.Operation == StatModifierOperation.Flat
                ? $"{(modifier.Value >= 0f ? "+" : string.Empty)}{modifier.Value:0.##}"
                : $"{(modifier.Value >= 0f ? "+" : string.Empty)}{modifier.Value * 100f:0.#}%";
        }

        private static string FormatSlot(EquipmentSlot slot)
        {
            return slot switch
            {
                EquipmentSlot.Weapon => "武器",
                EquipmentSlot.Helmet => "头盔",
                EquipmentSlot.Armor => "盔甲",
                EquipmentSlot.Gloves => "手套",
                EquipmentSlot.Shoes => "鞋子",
                EquipmentSlot.Accessory1 => "饰品 01",
                EquipmentSlot.Accessory2 => "饰品 02",
                EquipmentSlot.Accessory3 => "饰品 03",
                EquipmentSlot.Accessory4 => "饰品 04",
                EquipmentSlot.Accessory5 => "饰品 05",
                _ => slot.ToString()
            };
        }

        private static string FormatStat(StatType statType)
        {
            return statType switch
            {
                StatType.MaxHealth => "生命上限",
                StatType.Attack => "攻击力",
                StatType.Defense => "防御力",
                StatType.CritRate => "暴击率",
                StatType.CritDamage => "暴击伤害",
                StatType.ElectricDamageBonus => "雷属性伤害",
                _ => statType.ToString()
            };
        }

        private static string FormatStatValue(
            StatType statType,
            float value)
        {
            return statType is StatType.CritRate
                or StatType.CritDamage
                or StatType.ElectricDamageBonus
                ? $"{value * 100f:0.#}%"
                : $"{value:0.##}";
        }
    }
}
