namespace Train.Inventory.Events
{
    /// <summary>
    /// 表示背包服务已经创建完成并可供其他系统使用。
    /// </summary>
    public readonly struct InventorySystemReadyEvent
    {
        /// <summary>创建一条背包系统就绪消息。</summary>
        public InventorySystemReadyEvent(
            int capacity,
            long revision)
        {
            Capacity = capacity;
            Revision = revision;
        }

        /// <summary>获取背包槽位容量。</summary>
        public int Capacity { get; }

        /// <summary>获取系统就绪时的背包修订号。</summary>
        public long Revision { get; }
    }
}
