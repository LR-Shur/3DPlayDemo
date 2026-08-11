using Train.Gameplay.Combat;
using Train.Gameplay.Combat.Factions;
using Train.Gameplay.Enemy.Abstractions;
using UnityEngine;

namespace Train.Gameplay.Enemy.Combat
{
    /// <summary>
    /// 敌人近战攻击的触发伤害源，命中后向目标生命组件结算伤害。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyMeleeCombat : MonoBehaviour, IEnemyCombat, IDamageSource, IFactionMember
    {
        [SerializeField] private SphereCollider _hitVolume;
        [SerializeField, Min(0f)] private float _damage = 20f;
        [SerializeField, Min(0f)] private float _attackCooldown = 1.1f;
        [SerializeField, Min(0f)] private float _impactForce = 2f;
        [SerializeField] private DamageType _damageType = DamageType.Physical;
        [SerializeField] private LayerMask _damageLayers = ~0;
        [SerializeField] private CombatFaction _faction = CombatFaction.Enemy;

        private readonly Collider[] _overlaps = new Collider[24];
        private float _nextAttackTime;
        private bool _damageActive;
        private bool _damageAppliedThisAttack;
        private float _baseDamage;
        private float _baseAttackCooldown;

        public bool CanStartAttack => Time.time >= _nextAttackTime;
        public Transform SourceTransform => _hitVolume != null ? _hitVolume.transform : transform;
        public Transform OwnerTransform => transform;
        public float Damage => _damage;
        public DamageType DamageType => _damageType;
        public bool IsDamageActive => _damageActive;
        public CombatFaction Faction => _faction;

        private void Awake()
        {
            _baseDamage = _damage;
            _baseAttackCooldown = _attackCooldown;
            _hitVolume ??= GetComponentInChildren<SphereCollider>(true);
            if (_hitVolume != null)
            {
                _hitVolume.enabled = false;
            }
        }

        private void OnDisable()
        {
            EndAttack();
        }

        public void BeginAttack()
        {
            _nextAttackTime = Time.time + _attackCooldown;
            _damageActive = false;
            _damageAppliedThisAttack = false;
        }

        public void SetDamageActive(bool active)
        {
            _damageActive = active;
            if (active && !_damageAppliedThisAttack)
            {
                // 只有真正结算出伤害后才锁定本次攻击，避免命中检测过早导致整段攻击“空挥”。
                _damageAppliedThisAttack = ApplyWeaponHit();
            }
        }

        public void EndAttack()
        {
            _damageActive = false;
        }

        public void ConfigureFaction(CombatFaction faction)
        {
            _faction = faction;
        }

        /// <summary>
        /// 供 Boss 阶段调整攻击强度与节奏，始终基于初始配置计算，避免重复叠加误差。
        /// </summary>
        public void ApplyPhaseModifiers(
            float damageMultiplier,
            float cooldownMultiplier)
        {
            _damage = _baseDamage * Mathf.Max(0f, damageMultiplier);
            _attackCooldown = Mathf.Max(
                0.25f,
                _baseAttackCooldown * Mathf.Max(0.2f, cooldownMultiplier));
        }

        private bool ApplyWeaponHit()
        {
            if (_hitVolume == null)
            {
                return false;
            }

            var hitTransform = _hitVolume.transform;
            var center = hitTransform.TransformPoint(_hitVolume.center);
            var scale = hitTransform.lossyScale;
            var radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            var radius = _hitVolume.radius * radiusScale;
            if (TryApplyWeaponHit(center, radius))
            {
                return true;
            }

            // 动画还未把武器骨骼移动到手部时，使用角色前方的近战兜底范围，避免攻击完全不掉血。
            var fallbackCenter = transform.position + transform.forward * 0.45f + Vector3.up * 0.9f;
            return TryApplyWeaponHit(fallbackCenter, Mathf.Max(radius, 1.1f));
        }

        /// <summary>在指定范围内查找玩家生命组件并结算一次伤害。</summary>
        private bool TryApplyWeaponHit(Vector3 center, float radius)
        {
            var hitCount = Physics.OverlapSphereNonAlloc(
                center,
                radius,
                _overlaps,
                _damageLayers,
                QueryTriggerInteraction.Ignore);

            for (var i = 0; i < hitCount; i++)
            {
                var hit = _overlaps[i];
                if (hit == null || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                var damageable = hit.GetComponentInParent<Health>();
                if (damageable == null || !damageable.IsAlive)
                {
                    continue;
                }

                var targetFaction = FactionResolver.FindInParents(damageable.transform);
                if (!DamagePolicy.CanDamage(this, targetFaction))
                {
                    continue;
                }

                var point = hit.ClosestPoint(center);
                var direction = hit.bounds.center - center;
                var info = new DamageInfo(
                    _damage,
                    this,
                    point,
                    direction,
                    _impactForce,
                    _damageType);
                var result = DamageHandler.Apply(damageable, info, targetFaction);
                if (result.AppliedDamage > 0f)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
