using System.Threading;
using System.Threading.Tasks;

namespace Train.GameFlow.Core.States
{
    /// <summary>在退出阶段请求场景执行关卡离开流程。</summary>
    internal sealed class ExitingLevelState : LevelFlowState
    {
        /// <summary>创建退出状态。</summary>
        public ExitingLevelState(LevelFlowContext context)
            : base(context)
        {
        }

        /// <inheritdoc />
        public override LevelPhase Phase => LevelPhase.Exiting;

        /// <inheritdoc />
        public override Task EnterAsync(CancellationToken cancellationToken)
        {
            return Context.Actions.ExitAsync(cancellationToken);
        }
    }
}
