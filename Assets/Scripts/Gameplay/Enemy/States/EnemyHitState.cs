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
        private float _duration;
        private float _nextDuration;

        public EnemyHitState(EnemyStateMachine machine, EnemyContext context)
            : base(machine, context)
        {
        }

        public override void Enter()
        {
            _elapsed = 0f;
            _duration = _nextDuration > 0f
                ? _nextDuration
                : Context.Config.GetHitReactionDuration(
                    0f,
                    Train.Gameplay.Combat.DamageType.Physical);
            _nextDuration = 0f;
            Context.Motor.Stop();
            Context.Combat.EndAttack();
            Context.Animation.Play(EnemyAnimationId.Hit, 0.05f);
        }

        public void SetNextDuration(float duration)
        {
            _nextDuration = Mathf.Clamp(duration, .18f, .5f);
        }

        /// <summary>连续飞刃再次命中时延长僵直，但不重启整套状态和攻击动画。</summary>
        public void Refresh(float duration = -1f)
        {
            _elapsed = 0f;
            if (duration > 0f)
            {
                _duration = Mathf.Clamp(duration, .18f, .5f);
            }
            Context.Motor.Stop();
            Context.Combat.EndAttack();
        }

        public override void Tick()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed < _duration)
            {
                return;
            }

            EnemyMachine.ChangeState(
                Context.Sensor.HasTarget ? EnemyMachine.Chase : EnemyMachine.Idle);
        }
    }
}
