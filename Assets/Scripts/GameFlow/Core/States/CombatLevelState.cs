using System.Threading;
using System.Threading.Tasks;

namespace Train.GameFlow.Core.States
{
    /// <summary>在战斗阶段开启战斗逻辑，并在离开时将其关闭。</summary>
    internal sealed class CombatLevelState : LevelFlowState
    {
        /// <summary>创建战斗状态。</summary>
        public CombatLevelState(LevelFlowContext context)
            : base(context)
        {
        }

        /// <inheritdoc />
        public override LevelPhase Phase => LevelPhase.Combat;

        /// <inheritdoc />
        public override Task EnterAsync(CancellationToken cancellationToken)
        {
            return Context.Actions.SetCombatEnabledAsync(
                true,
                cancellationToken);
        }

        /// <inheritdoc />
        public override Task ExitAsync(CancellationToken cancellationToken)
        {
            return Context.Actions.SetCombatEnabledAsync(
                false,
                cancellationToken);
        }
    }
}
