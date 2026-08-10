namespace Train.GameFlow.Core
{
    /// <summary>
    /// 定义标准战斗关卡流程：
    /// 未开始 → 准备 → 开场 → 战斗 → 通关/失败 → 退出。
    /// </summary>
    public sealed class DefaultLevelTransitionPolicy : ILevelTransitionPolicy
    {
        /// <summary>
        /// 获取无状态的默认关卡转换策略单例。
        /// </summary>
        public static DefaultLevelTransitionPolicy Instance { get; } =
            new DefaultLevelTransitionPolicy();

        private DefaultLevelTransitionPolicy()
        {
        }

        /// <summary>
        /// 判断指定的两个关卡阶段之间是否允许直接转换。
        /// </summary>
        /// <param name="from">转换前阶段。</param>
        /// <param name="to">目标阶段。</param>
        /// <returns>允许转换时返回 <see langword="true"/>。</returns>
        public bool CanTransition(LevelPhase from, LevelPhase to)
        {
            switch (from)
            {
                case LevelPhase.None:
                    return to == LevelPhase.Preparing;

                case LevelPhase.Preparing:
                    return to == LevelPhase.Intro;

                case LevelPhase.Intro:
                    return to == LevelPhase.Combat;

                case LevelPhase.Combat:
                    return to == LevelPhase.Cleared ||
                           to == LevelPhase.Failed;

                case LevelPhase.Cleared:
                case LevelPhase.Failed:
                    return to == LevelPhase.Exiting;

                default:
                    return false;
            }
        }
    }
}
