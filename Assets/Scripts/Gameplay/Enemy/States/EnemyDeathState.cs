using Train.Gameplay.Enemy.Core;

namespace Train.Gameplay.Enemy.States
{
    /// <summary>
    /// 敌人死亡状态，播放死亡动画并触发延迟销毁。
    /// </summary>
    public sealed class EnemyDeathState : EnemyState
    {
        public EnemyDeathState(EnemyStateMachine machine, EnemyContext context)
            : base(machine, context)
        {
        }

        public override void Enter()
        {
            Context.Sensor.ClearTarget();
            Context.Combat.EndAttack();
            Context.Motor.Stop();
            Context.Motor.SetMovementEnabled(false);
            Context.Animation.Play(EnemyAnimationId.Death, 0.08f);
            Context.Lifecycle.DespawnAfter(Context.Config.DeathDespawnDelay);
        }
    }
}
