namespace Train.Equipment.Events
{
    /// <summary>
    /// 表示装备服务已完成初始化，可供其他应用模块使用。
    /// </summary>
    public readonly struct EquipmentSystemReadyEvent
    {
        /// <summary>创建装备系统就绪消息。</summary>
        public EquipmentSystemReadyEvent(
            int catalogCount,
            long revision)
        {
            CatalogCount = catalogCount;
            Revision = revision;
        }

        /// <summary>获取装备目录数量。</summary>
        public int CatalogCount { get; }

        /// <summary>获取初始化完成时的装备栏修订号。</summary>
        public long Revision { get; }
    }
}
