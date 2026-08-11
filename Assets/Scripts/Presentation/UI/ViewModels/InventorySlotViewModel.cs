using Train.Inventory.Data;

namespace Train.Presentation.UI.ViewModels
{
    /// <summary>
    /// 提供单个背包槽位渲染所需的不可变展示数据。
    /// </summary>
    public sealed class InventorySlotViewModel
    {
        /// <summary>创建一份背包槽位视图模型。</summary>
        public InventorySlotViewModel(
            int slotIndex,
            bool isOccupied,
            string itemId,
            string displayName,
            int quantity,
            int maxStack,
            ItemCategory category,
            ItemRarity rarity,
            string iconLocation,
            bool isSelected,
            bool isEquipped)
        {
            SlotIndex = slotIndex;
            IsOccupied = isOccupied;
            ItemId = itemId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Quantity = quantity;
            MaxStack = maxStack;
            Category = category;
            Rarity = rarity;
            IconLocation = iconLocation ?? string.Empty;
            IsSelected = isSelected;
            IsEquipped = isEquipped;
        }

        /// <summary>获取槽位索引。</summary>
        public int SlotIndex { get; }

        /// <summary>获取槽位是否包含物品。</summary>
        public bool IsOccupied { get; }

        /// <summary>获取槽位物品的稳定标识。</summary>
        public string ItemId { get; }

        /// <summary>获取槽位物品的展示名称。</summary>
        public string DisplayName { get; }

        /// <summary>获取槽位中的物品数量。</summary>
        public int Quantity { get; }

        /// <summary>获取此物品的单槽堆叠上限。</summary>
        public int MaxStack { get; }

        /// <summary>获取物品用途分类。</summary>
        public ItemCategory Category { get; }

        /// <summary>获取物品稀有度。</summary>
        public ItemRarity Rarity { get; }

        /// <summary>获取物品图标的资源定位地址。</summary>
        public string IconLocation { get; }

        /// <summary>获取此槽位当前是否被选中。</summary>
        public bool IsSelected { get; }

        /// <summary>当前物品是否已装备。</summary>
        public bool IsEquipped { get; }
    }
}
