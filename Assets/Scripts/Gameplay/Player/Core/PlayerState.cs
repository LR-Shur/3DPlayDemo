using Train.Gameplay.Common.StateMachine;

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
    }
}
