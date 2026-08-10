using Train.Gameplay.Common.StateMachine;

namespace Train.Gameplay.Enemy.Core
{
    /// <summary>
    /// 敌人状态机中单个状态的基类，提供上下文与状态机引用。
    /// </summary>
    public abstract class EnemyState : StateBase<EnemyContext>
    {
        protected EnemyState(EnemyStateMachine machine, EnemyContext context)
            : base(machine, context)
        {
            EnemyMachine = machine;
        }

        protected EnemyStateMachine EnemyMachine { get; }
    }
}
