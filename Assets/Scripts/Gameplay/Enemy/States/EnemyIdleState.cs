using Train.Gameplay.Enemy.Core;
using UnityEngine;

namespace Train.Gameplay.Enemy.States
{
    /// <summary>
    /// 敌人闲置状态，发现目标后进入追击。
    /// </summary>
    public sealed class EnemyIdleState : EnemyState
    {
        private float _elapsed;

        public EnemyIdleState(EnemyStateMachine machine, EnemyContext context)
            : base(machine, context)
        {
        }

        public override void Enter()
        {
            _elapsed = 0f;
            Context.Motor.Stop();
            Context.Animation.Play(EnemyAnimationId.Idle);
        }

        public override void Tick()
        {
            if (Context.Sensor.HasTarget)
            {
                EnemyMachine.ChangeState(EnemyMachine.Chase);
                return;
            }

            _elapsed += Time.deltaTime;
            if (_elapsed >= Context.Config.IdleSeconds)
            {
                EnemyMachine.ChangeState(EnemyMachine.Patrol);
            }
        }
    }
}
