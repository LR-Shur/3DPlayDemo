using System.Threading;
using System.Threading.Tasks;

namespace Train.GameFlow.Core.States
{
    /// <summary>表示通关或失败结果阶段，并请求场景展示对应结果。</summary>
    internal sealed class ResultLevelState : LevelFlowState
    {
        private readonly LevelPhase _phase;
        private readonly LevelOutcome _outcome;

        /// <summary>创建指定阶段与结果的结果状态。</summary>
        public ResultLevelState(
            LevelFlowContext context,
            LevelPhase phase,
            LevelOutcome outcome)
            : base(context)
        {
            _phase = phase;
            _outcome = outcome;
        }

        /// <inheritdoc />
        public override LevelPhase Phase => _phase;

        /// <inheritdoc />
        public override Task EnterAsync(CancellationToken cancellationToken)
        {
            return Context.Actions.PresentResultAsync(
                _outcome,
                cancellationToken);
        }
    }
}
