using System.Threading;
using System.Threading.Tasks;

namespace Train.GameFlow.Core.States
{
    /// <summary>在准备阶段请求场景完成资源和参战对象初始化。</summary>
    internal sealed class PreparingLevelState : LevelFlowState
    {
        /// <summary>创建准备状态。</summary>
        public PreparingLevelState(LevelFlowContext context)
            : base(context)
        {
        }

        /// <inheritdoc />
        public override LevelPhase Phase => LevelPhase.Preparing;

        /// <inheritdoc />
        public override Task EnterAsync(CancellationToken cancellationToken)
        {
            return Context.Actions.PrepareAsync(cancellationToken);
        }
    }
}
