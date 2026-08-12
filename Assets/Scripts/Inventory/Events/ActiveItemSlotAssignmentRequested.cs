using System;

namespace Train.Inventory.Events
{
    /// <summary>请求把背包中的消耗品绑定到主动道具快捷槽。</summary>
    public readonly struct ActiveItemSlotAssignmentRequested
    {
        public ActiveItemSlotAssignmentRequested(int slotIndex, string itemId)
        {
            SlotIndex = slotIndex;
            ItemId = itemId ?? string.Empty;
        }

        public int SlotIndex { get; }
        public string ItemId { get; }
    }
}
