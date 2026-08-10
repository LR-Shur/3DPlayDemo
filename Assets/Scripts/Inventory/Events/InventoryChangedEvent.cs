namespace Train.Inventory.Events
{
    /// <summary>
    /// 表示背包内容已成功变化，表现层可据此重新读取最新快照。
    /// </summary>
    public readonly struct InventoryChangedEvent
    {
        /// <summary>创建一条背包变化消息。</summary>
        public InventoryChangedEvent(long revision)
        {
            Revision = revision;
        }

        /// <summary>获取变化完成后的背包修订号。</summary>
        public long Revision { get; }
    }
}
