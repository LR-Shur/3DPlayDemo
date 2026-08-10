using System.Threading;
using System.Threading.Tasks;

namespace Train.GameFlow.Core.States
{
    /// <summary>在开场阶段请求场景播放关卡介绍。</summary>
    internal sealed class IntroLevelState : LevelFlowState
    {
        /// <summary>创建开场状态。</summary>
        public IntroLevelState(LevelFlowContext context)
            : base(context)
        {
        }

        /// <inheritdoc />
        public override LevelPhase Phase => LevelPhase.Intro;

        /// <inheritdoc />
        public override Task EnterAsync(CancellationToken cancellationToken)
        {
            return Context.Actions.PlayIntroAsync(cancellationToken);
        }
    }
}
