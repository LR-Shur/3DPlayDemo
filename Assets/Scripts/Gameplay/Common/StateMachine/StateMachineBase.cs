namespace Train.Gameplay.Common.StateMachine
{
    /// <summary>
    /// 为任意游戏对象提供可复用的顶层状态切换与更新流程。
    /// 玩家和敌人状态机可通过各自的上下文类型继承此类。
    /// </summary>
    public abstract class StateMachineBase<TContext>
    {
        /// <summary>
        /// 获取当前激活的顶层状态。
        /// </summary>
        public StateBase<TContext> CurrentState { get; private set; }

        /// <summary>
        /// 更新当前状态及其所有激活的子状态。
        /// </summary>
        public void Tick()
        {
            CurrentState?.Tick();
        }

        /// <summary>
        /// 退出当前状态并进入指定的新顶层状态。
        /// </summary>
        /// <param name="nextState">即将激活的状态。</param>
        public void ChangeState(StateBase<TContext> nextState)
        {
            if (nextState == null || ReferenceEquals(CurrentState, nextState))
            {
                return;
            }

            CurrentState?.Exit();
            CurrentState = nextState;
            CurrentState.Enter();
        }
    }
}
