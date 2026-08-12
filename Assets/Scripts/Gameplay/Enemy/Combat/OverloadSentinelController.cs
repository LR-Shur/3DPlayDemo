using System;
using Train.Gameplay.Combat;
using Train.Gameplay.Combat.Factions;
using UnityEngine;

namespace Train.Gameplay.Enemy.Combat
{
    /// <summary>
    /// 过载哨兵的范围电弧技能。
    /// 先显示短暂的环形预警，再在技能中段对范围内玩家造成一次电属性伤害。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OverloadSentinelController : MonoBehaviour, IDamageSource, IFactionMember
    {
        [SerializeField, Min(0.5f)] private float _castInterval = 4.5f;
        [SerializeField, Min(0f)] private float _telegraphSeconds = 0.8f;
        [SerializeField, Min(0f)] private float _activeSeconds = 0.45f;
        [SerializeField, Min(0.5f)] private float _radius = 3.6f;
        [SerializeField, Min(0f)] private float _damage = 28f;
        [SerializeField, Min(0f)] private float _impactForce = 2f;
        [SerializeField] private DamageType _damageType = DamageType.Electric;
        [SerializeField] private LayerMask _damageLayers = ~0;
        [SerializeField] private Color _telegraphColor = new(0.15f, 0.85f, 1f, 0.9f);

        private readonly Collider[] _overlaps = new Collider[24];
        private LineRenderer _ring;
        private float _nextCastTime;
        private float _castStartedAt;
        private bool _damageApplied;
        private Health _health;

        public Transform SourceTransform => transform;
        public Transform OwnerTransform => transform;
        public float Damage => _damage;
        public DamageType DamageType => _damageType;
        public bool IsDamageActive { get; private set; }
        public CombatFaction Faction => CombatFaction.Enemy;

        private void Awake()
        {
            _health = GetComponent<Health>();
            CreateRing();
            _nextCastTime = Time.time + 1.8f;
        }

        private void OnDisable()
        {
            IsDamageActive = false;
            if (_ring != null)
            {
                _ring.enabled = false;
            }
        }

        private void Update()
        {
            if (_health != null && !_health.IsAlive)
            {
                IsDamageActive = false;
                _ring.enabled = false;
                return;
            }

            if (_castStartedAt <= 0f)
            {
                if (Time.time >= _nextCastTime)
                {
                    BeginCast();
                }

                return;
            }

            var elapsed = Time.time - _castStartedAt;
            IsDamageActive = elapsed >= _telegraphSeconds &&
                elapsed < _telegraphSeconds + _activeSeconds;
            _ring.enabled = elapsed < _telegraphSeconds + _activeSeconds;
            _ring.startColor = _telegraphColor;
            _ring.endColor = _telegraphColor;

            if (IsDamageActive && !_damageApplied)
            {
                _damageApplied = ApplyAreaDamage();
            }

            if (elapsed >= _telegraphSeconds + _activeSeconds)
            {
                _castStartedAt = 0f;
                _nextCastTime = Time.time + _castInterval;
                IsDamageActive = false;
                _ring.enabled = false;
            }
        }

        private void BeginCast()
        {
            _castStartedAt = Time.time;
            _damageApplied = false;
            _ring.positionCount = 32;
            _ring.widthMultiplier = 0.08f;
            for (var i = 0; i < _ring.positionCount; i++)
            {
                var angle = i / (float)(_ring.positionCount - 1) * Mathf.PI * 2f;
                _ring.SetPosition(i, new Vector3(
                    Mathf.Cos(angle) * _radius,
                    0.08f,
                    Mathf.Sin(angle) * _radius));
            }

            _ring.enabled = true;
        }

        private bool ApplyAreaDamage()
        {
            var hits = Physics.OverlapSphereNonAlloc(
                transform.position,
                _radius,
                _overlaps,
                _damageLayers,
                QueryTriggerInteraction.Ignore);

            var applied = false;
            for (var i = 0; i < hits; i++)
            {
                var hit = _overlaps[i];
                if (hit == null || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                var target = hit.GetComponentInParent<Health>();
                if (target == null || !target.IsAlive)
                {
                    continue;
                }

                var targetFaction = FactionResolver.FindInParents(target.transform);
                if (!DamagePolicy.CanDamage(this, targetFaction))
                {
                    continue;
                }

                var direction = target.transform.position - transform.position;
                var info = new DamageInfo(
                    _damage,
                    this,
                    target.transform.position,
                    direction,
                    _impactForce,
                    _damageType);
                var result = DamageHandler.Apply(target, info, targetFaction);
                applied |= result.AppliedDamage > 0f;
            }

            return applied;
        }

        private void CreateRing()
        {
            var ringObject = new GameObject("OverloadArc");
            ringObject.transform.SetParent(transform, false);
            _ring = ringObject.AddComponent<LineRenderer>();
            _ring.useWorldSpace = false;
            _ring.loop = false;
            _ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _ring.receiveShadows = false;
            _ring.material = new Material(Shader.Find("Sprites/Default"));
            _ring.startColor = _telegraphColor;
            _ring.endColor = _telegraphColor;
            _ring.enabled = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.15f, 0.85f, 1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
