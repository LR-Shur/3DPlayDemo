using Train.Gameplay.Combat;
using Train.Gameplay.Enemy.Abstractions;
using Train.Gameplay.Enemy.Data;
using UnityEngine;

namespace Train.Gameplay.Enemy.Core
{
    /// <summary>
    /// 敌人根控制器，负责组装配置、状态机并驱动死亡表现。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class EnemyController : MonoBehaviour
    {
        [SerializeField] private EnemyConfig _config;
        [SerializeField] private MonoBehaviour _sensorComponent;
        [SerializeField] private MonoBehaviour _motorComponent;
        [SerializeField] private MonoBehaviour _combatComponent;
        [SerializeField] private MonoBehaviour _animationComponent;
        [SerializeField] private MonoBehaviour _patrolComponent;
        [SerializeField] private MonoBehaviour _lifecycleComponent;
        [SerializeField] private string _currentStateName;

        private Health _health;
        private IEnemySensor _sensor;
        private EnemyStateMachine _stateMachine;
        private EnemyConfig _runtimeConfig;

        public string CurrentStateName => _currentStateName;
        public EnemyConfig Config => _config;

        private void Awake()
        {
            _health = GetComponent<Health>();
            BuildStateMachine();
        }

        /// <summary>构建敌人状态机，供初始配置和运行时 Luban 覆盖共同使用。</summary>
        private void BuildStateMachine()
        {
            _sensor = Resolve<IEnemySensor>(_sensorComponent);
            var motor = Resolve<IEnemyMotor>(_motorComponent);
            var combat = Resolve<IEnemyCombat>(_combatComponent);
            var animation = Resolve<IEnemyAnimation>(_animationComponent);
            var patrol = Resolve<IEnemyPatrol>(_patrolComponent);
            var lifecycle = Resolve<IEnemyLifecycle>(_lifecycleComponent);

            if (_config == null || _sensor == null || motor == null || combat == null ||
                animation == null || patrol == null || lifecycle == null)
            {
                Debug.LogError(
                    "EnemyController requires config, sensor, motor, combat, animation, patrol and lifecycle services.",
                    this);
                enabled = false;
                return;
            }

            var context = new EnemyContext(
                _sensor,
                motor,
                combat,
                animation,
                patrol,
                lifecycle,
                _health,
                _config);
            _stateMachine = new EnemyStateMachine(context);
            _stateMachine.ChangeState(_stateMachine.Idle);
        }

        /// <summary>应用敌人原型生成的运行时配置，并重建状态机上下文。</summary>
        public void ApplyRuntimeConfig(EnemyConfig config)
        {
            if (config == null)
            {
                return;
            }

            _config = config;
            if (_runtimeConfig != null)
            {
                Destroy(_runtimeConfig);
            }

            _runtimeConfig = config;
            BuildStateMachine();
        }

        private void OnEnable()
        {
            _health ??= GetComponent<Health>();
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (_health == null)
            {
                return;
            }

            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }

        private void OnDestroy()
        {
            if (_runtimeConfig != null)
            {
                Destroy(_runtimeConfig);
                _runtimeConfig = null;
            }
        }

        private void Update()
        {
            _sensor?.Tick();
            _stateMachine?.Tick();
            _currentStateName = _stateMachine?.CurrentState?.GetType().Name ?? string.Empty;
        }

        private void OnDamaged(DamageInfo damageInfo, DamageResult result)
        {
            if (_stateMachine != null && !result.Killed)
            {
                _stateMachine.ChangeState(_stateMachine.Hit);
            }
        }

        private void OnDied(DamageInfo damageInfo)
        {
            if (_stateMachine != null)
            {
                _stateMachine.ChangeState(_stateMachine.Death);
            }
        }

        private T Resolve<T>(MonoBehaviour preferred) where T : class
        {
            if (preferred is T resolved)
            {
                return resolved;
            }

            foreach (var behaviour in GetComponents<MonoBehaviour>())
            {
                if (behaviour is T service)
                {
                    return service;
                }
            }

            return null;
        }
    }
}
