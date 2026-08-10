namespace Train.Buffs.Core
{
    /// <summary>
    /// Buff 句柄一次语义变更的类型。
    /// 普通 Tick 只改变剩余秒数，不产生事件；到期才产生语义变更事件。
    /// </summary>
    public enum BuffChangeKind
    {
        /// <summary>
        /// 新增了一个 Buff。
        /// </summary>
        Applied = 0,

        /// <summary>
        /// 已有 Buff 的持续时间被刷新，但层数没有增加。
        /// </summary>
        Refreshed = 1,

        /// <summary>
        /// 已有 Buff 增加了层数并刷新持续时间。
        /// </summary>
        Stacked = 2,

        /// <summary>
        /// Buff 被外部命令主动移除。
        /// </summary>
        Removed = 3,

        /// <summary>
        /// Buff 因持续时间耗尽而移除。
        /// </summary>
        Expired = 4
    }
}
