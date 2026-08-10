using Train.Gameplay.Enemy.Core;
using UnityEngine;

namespace Train.Gameplay.Enemy.States
{
    /// <summary>
    /// 敌人巡逻状态，在巡逻区域移动并在发现目标后追击。
    /// </summary>
    public sealed class EnemyPatrolState : EnemyState
    {
        private Vector3 _destination;

        public EnemyPatrolState(EnemyStateMachine machine, EnemyContext context)
            : base(machine, context)
        {
        }

        public override void Enter()
        {
            if (!Context.Patrol.TryGetNextDestination(Context.Motor.Position, out _destination))
            {
                EnemyMachine.ChangeState(EnemyMachine.Idle);
                return;
            }

            Context.Animation.Play(EnemyAnimationId.Walk);
        }

        public override void Tick()
        {
            if (Context.Sensor.HasTarget)
            {
                EnemyMachine.ChangeState(EnemyMachine.Chase);
                return;
            }

            if (Context.Motor.HasReachedDestination)
            {
                EnemyMachine.ChangeState(EnemyMachine.Idle);
                return;
            }

            Context.Motor.MoveTo(_destination, Context.Config.PatrolSpeed);
            Context.Motor.Face(_destination, Context.Config.TurnSpeed);
        }

        public override void Exit()
        {
            Context.Motor.Stop();
        }
    }
}
