namespace Train.Equipment.Events
{
    /// <summary>
    /// 表示装备系统在资源加载或配置构建过程中初始化失败。
    /// </summary>
    public readonly struct EquipmentSystemFailedEvent
    {
        /// <summary>创建装备系统失败消息。</summary>
        public EquipmentSystemFailedEvent(string reason)
        {
            Reason = reason;
        }

        /// <summary>获取便于诊断的失败原因。</summary>
        public string Reason { get; }
    }
}
