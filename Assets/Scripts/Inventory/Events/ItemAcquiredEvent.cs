namespace Train.Inventory.Events
{
    /// <summary>
    /// 表示玩家已成功获得一种物品。
    /// </summary>
    public readonly struct ItemAcquiredEvent
    {
        /// <summary>创建一条物品获得消息。</summary>
        public ItemAcquiredEvent(
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

        /// <summary>获取获得物品的稳定标识。</summary>
        public string ItemId { get; }

        /// <summary>获取本次获得数量。</summary>
        public int Quantity { get; }

        /// <summary>获取变化后该物品的总持有数量。</summary>
        public int TotalQuantity { get; }

        /// <summary>获取变化完成后的背包修订号。</summary>
        public long Revision { get; }
    }
}
