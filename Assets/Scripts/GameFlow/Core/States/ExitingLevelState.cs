using System.Threading;
using System.Threading.Tasks;

namespace Train.GameFlow.Core.States
{
    /// <summary>
    /// 在退出阶段请求场景执行关卡离开流程。
    /// </summary>
    internal sealed class ExitingLevelState : LevelFlowState
    {
        /// <inheritdoc />
        public override LevelPhase Phase => LevelPhase.Exiting;

        /// <inheritdoc />
        public override Task EnterAsync(
            ILevelFlowActions actions,
            CancellationToken cancellationToken)
        {
            return actions.ExitAsync(cancellationToken);
        }
    }
}
