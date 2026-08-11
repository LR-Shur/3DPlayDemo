using Train.Inventory.Data;

namespace Train.Presentation.UI.ViewModels
{
    /// <summary>
    /// 提供背包所选物品详情区域需要的不可变展示数据。
    /// </summary>
    public sealed class InventoryItemDetailViewModel
    {
        /// <summary>获取一个表示未选择物品的共享详情模型。</summary>
        public static readonly InventoryItemDetailViewModel Empty =
            new(
                false,
                -1,
                string.Empty,
                string.Empty,
                string.Empty,
                0,
                0,
                ItemCategory.Material,
                ItemRarity.Common,
                string.Empty,
                false);

        /// <summary>创建一份背包物品详情视图模型。</summary>
        public InventoryItemDetailViewModel(
            bool hasItem,
            int slotIndex,
            string itemId,
            string displayName,
            string description,
            int quantity,
            int maxStack,
            ItemCategory category,
            ItemRarity rarity,
            string iconLocation,
            bool isEquipped)
        {
            HasItem = hasItem;
            SlotIndex = slotIndex;
            ItemId = itemId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Description = description ?? string.Empty;
            Quantity = quantity;
            MaxStack = maxStack;
            Category = category;
            Rarity = rarity;
            IconLocation = iconLocation ?? string.Empty;
            IsEquipped = isEquipped;
        }

        /// <summary>获取当前详情是否包含有效物品。</summary>
        public bool HasItem { get; }

        /// <summary>获取对应的背包槽位索引。</summary>
        public int SlotIndex { get; }

        /// <summary>获取物品稳定标识。</summary>
        public string ItemId { get; }

        /// <summary>获取物品展示名称。</summary>
        public string DisplayName { get; }

        /// <summary>获取物品展示说明。</summary>
        public string Description { get; }

        /// <summary>获取当前槽位中的物品数量。</summary>
        public int Quantity { get; }

        /// <summary>获取此物品的单槽堆叠上限。</summary>
        public int MaxStack { get; }

        /// <summary>获取物品用途分类。</summary>
        public ItemCategory Category { get; }

        /// <summary>获取物品稀有度。</summary>
        public ItemRarity Rarity { get; }

        /// <summary>获取物品图标的资源定位地址。</summary>
        public string IconLocation { get; }

        /// <summary>当前详情物品是否已装备。</summary>
        public bool IsEquipped { get; }

        /// <summary>当前详情物品是否允许丢弃。</summary>
        public bool CanDiscard => HasItem && !IsEquipped;
    }
}
