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
        [SerializeField] private string _weaponNodeName = "0005_Ellen_Weapon";

        private readonly HashSet<IDamageable> _hitTargets = new();
        private Collider _trigger;
        private Collider _localCollider;
        private IWeaponHitEffect[] _hitEffects = Array.Empty<IWeaponHitEffect>();

        /// <summary>伤害来源位置使用真实武器节点，便于命中特效和击退方向贴合刀身。</summary>
        public Transform SourceTransform => _trigger != null ? _trigger.transform : transform;
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
            _ownerTransform ??= transform.root;
            _localCollider = GetComponent<Collider>();
            _trigger = ResolveWeaponCollider();
            if (_trigger != null)
            {
                _trigger.isTrigger = true;
                _trigger.enabled = false;
                EnsureTriggerForwarder(_trigger.gameObject);
            }

            // 保留旧的占位碰撞体作为兜底，但实际攻击期间只启用武器节点上的碰撞体。
            if (_localCollider != null && _localCollider != _trigger)
            {
                _localCollider.enabled = false;
            }

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
                _trigger = ResolveWeaponCollider();
            }

            if (_trigger != null)
            {
                _trigger.enabled = true;
            }
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

        /// <summary>
        /// 仅在 Unity 场景视图选中武器时绘制命中盒，运行时不会显示，方便关卡调试。
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            var collider = _trigger != null
                ? _trigger
                : FindWeaponColliderInEditor();
            if (collider == null)
            {
                DrawWeaponRendererBounds();
                return;
            }

            Gizmos.color = new Color(1f, 0.25f, 0.12f, 0.8f);
            var previousMatrix = Gizmos.matrix;
            Gizmos.matrix = collider.transform.localToWorldMatrix;
            switch (collider)
            {
                case BoxCollider box:
                    Gizmos.DrawWireCube(box.center, box.size);
                    break;
                case SphereCollider sphere:
                    Gizmos.DrawWireSphere(sphere.center, sphere.radius);
                    break;
                case CapsuleCollider capsule:
                    Gizmos.DrawWireSphere(capsule.center, capsule.radius);
                    break;
            }

            Gizmos.matrix = previousMatrix;
        }

        /// <summary>优先查找真实的 0005_Ellen_Weapon 节点，并在其上复用或创建碰撞体。</summary>
        private Collider ResolveWeaponCollider()
        {
            var weapon = FindWeaponNode();
            if (weapon == null)
            {
                return _localCollider ?? GetComponent<Collider>();
            }

            var collider = weapon.GetComponent<Collider>();
            var createdAtRuntime = false;
            if (collider == null)
            {
                collider = weapon.gameObject.AddComponent<BoxCollider>();
                createdAtRuntime = true;
            }

            // 预制体中已经保存了按网格计算的碰撞体时保持美术侧尺寸；只有旧预制体运行时补建时才重新估算。
            if (createdAtRuntime && collider is BoxCollider box)
            {
                FitBoxToWeaponRenderers(box, weapon);
            }

            return collider;
        }

        /// <summary>编辑器 Gizmo 只读取已有碰撞体，不在绘制阶段修改场景。</summary>
        private Collider FindWeaponColliderInEditor()
        {
            var weapon = FindWeaponNode();
            return weapon != null ? weapon.GetComponent<Collider>() : _localCollider ?? GetComponent<Collider>();
        }

        private Transform FindWeaponNode()
        {
            if (string.IsNullOrWhiteSpace(_weaponNodeName) || transform.root == null)
            {
                return null;
            }

            var transforms = transform.root.GetComponentsInChildren<Transform>(true);
            foreach (var candidate in transforms)
            {
                if (string.Equals(candidate.name, _weaponNodeName, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }

        private void EnsureTriggerForwarder(GameObject target)
        {
            var forwarder = target.GetComponent<SwordHitboxTriggerForwarder>();
            if (forwarder == null)
            {
                forwarder = target.AddComponent<SwordHitboxTriggerForwarder>();
            }

            forwarder.Bind(this);
        }

        private static void FitBoxToWeaponRenderers(BoxCollider box, Transform weapon)
        {
            var renderers = weapon.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            var worldBounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                worldBounds.Encapsulate(renderers[i].bounds);
            }

            var localCenter = weapon.InverseTransformPoint(worldBounds.center);
            var localSize = weapon.InverseTransformVector(worldBounds.size);
            localSize = new Vector3(
                Mathf.Max(Mathf.Abs(localSize.x) + 0.04f, 0.05f),
                Mathf.Max(Mathf.Abs(localSize.y) + 0.04f, 0.05f),
                Mathf.Max(Mathf.Abs(localSize.z) + 0.04f, 0.05f));
            box.center = localCenter;
            box.size = localSize;
        }

        private void DrawWeaponRendererBounds()
        {
            var weapon = FindWeaponNode();
            if (weapon == null)
            {
                return;
            }

            var renderers = weapon.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            Gizmos.color = new Color(1f, 0.25f, 0.12f, 0.8f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        internal void HandleTrigger(Collider other)
        {
            TryDamage(other);
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

                var sourcePosition = SourceTransform.position;
                var closestPoint = other.ClosestPoint(sourcePosition);
                var direction = other.bounds.center - sourcePosition;
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

    /// <summary>
    /// 挂在真实武器碰撞体上的转发器。SwordHitbox 仍负责伤害策略，转发器只负责接收 Unity 物理回调，
    /// 这样碰撞体可以直接贴在 0005_Ellen_Weapon 网格节点上，而不会把战斗逻辑耦合到模型层。
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class SwordHitboxTriggerForwarder : MonoBehaviour
    {
        private SwordHitbox _owner;

        public void Bind(SwordHitbox owner)
        {
            _owner = owner;
        }

        private void OnTriggerEnter(Collider other)
        {
            _owner?.HandleTrigger(other);
        }

        private void OnTriggerStay(Collider other)
        {
            _owner?.HandleTrigger(other);
        }
    }
}
