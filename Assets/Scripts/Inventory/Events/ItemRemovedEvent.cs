namespace Train.Inventory.Events
{
    /// <summary>
    /// 表示玩家已成功从背包移除或消耗一种物品。
    /// </summary>
    public readonly struct ItemRemovedEvent
    {
        /// <summary>创建一条物品移除消息。</summary>
        public ItemRemovedEvent(
            string itemId,
            int quantity,
            int totalQuantity,
            long revision)
        {
            ItemId = itemId;
            Quantity = quantity;
            TotalQuantity = totalQuantity;
            Revision = revision;
        }

        /// <summary>获取被移除物品的稳定标识。</summary>
        public string ItemId { get; }

        /// <summary>获取本次移除数量。</summary>
        public int Quantity { get; }

        /// <summary>获取变化后该物品的总持有数量。</summary>
        public int TotalQuantity { get; }

        /// <summary>获取变化完成后的背包修订号。</summary>
        public long Revision { get; }
    }
}
