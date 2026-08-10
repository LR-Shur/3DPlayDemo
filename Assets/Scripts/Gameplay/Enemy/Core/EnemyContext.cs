using Train.Gameplay.Combat;
using Train.Gameplay.Enemy.Abstractions;
using Train.Gameplay.Enemy.Data;

namespace Train.Gameplay.Enemy.Core
{
    /// <summary>
    /// 敌人状态机共享的上下文，保存移动、感知、战斗和动画能力。
    /// </summary>
    public sealed class EnemyContext
    {
        public EnemyContext(
            IEnemySensor sensor,
            IEnemyMotor motor,
            IEnemyCombat combat,
            IEnemyAnimation animation,
            IEnemyPatrol patrol,
            IEnemyLifecycle lifecycle,
            Health health,
            EnemyConfig config)
        {
            Sensor = sensor;
            Motor = motor;
            Combat = combat;
            Animation = animation;
            Patrol = patrol;
            Lifecycle = lifecycle;
            Health = health;
            Config = config;
        }

        public IEnemySensor Sensor { get; }
        public IEnemyMotor Motor { get; }
        public IEnemyCombat Combat { get; }
        public IEnemyAnimation Animation { get; }
        public IEnemyPatrol Patrol { get; }
        public IEnemyLifecycle Lifecycle { get; }
        public Health Health { get; }
        public EnemyConfig Config { get; }
    }
}
