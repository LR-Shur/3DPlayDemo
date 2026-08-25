using Train.Gameplay.Enemy.Core;

namespace Train.Gameplay.Enemy.States
{
    /// <summary>
    /// 敌人追击状态，追踪感知目标并进入攻击范围时切换攻击。
    /// </summary>
    public sealed class EnemyChaseState : EnemyState
    {
        public EnemyChaseState(EnemyStateMachine machine, EnemyContext context)
            : base(machine, context)
        {
        }

        public override void Enter()
        {
            Context.Animation.Play(EnemyAnimationId.Run);
        }

        public override void Tick()
        {
            if (!Context.Sensor.HasTarget)
            {
                EnemyMachine.ChangeState(EnemyMachine.Idle);
                return;
            }

            var targetPosition = Context.Sensor.Target.position;
            Context.Motor.Face(targetPosition, Context.Config.TurnSpeed);
            if (Context.Sensor.DistanceToTarget <= Context.Combat.AttackRange)
            {
                Context.Motor.Stop();
                if (Context.Combat.CanStartAttack)
                {
                    EnemyMachine.ChangeState(EnemyMachine.Attack);
                }

                return;
            }

            Context.Motor.MoveTo(targetPosition, Context.Config.ChaseSpeed);
        }

        public override void Exit()
        {
            Context.Motor.Stop();
        }
    }
}
