namespace Train.Inventory.Core
{
    /// <summary>
    /// 固定背包槽位在某一时刻的不可变数据。
    /// </summary>
    public readonly struct InventorySlotSnapshot
    {
        /// <summary>
        /// 创建一份槽位快照。
        /// </summary>
        public InventorySlotSnapshot(
            int slotIndex,
            string itemId,
            int quantity)
        {
            SlotIndex = slotIndex;
            ItemId = itemId;
            Quantity = quantity;
        }

        /// <summary>获取槽位索引。</summary>
        public int SlotIndex { get; }

        /// <summary>获取槽位中的物品标识；空槽位时为空。</summary>
        public string ItemId { get; }

        /// <summary>获取槽位中的物品数量。</summary>
        public int Quantity { get; }

        /// <summary>获取此槽位是否为空。</summary>
        public bool IsEmpty => Quantity == 0;
    }
}
