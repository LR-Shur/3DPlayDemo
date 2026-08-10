using System.Threading;
using System.Threading.Tasks;

namespace Train.Gameplay.Common.StateMachine
{
    /// <summary>
    /// 定义可异步进入和退出的状态生命周期。
    /// </summary>
    public interface IAsyncState<TContext>
    {
        /// <summary>进入状态并执行一次性初始化。</summary>
        Task EnterAsync(CancellationToken cancellationToken);

        /// <summary>退出状态并释放本状态持有的运行时工作。</summary>
        Task ExitAsync(CancellationToken cancellationToken);
    }
}
