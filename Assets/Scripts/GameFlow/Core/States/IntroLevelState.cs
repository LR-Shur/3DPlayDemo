using System.Threading;
using System.Threading.Tasks;

namespace Train.GameFlow.Core.States
{
    /// <summary>
    /// 在开场阶段请求场景播放关卡介绍。
    /// </summary>
    internal sealed class IntroLevelState : LevelFlowState
    {
        /// <inheritdoc />
        public override LevelPhase Phase => LevelPhase.Intro;

        /// <inheritdoc />
        public override Task EnterAsync(
            ILevelFlowActions actions,
            CancellationToken cancellationToken)
        {
            return actions.PlayIntroAsync(cancellationToken);
        }
    }
}
