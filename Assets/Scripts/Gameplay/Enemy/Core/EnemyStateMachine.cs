using Train.Gameplay.Common.StateMachine;
using Train.Gameplay.Enemy.States;

namespace Train.Gameplay.Enemy.Core
{
    /// <summary>
    /// 敌人专用状态机，管理闲置、巡逻、追击、受击、攻击与死亡状态。
    /// </summary>
    public sealed class EnemyStateMachine : StateMachineBase<EnemyContext>
    {
        public EnemyStateMachine(EnemyContext context)
        {
            Context = context;
            Idle = new EnemyIdleState(this, context);
            Patrol = new EnemyPatrolState(this, context);
            Chase = new EnemyChaseState(this, context);
            Attack = new EnemyAttackState(this, context);
            Hit = new EnemyHitState(this, context);
            Death = new EnemyDeathState(this, context);
        }

        public EnemyContext Context { get; }
        public EnemyIdleState Idle { get; }
        public EnemyPatrolState Patrol { get; }
        public EnemyChaseState Chase { get; }
        public EnemyAttackState Attack { get; }
        public EnemyHitState Hit { get; }
        public EnemyDeathState Death { get; }
    }
}
