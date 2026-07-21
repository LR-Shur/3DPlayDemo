using Train.Gameplay.Common.StateMachine;

namespace Train.Gameplay.Player.Core
{
    /// <summary>
    /// 将通用状态机基类具体化为玩家状态机。
    /// 后续敌人可使用敌人上下文继承相同基类。
    /// </summary>
    public sealed class PlayerStateMachine : StateMachineBase<PlayerContext>
    {
        /// <summary>
        /// 保存玩家上下文，供玩家控制器创建状态时使用。
        /// </summary>
        public PlayerStateMachine(PlayerContext context)
        {
            Context = context;
        }

        /// <summary>
        /// 获取该玩家所有状态共享的服务与数据。
        /// </summary>
        public PlayerContext Context { get; }
    }
}
