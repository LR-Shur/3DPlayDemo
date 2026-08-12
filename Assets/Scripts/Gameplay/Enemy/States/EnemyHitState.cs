using Train.Gameplay.Enemy.Core;
using UnityEngine;

namespace Train.Gameplay.Enemy.States
{
    /// <summary>
    /// 敌人受击状态，短暂停顿后回到追击。
    /// </summary>
    public sealed class EnemyHitState : EnemyState
    {
        private float _elapsed;

        public EnemyHitState(EnemyStateMachine machine, EnemyContext context)
            : base(machine, context)
        {
        }

        public override void Enter()
        {
            _elapsed = 0f;
            Context.Motor.Stop();
            Context.Combat.EndAttack();
            Context.Animation.Play(EnemyAnimationId.Hit, 0.05f);
        }

        /// <summary>连续飞刃再次命中时延长僵直，但不重启整套状态和攻击动画。</summary>
        public void Refresh()
        {
            _elapsed = 0f;
            Context.Motor.Stop();
            Context.Combat.EndAttack();
        }

        public override void Tick()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed < Context.Config.HitReactionDuration)
            {
                return;
            }

            EnemyMachine.ChangeState(
                Context.Sensor.HasTarget ? EnemyMachine.Chase : EnemyMachine.Idle);
        }
    }
}
