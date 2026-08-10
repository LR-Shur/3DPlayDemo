using System.Threading;
using System.Threading.Tasks;

namespace Train.GameFlow.Core.States
{
    /// <summary>
    /// 定义一个可进入、可退出的关卡流程状态。
    /// </summary>
    internal interface ILevelFlowState
    {
        /// <summary>获取此状态对应的关卡阶段。</summary>
        LevelPhase Phase { get; }

        /// <summary>执行进入此状态时的异步动作。</summary>
        Task EnterAsync(
            ILevelFlowActions actions,
            CancellationToken cancellationToken);

        /// <summary>执行离开此状态时的异步动作。</summary>
        Task ExitAsync(
            ILevelFlowActions actions,
            CancellationToken cancellationToken);
    }
}
