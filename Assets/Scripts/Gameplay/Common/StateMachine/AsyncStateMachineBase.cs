using System;
using System.Threading;
using System.Threading.Tasks;

namespace Train.Gameplay.Common.StateMachine
{
    /// <summary>
    /// 可复用的异步状态机基类。
    /// 它统一处理串行切换、重复请求、进入失败后的恢复和故障锁定，
    /// 业务状态机只需要提供状态集合、转换策略和领域事件。
    /// </summary>
    public abstract class StateMachineBase<TContext, TState>
        where TState : class, IAsyncState<TContext>
    {
        private readonly SemaphoreSlim _transitionGate = new SemaphoreSlim(1, 1);

        /// <summary>使用共享上下文创建异步状态机。</summary>
        protected StateMachineBase(TContext context)
        {
            Context = context;
        }

        /// <summary>获取所有状态共享的上下文。</summary>
        protected TContext Context { get; }

        /// <summary>获取当前状态；尚未启动时为空。</summary>
        public TState CurrentState { get; private set; }

        /// <summary>恢复失败后状态机将锁定，避免继续执行未知流程。</summary>
        public bool IsFaulted { get; private set; }

        /// <summary>
        /// 按领域策略串行切换状态。
        /// 状态机内部统一完成旧状态退出、目标状态进入及失败恢复。
        /// </summary>
        protected async Task<AsyncStateTransitionResult> TransitionAsync(
            TState targetState,
            Func<TState, TState, bool> canTransition,
            CancellationToken cancellationToken)
        {
            if (targetState == null)
            {
                throw new ArgumentNullException(nameof(targetState));
            }

            if (canTransition == null)
            {
                throw new ArgumentNullException(nameof(canTransition));
            }

            await _transitionGate.WaitAsync(cancellationToken);
            try
            {
                ThrowIfFaulted();

                var previousState = CurrentState;
                if (ReferenceEquals(previousState, targetState))
                {
                    return AsyncStateTransitionResult.IgnoredSameState;
                }

                if (!canTransition(previousState, targetState))
                {
                    throw CreateInvalidTransitionException(
                        previousState,
                        targetState);
                }

                try
                {
                    if (previousState != null)
                    {
                        await previousState.ExitAsync(cancellationToken);
                    }

                    await targetState.EnterAsync(cancellationToken);
                }
                catch (Exception transitionException)
                {
                    await RestorePreviousStateAsync(
                        previousState,
                        transitionException);
                    throw;
                }

                CurrentState = targetState;
                OnStateChanged(previousState, targetState);
                return AsyncStateTransitionResult.Completed;
            }
            finally
            {
                _transitionGate.Release();
            }
        }

        /// <summary>为业务层创建领域化的非法转换异常。</summary>
        protected virtual Exception CreateInvalidTransitionException(
            TState previousState,
            TState targetState)
        {
            return new InvalidOperationException(
                "The requested state transition is not allowed.");
        }

        /// <summary>为业务层创建领域化的恢复失败异常。</summary>
        protected virtual Exception CreateRecoveryException(
            TState previousState,
            Exception transitionException,
            Exception recoveryException)
        {
            return new StateMachineRecoveryException(
                transitionException,
                recoveryException);
        }

        /// <summary>状态成功切换后由派生类发布领域事件或更新派生快照。</summary>
        protected virtual void OnStateChanged(
            TState previousState,
            TState currentState)
        {
        }

        private async Task RestorePreviousStateAsync(
            TState previousState,
            Exception transitionException)
        {
            if (previousState == null)
            {
                return;
            }

            try
            {
                // 恢复时忽略原调用方的取消，尽可能回到稳定状态。
                await previousState.EnterAsync(CancellationToken.None);
            }
            catch (Exception recoveryException)
            {
                IsFaulted = true;
                throw CreateRecoveryException(
                    previousState,
                    transitionException,
                    recoveryException);
            }
        }

        private void ThrowIfFaulted()
        {
            if (IsFaulted)
            {
                throw new InvalidOperationException(
                    "The state machine is faulted because state recovery failed.");
            }
        }
    }
}
