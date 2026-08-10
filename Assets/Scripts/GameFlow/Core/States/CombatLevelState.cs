using System.Threading;
using System.Threading.Tasks;

namespace Train.GameFlow.Core.States
{
    /// <summary>
    /// 在战斗阶段开启战斗逻辑，并在离开时将其关闭。
    /// </summary>
    internal sealed class CombatLevelState : LevelFlowState
    {
        /// <inheritdoc />
        public override LevelPhase Phase => LevelPhase.Combat;

        /// <inheritdoc />
        public override Task EnterAsync(
            ILevelFlowActions actions,
            CancellationToken cancellationToken)
        {
            return actions.SetCombatEnabledAsync(
                true,
                cancellationToken);
        }

        /// <inheritdoc />
        public override Task ExitAsync(
            ILevelFlowActions actions,
            CancellationToken cancellationToken)
        {
            return actions.SetCombatEnabledAsync(
                false,
                cancellationToken);
        }
    }
}
