namespace Train.WorldInteraction.Core
{
    /// <summary>
    /// 一次交互命令的业务结果类型。
    /// 表现层可以据此显示拾取成功、背包已满或目标不可用等反馈。
    /// </summary>
    public enum InteractResultCode
    {
        /// <summary>
        /// 未初始化结果。default(InteractResult) 会落到此值，不能被误判为成功。
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// 交互成功完成。
        /// </summary>
        Success = 1,

        /// <summary>
        /// 交互因背包空间不足而失败。
        /// </summary>
        InventoryFull = 2,

        /// <summary>
        /// 目标当前不可使用，例如已被拾取或条件尚未满足。
        /// </summary>
        Unavailable = 3,

        /// <summary>
        /// 玩家附近没有可作为焦点的交互候选。
        /// </summary>
        NoFocusedCandidate = 4,

        /// <summary>
        /// 交互执行失败，但不属于上述可细分的业务原因。
        /// </summary>
        Failed = 5,

        /// <summary>
        /// 交互被玩家或上层流程主动取消。
        /// </summary>
        Cancelled = 6
    }
}
