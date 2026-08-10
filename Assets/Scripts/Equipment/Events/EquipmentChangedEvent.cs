using Train.Equipment.Core;

namespace Train.Equipment.Events
{
    /// <summary>
    /// 表示装备栏已经成功提交一次穿戴、卸下或饰品换槽操作。
    /// 表现层收到消息后可通过装备服务读取最新不可变快照。
    /// </summary>
    public readonly struct EquipmentChangedEvent
    {
        /// <summary>创建一条装备栏变化应用事件。</summary>
        public EquipmentChangedEvent(
            EquipmentChangeKind kind,
            EquipmentSlot targetSlot,
            EquipmentSlot? sourceSlot,
            string previousItemId,
            string currentItemId,
            long revision)
        {
            Kind = kind;
            TargetSlot = targetSlot;
            SourceSlot = sourceSlot;
            PreviousItemId = previousItemId;
            CurrentItemId = currentItemId;
            Revision = revision;
        }

        /// <summary>获取本次变化类型。</summary>
        public EquipmentChangeKind Kind { get; }

        /// <summary>获取本次操作的目标槽位。</summary>
        public EquipmentSlot TargetSlot { get; }

        /// <summary>
        /// 获取饰品移动的来源槽或卸下槽；普通穿戴时为空。
        /// </summary>
        public EquipmentSlot? SourceSlot { get; }

        /// <summary>获取操作前目标槽中的物品标识；没有则为空。</summary>
        public string PreviousItemId { get; }

        /// <summary>获取操作后目标槽中的物品标识；卸下时为空。</summary>
        public string CurrentItemId { get; }

        /// <summary>获取变化完成后的装备栏修订号。</summary>
        public long Revision { get; }
    }
}
