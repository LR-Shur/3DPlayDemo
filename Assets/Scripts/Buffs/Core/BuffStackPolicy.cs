namespace Train.Buffs.Core
{
    /// <summary>
    /// 同一 Buff 再次施加时的合并策略。
    /// </summary>
    public enum BuffStackPolicy
    {
        /// <summary>
        /// 不增加层数，仅刷新剩余时间。
        /// </summary>
        RefreshDuration = 0,

        /// <summary>
        /// 增加层数并刷新剩余时间，最终层数不超过最大层数。
        /// </summary>
        AddStack = 1,

        /// <summary>
        /// 每次施加都建立独立实例。
        /// 当前句柄为稳定归因而按 Buff 与来源合并，此策略预留给后续实例键扩展。
        /// </summary>
        Independent = 2
    }
}
