using System;
using System.Collections.Generic;
using Train.Gameplay.Combat.Factions;
using Train.Gameplay.Combat.HitEffects;
using UnityEngine;

namespace Train.Gameplay.Combat
{
    /// <summary>
    /// 剑的触发器伤害源。同一次攻击对同一生命体最多造成一次伤害。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class SwordHitbox : MonoBehaviour, IDamageSource, IFactionMember
    {
        [SerializeField, Min(0f)] private float _damage = 25f;
        [SerializeField, Min(0f)] private float _impactForce = 2f;
        [SerializeField] private DamageType _damageType = DamageType.Physical;
        [SerializeField] private Transform _ownerTransform;
        [SerializeField] private CombatFaction _faction = CombatFaction.Player;

        private readonly HashSet<IDamageable> _hitTargets = new();
        private Collider _trigger;
        private IWeaponHitEffect[] _hitEffects = Array.Empty<IWeaponHitEffect>();

        public Transform SourceTransform => transform;
        public Transform OwnerTransform => _ownerTransform;
        public float Damage => _damage;
        public DamageType DamageType => _damageType;
        public bool IsDamageActive => _trigger != null && _trigger.enabled;
        public CombatFaction Faction
        {
            get
            {
                var ownerFaction = FactionResolver.FindInParents(_ownerTransform);
                return ownerFaction != null && !ReferenceEquals(ownerFaction, this)
                    ? ownerFaction.Faction
                    : _faction;
            }
        }

        private void Awake()
        {
            _trigger = GetComponent<Collider>();
            _trigger.isTrigger = true;
            _trigger.enabled = false;
            _ownerTransform ??= transform.root;
            CacheHitEffects();
        }

        private void OnDisable()
        {
            _hitTargets.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            TryDamage(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryDamage(other);
        }

        public void BeginAttack()
        {
            _hitTargets.Clear();
            if (_trigger == null)
            {
                _trigger = GetComponent<Collider>();
            }

            _trigger.enabled = true;
        }

        public void EndAttack()
        {
            if (_trigger != null)
            {
                _trigger.enabled = false;
            }
        }

        public void Configure(Transform ownerTransform, float damage, float impactForce = 2f)
        {
            _ownerTransform = ownerTransform;
            _damage = Mathf.Max(0f, damage);
            _impactForce = Mathf.Max(0f, impactForce);
        }

        public void ConfigureFaction(CombatFaction faction)
        {
            _faction = faction;
        }

        /// <summary>
        /// 配置武器造成的伤害类型。
        /// 雷剑等元素武器通过配置数据调用此方法，不需要复制命中盒逻辑。
        /// </summary>
        /// <param name="damageType">新的伤害类型。</param>
        public void ConfigureDamageType(DamageType damageType)
        {
            _damageType = damageType;
        }

        /// <summary>
        /// 只更新武器基础伤害，不改变所有者、冲击力和伤害类型。
        /// 装备属性绑定器在换装后调用此方法。
        /// </summary>
        /// <param name="damage">新的非负基础伤害。</param>
        public void SetDamage(float damage)
        {
            _damage = Mathf.Max(0f, damage);
        }

        private void TryDamage(Collider other)
        {
            if (!IsDamageActive || other == null)
            {
                return;
            }

            if (_ownerTransform != null &&
                (other.transform == _ownerTransform ||
                 other.transform.IsChildOf(_ownerTransform)))
            {
                return;
            }

            var behaviours = other.GetComponentsInParent<MonoBehaviour>(true);
            foreach (var behaviour in behaviours)
            {
                if (behaviour is not IDamageable damageable ||
                    !damageable.IsAlive ||
                    _hitTargets.Contains(damageable))
                {
                    continue;
                }

                var targetFaction = FactionResolver.FindInParents(behaviour.transform);
                if (!DamagePolicy.CanDamage(this, targetFaction))
                {
                    break;
                }

                var closestPoint = other.ClosestPoint(transform.position);
                var direction = other.bounds.center - transform.position;
                var damageInfo = new DamageInfo(
                    _damage,
                    this,
                    closestPoint,
                    direction,
                    _impactForce,
                    _damageType);

                _hitTargets.Add(damageable);
                var result = DamageHandler.Apply(
                    damageable,
                    damageInfo,
                    targetFaction);
                NotifyHitEffects(damageable, damageInfo, result);
                break;
            }
        }

        /// <summary>
        /// 缓存与命中盒挂在同一物体上的可插拔命中特效。
        /// </summary>
        private void CacheHitEffects()
        {
            var behaviours = GetComponents<MonoBehaviour>();
            var effects = new List<IWeaponHitEffect>();
            foreach (var behaviour in behaviours)
            {
                if (behaviour is IWeaponHitEffect effect)
                {
                    effects.Add(effect);
                }
            }

            _hitEffects = effects.ToArray();
        }

        /// <summary>
        /// 仅在目标实际受到伤害后通知武器附加效果。
        /// </summary>
        private void NotifyHitEffects(
            IDamageable target,
            DamageInfo damageInfo,
            DamageResult result)
        {
            if (result.AppliedDamage <= 0f)
            {
                return;
            }

            foreach (var hitEffect in _hitEffects)
            {
                hitEffect.OnDamageApplied(target, damageInfo, result);
            }
        }
    }
}
