using UnityEngine;

namespace Train.Gameplay.Enemy.Data
{
    /// <summary>
    /// 敌人基础数值与行为配置，供关卡和预制体复用。
    /// </summary>
    [CreateAssetMenu(menuName = "Train/Enemy/Enemy Config", fileName = "EnemyConfig")]
    public sealed class EnemyConfig : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float _patrolSpeed = 1.5f;
        [SerializeField, Min(0f)] private float _chaseSpeed = 3.5f;
        [SerializeField, Min(0f)] private float _turnSpeed = 10f;
        [SerializeField, Min(0f)] private float _idleSeconds = 1.5f;

        [Header("Combat")]
        [SerializeField, Min(0.1f)] private float _attackRange = 0.9f;
        [SerializeField, Min(0.1f)] private float _attackDuration = 0.9f;
        [SerializeField, Min(0f)] private float _hitboxStartTime = 0.22f;
        [SerializeField, Min(0f)] private float _hitboxEndTime = 0.55f;
        [SerializeField, Min(0f)] private float _hitReactionDuration = 0.45f;
        [SerializeField, Min(0f)] private float _deathDespawnDelay = 2.2f;

        public float PatrolSpeed => _patrolSpeed;
        public float ChaseSpeed => _chaseSpeed;
        public float TurnSpeed => _turnSpeed;
        public float IdleSeconds => _idleSeconds;
        public float AttackRange => _attackRange;
        public float AttackDuration => _attackDuration;
        public float HitboxStartTime => Mathf.Min(_hitboxStartTime, _attackDuration);
        public float HitboxEndTime => Mathf.Clamp(_hitboxEndTime, HitboxStartTime, _attackDuration);
        public float HitReactionDuration => _hitReactionDuration;
        public float DeathDespawnDelay => _deathDespawnDelay;

        /// <summary>按 Luban 的移动速度创建运行时副本，不修改项目中的原始配置资产。</summary>
        public static EnemyConfig CreateRuntime(EnemyConfig baseline, float moveSpeed)
        {
            if (baseline == null)
            {
                return null;
            }

            var runtime = Instantiate(baseline);
            runtime.name = $"{baseline.name}_Runtime";
            runtime._patrolSpeed = Mathf.Max(0f, moveSpeed);
            runtime._chaseSpeed = Mathf.Max(0f, moveSpeed);
            return runtime;
        }
    }
}
