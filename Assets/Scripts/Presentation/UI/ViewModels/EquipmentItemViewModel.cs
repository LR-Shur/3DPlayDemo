using Train.Equipment.Data;
using UnityEngine;

namespace Train.Presentation.UI.ViewModels
{
    /// <summary>
    /// 装备目录中一张候选装备卡片的只读展示数据。
    /// </summary>
    public sealed class EquipmentItemViewModel
    {
        /// <summary>创建候选装备卡片。</summary>
        public EquipmentItemViewModel(
            string itemId,
            string displayName,
            EquipmentRarity rarity,
            Sprite icon,
            int ownedQuantity,
            bool isEquipped,
            bool isSelected)
        {
            ItemId = itemId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Rarity = rarity;
            Icon = icon;
            OwnedQuantity = ownedQuantity;
            IsEquipped = isEquipped;
            IsSelected = isSelected;
        }

        /// <summary>装备稳定标识。</summary>
        public string ItemId { get; }

        /// <summary>装备显示名称。</summary>
        public string DisplayName { get; }

        /// <summary>装备品质。</summary>
        public EquipmentRarity Rarity { get; }

        /// <summary>装备图标。</summary>
        public Sprite Icon { get; }

        /// <summary>背包持有数量。</summary>
        public int OwnedQuantity { get; }

        /// <summary>当前是否已穿戴在任意槽位。</summary>
        public bool IsEquipped { get; }

        /// <summary>当前是否被玩家选中。</summary>
        public bool IsSelected { get; }
    }
}
