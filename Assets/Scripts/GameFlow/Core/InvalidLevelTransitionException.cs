using System;

namespace Train.GameFlow.Core
{
    /// <summary>
    /// 请求了转换策略不允许的关卡阶段跳转时抛出的异常。
    /// </summary>
    public sealed class InvalidLevelTransitionException : InvalidOperationException
    {
        /// <summary>
        /// 创建一条非法关卡阶段转换异常。
        /// </summary>
        public InvalidLevelTransitionException(LevelPhase from, LevelPhase to)
            : base($"Level phase cannot transition from {from} to {to}.")
        {
            From = from;
            To = to;
        }

        /// <summary>获取转换前阶段。</summary>
        public LevelPhase From { get; }

        /// <summary>获取被拒绝的目标阶段。</summary>
        public LevelPhase To { get; }
    }
}
