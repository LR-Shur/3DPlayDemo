using Train.Equipment.Core;
using UnityEngine;

namespace Train.Presentation.UI.ViewModels
{
    /// <summary>
    /// 一个装备或饰品槽位的只读展示数据。
    /// </summary>
    public sealed class EquipmentSlotViewModel
    {
        /// <summary>创建装备槽位展示数据。</summary>
        public EquipmentSlotViewModel(
            EquipmentSlot slot,
            string slotName,
            string itemName,
            Sprite icon,
            bool isOccupied,
            bool isSelected)
        {
            Slot = slot;
            SlotName = slotName ?? string.Empty;
            ItemName = itemName ?? string.Empty;
            Icon = icon;
            IsOccupied = isOccupied;
            IsSelected = isSelected;
        }

        /// <summary>领域槽位标识。</summary>
        public EquipmentSlot Slot { get; }

        /// <summary>本地化槽位名称。</summary>
        public string SlotName { get; }

        /// <summary>当前装备名称。</summary>
        public string ItemName { get; }

        /// <summary>当前装备图标。</summary>
        public Sprite Icon { get; }

        /// <summary>槽位当前是否有装备。</summary>
        public bool IsOccupied { get; }

        /// <summary>当前是否选中此槽位。</summary>
        public bool IsSelected { get; }
    }
}
