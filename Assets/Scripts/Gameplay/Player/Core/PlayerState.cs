using Train.Gameplay.Common.StateMachine;
using Train.Gameplay.Player.Animation.Data;

namespace Train.Gameplay.Player.Core
{
    /// <summary>
    /// 提供玩家专用状态基类，避免具体玩家状态重复编写泛型类型参数。
    /// </summary>
    public abstract class PlayerState : StateBase<PlayerContext>
    {
        /// <summary>
        /// 使用所属玩家状态机和共享上下文初始化玩家状态。
        /// </summary>
        protected PlayerState(PlayerStateMachine machine, PlayerContext context)
            : base(machine, context)
        {
            PlayerMachine = machine;
        }

        /// <summary>
        /// 获取强类型的玩家状态机。
        /// </summary>
        protected PlayerStateMachine PlayerMachine { get; }

        /// <summary>
        /// 读取动画目录定义的位移策略，并将其配置给玩家移动组件。
        /// 所有玩家状态统一经由此方法决定动画位移，不在各状态中写死 Root Motion 规则。
        /// </summary>
        /// <param name="animationId">即将播放的动画标识。</param>
        protected void ConfigureAnimationMovement(PlayerAnimationId animationId)
        {
            if (Context.Animation.TryGetMovementSettings(
                    animationId,
                    out var movementPolicy,
                    out var rootMotionPositionScale))
            {
                Context.Motor.ConfigureAnimationMovement(movementPolicy, rootMotionPositionScale);
                return;
            }

            Context.Motor.ConfigureAnimationMovement(PlayerAnimationMovementPolicy.ScriptedMovement, 1f);
        }
    }
}
