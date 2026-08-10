using System;

namespace Train.GameFlow.Core
{
    /// <summary>
    /// 当阶段转换失败，并且尽力恢复前一状态也失败时抛出的异常。
    /// 此异常发生后，状态机会进入故障状态。
    /// </summary>
    public sealed class LevelFlowRecoveryException : InvalidOperationException
    {
        /// <summary>
        /// 创建一条包含转换异常与恢复异常的关卡流程恢复异常。
        /// </summary>
        public LevelFlowRecoveryException(
            LevelPhase phase,
            Exception transitionException,
            Exception recoveryException)
            : base(
                $"Level flow failed while leaving {phase}, then failed to restore it.",
                new AggregateException(transitionException, recoveryException))
        {
            Phase = phase;
            TransitionException = transitionException;
            RecoveryException = recoveryException;
        }

        /// <summary>获取恢复失败时原本所在的阶段。</summary>
        public LevelPhase Phase { get; }

        /// <summary>获取最初导致转换失败的异常。</summary>
        public Exception TransitionException { get; }

        /// <summary>获取恢复前一状态时发生的异常。</summary>
        public Exception RecoveryException { get; }
    }
}
