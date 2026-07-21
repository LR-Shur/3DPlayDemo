namespace Train.Gameplay.Common.StateMachine
{
    /// <summary>
    /// 定义层次状态机所需的通用生命周期和父子状态关系。
    /// 状态机持有顶层状态，每个状态可持有一个当前子状态。
    /// </summary>
    public abstract class StateBase<TContext>
    {
        /// <summary>
        /// 使用所属状态机和游戏上下文初始化状态。
        /// </summary>
        protected StateBase(StateMachineBase<TContext> machine, TContext context)
        {
            Machine = machine;
            Context = context;
        }

        /// <summary>
        /// 获取拥有此状态的状态机。
        /// </summary>
        protected StateMachineBase<TContext> Machine { get; }

        /// <summary>
        /// 获取此状态使用的游戏服务与数据。
        /// </summary>
        protected TContext Context { get; }

        /// <summary>
        /// 获取当前子状态；若此状态不是父状态则为空。
        /// </summary>
        protected StateBase<TContext> ChildState { get; private set; }

        /// <summary>
        /// 在状态激活时执行一次。
        /// </summary>
        public virtual void Enter()
        {
        }

        /// <summary>
        /// 更新此状态，然后更新其当前子状态。
        /// </summary>
        public virtual void Tick()
        {
            ChildState?.Tick();
        }

        /// <summary>
        /// 在状态被替换前执行一次，并清理当前子状态。
        /// </summary>
        public virtual void Exit()
        {
            ChildState?.Exit();
            ChildState = null;
        }

        /// <summary>
        /// 使用另一个子状态替换当前子状态。
        /// </summary>
        /// <param name="nextState">即将激活的子状态。</param>
        protected void SetChildState(StateBase<TContext> nextState)
        {
            if (nextState == null || ReferenceEquals(ChildState, nextState))
            {
                return;
            }

            ChildState?.Exit();
            ChildState = nextState;
            ChildState.Enter();
        }
    }
}
