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
                _damageAppliedThisAttack = true;
                ApplyWeaponHit();
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

        private void ApplyWeaponHit()
        {
            if (_hitVolume == null)
            {
                return;
            }

            var hitTransform = _hitVolume.transform;
            var center = hitTransform.TransformPoint(_hitVolume.center);
            var scale = hitTransform.lossyScale;
            var radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            var radius = _hitVolume.radius * radiusScale;
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

                foreach (var behaviour in hit.GetComponentsInParent<MonoBehaviour>(true))
                {
                    if (behaviour is not IDamageable damageable || !damageable.IsAlive)
                    {
                        continue;
                    }

                    var targetFaction = FactionResolver.FindInParents(behaviour.transform);
                    if (!DamagePolicy.CanDamage(this, targetFaction))
                    {
                        break;
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
                    DamageHandler.Apply(damageable, info, targetFaction);
                    return;
                }
            }
        }
    }
}
