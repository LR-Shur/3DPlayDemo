namespace Train.GameFlow.Core
{
    /// <summary>
    /// 定义关卡阶段之间允许的转换边界。
    /// </summary>
    public interface ILevelTransitionPolicy
    {
        /// <summary>
        /// 判断是否允许从一个阶段直接转换到另一个阶段。
        /// </summary>
        bool CanTransition(LevelPhase from, LevelPhase to);
    }
}
