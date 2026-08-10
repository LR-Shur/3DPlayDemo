using System;

namespace Train.Gameplay.Common.StateMachine
{
    /// <summary>通用异步状态机恢复失败异常。</summary>
    public sealed class StateMachineRecoveryException : Exception
    {
        /// <summary>创建包含原始转换异常和恢复异常的错误。</summary>
        public StateMachineRecoveryException(
            Exception transitionException,
            Exception recoveryException)
            : base(
                "State recovery failed after a transition error.",
                recoveryException)
        {
            TransitionException = transitionException;
            RecoveryException = recoveryException;
        }

        /// <summary>导致恢复流程开始的原始异常。</summary>
        public Exception TransitionException { get; }

        /// <summary>恢复流程抛出的异常。</summary>
        public Exception RecoveryException { get; }
    }
}
