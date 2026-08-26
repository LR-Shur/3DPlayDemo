using Train.Gameplay.Enemy.Core;
using UnityEngine;

namespace Train.Gameplay.Enemy.States
{
    /// <summary>
    /// 敌人受击状态，短暂停顿后回到追击。
    /// </summary>
    public sealed class EnemyHitState : EnemyState
    {
        private const float MinDuration = .18f;
        private const float MaxDuration = .42f;
        private const float MaxRefreshExtension = .12f;

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

        /// <summary>保留已消耗时间，并限制连续命中对剩余僵直的单次延长。</summary>
        public static float CalculateRefreshedDuration(
            float elapsed,
            float currentDuration,
            float requestedDuration)
        {
            var safeElapsed = Mathf.Max(0f, elapsed);
            var currentRemaining = Mathf.Max(0f, currentDuration - safeElapsed);
            var requestedRemaining = Mathf.Clamp(
                requestedDuration,
                MinDuration,
                MaxDuration);
            var maxRemaining = Mathf.Min(
                MaxDuration,
                currentRemaining + MaxRefreshExtension);
            var refreshedRemaining = Mathf.Max(
                currentRemaining,
                Mathf.Min(requestedRemaining, maxRemaining));
            return safeElapsed + refreshedRemaining;
        }

        public void SetNextDuration(float duration)
        {
            _nextDuration = Mathf.Clamp(duration, MinDuration, MaxDuration);
        }

        /// <summary>连续飞刃再次命中时延长僵直，但不重启整套状态和攻击动画。</summary>
        public void Refresh(float duration = -1f)
        {
            if (duration > 0f)
            {
                _duration = CalculateRefreshedDuration(
                    _elapsed,
                    _duration,
                    duration);
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
