namespace Train.Inventory.Events
{
    /// <summary>
    /// 表示背包系统在启动或运行过程中初始化失败。
    /// </summary>
    public readonly struct InventorySystemFailedEvent
    {
        /// <summary>创建一条背包系统失败消息。</summary>
        public InventorySystemFailedEvent(string reason)
        {
            Reason = reason;
        }

        /// <summary>获取便于诊断的失败原因。</summary>
        public string Reason { get; }
    }
}
