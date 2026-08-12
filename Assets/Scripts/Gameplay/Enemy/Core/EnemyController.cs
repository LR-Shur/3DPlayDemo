using Train.Gameplay.Combat;
using Train.Gameplay.Enemy.Abstractions;
using Train.Gameplay.Enemy.Data;
using Train.Gameplay.Enemy.Animation;
using Train.Gameplay.Enemy.Presentation;
using Train.Gameplay.Enemy.States;
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
        private EnemyHitReaction _hitReaction;
        private IEnemyMotor _motor;
        private IEnemyCombat _combat;
        private EnemyAnimator _enemyAnimator;
        private EnemyFreezeVisual _freezeVisual;
        private float _freezeUntil;
        private bool _isFrozen;

        public string CurrentStateName => _currentStateName;
        public EnemyConfig Config => _config;
        public bool IsFrozen => _isFrozen;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _hitReaction = GetComponent<EnemyHitReaction>() ?? gameObject.AddComponent<EnemyHitReaction>();
            _freezeVisual = GetComponent<EnemyFreezeVisual>() ?? gameObject.AddComponent<EnemyFreezeVisual>();
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

            _motor = motor;
            _combat = combat;
            _enemyAnimator = animation as EnemyAnimator;

            var context = new EnemyContext(
                _sensor,
                motor,
                combat,
                animation,
                patrol,
                lifecycle,
                _health,
                _config,
                _hitReaction);
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
            if (_isFrozen)
            {
                _isFrozen = false;
                _freezeUntil = 0f;
                _motor?.SetMovementEnabled(true);
                _enemyAnimator?.SetPlaybackPaused(false);
                _freezeVisual?.SetFrozen(false);
            }

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
            if (_isFrozen)
            {
                if (Time.time >= _freezeUntil)
                {
                    ResumeFromFreeze();
                }

                return;
            }

            _sensor?.Tick();
            _stateMachine?.Tick();
            _currentStateName = _stateMachine?.CurrentState?.GetType().Name ?? string.Empty;
        }

        /// <summary>冻结敌人指定秒数；重复命中会刷新结束时间。</summary>
        public bool Freeze(float seconds)
        {
            if (_health == null || !_health.IsAlive || _stateMachine == null)
            {
                return false;
            }

            _freezeUntil = Mathf.Max(_freezeUntil, Time.time + Mathf.Max(.1f, seconds));
            if (!_isFrozen)
            {
                _isFrozen = true;
                _motor?.SetMovementEnabled(false);
                _combat?.EndAttack();
                _enemyAnimator?.SetPlaybackPaused(true);
                _freezeVisual?.SetFrozen(true);
            }

            return true;
        }

        /// <summary>将敌人向主动道具中心拉近，位移在 EnemyMotor 的 LateUpdate 应用。</summary>
        public void ApplyPull(Vector3 center, float strength)
        {
            if (_isFrozen || _health == null || !_health.IsAlive || _motor == null)
            {
                return;
            }

            var direction = center - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= .01f)
            {
                return;
            }

            var distance = direction.magnitude;
            _motor.ApplyExternalPull(direction.normalized * Mathf.Min(distance, Mathf.Max(0f, strength) * Time.deltaTime));
        }

        private void ResumeFromFreeze()
        {
            _isFrozen = false;
            _freezeUntil = 0f;
            if (_health == null || !_health.IsAlive)
            {
                return;
            }

            _motor?.SetMovementEnabled(true);
            _enemyAnimator?.SetPlaybackPaused(false);
            _freezeVisual?.SetFrozen(false);
            _stateMachine.ChangeState(_sensor != null && _sensor.HasTarget
                ? _stateMachine.Chase
                : _stateMachine.Idle);
        }

        private void OnDamaged(DamageInfo damageInfo, DamageResult result)
        {
            if (_stateMachine != null && !result.Killed)
            {
                _hitReaction?.PlayHit(damageInfo.HitDirection);
                if (_stateMachine.CurrentState is EnemyHitState hitState)
                {
                    hitState.Refresh();
                }
                else
                {
                    _stateMachine.ChangeState(_stateMachine.Hit);
                }
            }
        }

        private void OnDied(DamageInfo damageInfo)
        {
            _isFrozen = false;
            _freezeUntil = 0f;
            _motor?.SetMovementEnabled(true);
            _enemyAnimator?.SetPlaybackPaused(false);
            _freezeVisual?.SetFrozen(false);
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
