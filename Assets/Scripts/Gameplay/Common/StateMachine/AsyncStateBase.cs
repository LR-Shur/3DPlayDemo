using System.Threading;
using System.Threading.Tasks;

namespace Train.Gameplay.Common.StateMachine
{
    /// <summary>
    /// 为异步状态提供共享上下文和默认退出行为。
    /// 关卡、加载流程等非帧驱动状态机可以直接复用此基类。
    /// </summary>
    public abstract class AsyncStateBase<TContext> : IAsyncState<TContext>
    {
        /// <summary>使用状态共享上下文创建状态。</summary>
        protected AsyncStateBase(TContext context)
        {
            Context = context;
        }

        /// <summary>获取状态共享的业务上下文。</summary>
        protected TContext Context { get; }

        /// <inheritdoc />
        public abstract Task EnterAsync(CancellationToken cancellationToken);

        /// <inheritdoc />
        public virtual Task ExitAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
