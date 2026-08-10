namespace Train.Buffs.Core
{
    /// <summary>
    /// 单个 Buff 在某一时刻的只读快照，可安全交给 UI、存档和调试工具读取。
    /// </summary>
    public readonly struct BuffSnapshot
    {
        /// <summary>
        /// 创建单个 Buff 快照。
        /// </summary>
        public BuffSnapshot(
            string buffId,
            string sourceId,
            BuffStackPolicy stackPolicy,
            float remainingDuration,
            float magnitude,
            int stackCount,
            int maxStacks)
        {
            BuffId = buffId;
            SourceId = sourceId;
            StackPolicy = stackPolicy;
            RemainingDuration = remainingDuration;
            Magnitude = magnitude;
            StackCount = stackCount;
            MaxStacks = maxStacks;
        }

        /// <summary>
        /// Buff 稳定标识。
        /// </summary>
        public string BuffId { get; }

        /// <summary>
        /// Buff 来源稳定标识。
        /// </summary>
        public string SourceId { get; }

        /// <summary>
        /// 重复施加时使用的叠加策略。
        /// </summary>
        public BuffStackPolicy StackPolicy { get; }

        /// <summary>
        /// 剩余持续时间，单位为秒。
        /// </summary>
        public float RemainingDuration { get; }

        /// <summary>
        /// 每层效果强度。
        /// </summary>
        public float Magnitude { get; }

        /// <summary>
        /// 当前层数。
        /// </summary>
        public int StackCount { get; }

        /// <summary>
        /// 最大层数。
        /// </summary>
        public int MaxStacks { get; }
    }
}
