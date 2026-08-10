namespace Train.GameFlow.Core
{
    /// <summary>
    /// 表示关卡状态机已经完成一次阶段转换。
    /// </summary>
    public readonly struct LevelPhaseChanged
    {
        /// <summary>
        /// 创建一条关卡阶段变化记录。
        /// </summary>
        public LevelPhaseChanged(
            LevelPhase previous,
            LevelPhase current,
            LevelOutcome outcome)
        {
            Previous = previous;
            Current = current;
            Outcome = outcome;
        }

        /// <summary>获取转换前阶段。</summary>
        public LevelPhase Previous { get; }

        /// <summary>获取转换后阶段。</summary>
        public LevelPhase Current { get; }

        /// <summary>获取转换完成后的关卡结果。</summary>
        public LevelOutcome Outcome { get; }
    }
}
