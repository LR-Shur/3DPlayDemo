using Train.Gameplay.Enemy.Core;
using UnityEngine;

namespace Train.Gameplay.Enemy.States
{
    /// <summary>
    /// 敌人攻击状态，持续短暂时间后回到追击或闲置。
    /// </summary>
    public sealed class EnemyAttackState : EnemyState
    {
        private float _elapsed;
        private bool _damageActive;

        public EnemyAttackState(EnemyStateMachine machine, EnemyContext context)
            : base(machine, context)
        {
        }

        public override void Enter()
        {
            _elapsed = 0f;
            _damageActive = false;
            Context.Motor.Stop();
            Context.Combat.BeginAttack();
            Context.Animation.Play(EnemyAnimationId.Attack, 0.08f);
        }

        public override void Tick()
        {
            _elapsed += Time.deltaTime;

            if (Context.Sensor.HasTarget)
            {
                Context.Motor.Face(Context.Sensor.Target.position, Context.Config.TurnSpeed);
            }

            var shouldDamage = _elapsed >= Context.Config.HitboxStartTime &&
                               _elapsed <= Context.Config.HitboxEndTime;
            if (shouldDamage != _damageActive)
            {
                _damageActive = shouldDamage;
                Context.Combat.SetDamageActive(_damageActive);
            }

            if (_elapsed < Context.Config.AttackDuration)
            {
                return;
            }

            Context.Combat.EndAttack();
            EnemyMachine.ChangeState(
                Context.Sensor.HasTarget ? EnemyMachine.Chase : EnemyMachine.Idle);
        }

        public override void Exit()
        {
            _damageActive = false;
            Context.Combat.EndAttack();
        }
    }
}
