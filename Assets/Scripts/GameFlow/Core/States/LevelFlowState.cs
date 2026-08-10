using System.Threading;
using System.Threading.Tasks;

namespace Train.GameFlow.Core.States
{
    /// <summary>
    /// 为关卡流程状态提供默认退出行为的抽象基类。
    /// </summary>
    internal abstract class LevelFlowState : ILevelFlowState
    {
        /// <summary>获取派生状态对应的关卡阶段。</summary>
        public abstract LevelPhase Phase { get; }

        /// <summary>执行进入派生状态时的动作。</summary>
        public abstract Task EnterAsync(
            ILevelFlowActions actions,
            CancellationToken cancellationToken);

        /// <summary>
        /// 执行离开状态时的动作；默认无需额外处理。
        /// </summary>
        public virtual Task ExitAsync(
            ILevelFlowActions actions,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
