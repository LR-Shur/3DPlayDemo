using Train.Gameplay.Combat;
using Train.Gameplay.Combat.Factions;
using UnityEngine;

namespace Train.Gameplay.Combat
{
    /// <summary>
    /// 能源中继核心的场地脉冲。
    /// 蓄能时显示逐渐扩大的环形预警，释放时对范围内玩家造成一次电属性伤害。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RelayPulseController : MonoBehaviour, IDamageSource, IFactionMember
    {
        [SerializeField, Min(1f)] private float _pulseInterval = 7f;
        [SerializeField, Min(0f)] private float _telegraphSeconds = 1.4f;
        [SerializeField, Min(0f)] private float _activeSeconds = 0.35f;
        [SerializeField, Min(1f)] private float _radius = 6f;
        [SerializeField, Min(0f)] private float _damage = 22f;
        [SerializeField, Min(0f)] private float _impactForce = 1.5f;
        [SerializeField] private DamageType _damageType = DamageType.Electric;
        [SerializeField] private LayerMask _damageLayers = ~0;
        [SerializeField] private Color _pulseColor = new(0.2f, 0.8f, 1f, 0.85f);

        private readonly Collider[] _overlaps = new Collider[24];
        private LineRenderer _ring;
        private float _nextPulseTime;
        private float _pulseStartedAt;
        private bool _damageApplied;
        private Health _health;

        public Transform SourceTransform => transform;
        public Transform OwnerTransform => transform;
        public float Damage => _damage;
        public DamageType DamageType => _damageType;
        public bool IsDamageActive { get; private set; }
        public CombatFaction Faction => CombatFaction.Neutral;

        private void Awake()
        {
            _health = GetComponent<Health>();
            CreateRing();
            _nextPulseTime = Time.time + 2.5f;
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

            if (_pulseStartedAt <= 0f)
            {
                if (Time.time >= _nextPulseTime)
                {
                    BeginPulse();
                }

                return;
            }

            var elapsed = Time.time - _pulseStartedAt;
            var totalSeconds = _telegraphSeconds + _activeSeconds;
            var progress = Mathf.Clamp01(elapsed / totalSeconds);
            _ring.enabled = elapsed < totalSeconds;
            _ring.startColor = _pulseColor;
            _ring.endColor = _pulseColor;
            _ring.transform.localScale = Vector3.one * Mathf.Lerp(0.25f, 1f, progress);
            IsDamageActive = elapsed >= _telegraphSeconds && elapsed < totalSeconds;

            if (IsDamageActive && !_damageApplied)
            {
                _damageApplied = ApplyPulseDamage();
            }

            if (elapsed >= totalSeconds)
            {
                _pulseStartedAt = 0f;
                _nextPulseTime = Time.time + _pulseInterval;
                IsDamageActive = false;
                _ring.enabled = false;
                _ring.transform.localScale = Vector3.one;
            }
        }

        private void BeginPulse()
        {
            _pulseStartedAt = Time.time;
            _damageApplied = false;
            _ring.enabled = true;
        }

        private bool ApplyPulseDamage()
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
            var ringObject = new GameObject("RelayPulseWarning");
            ringObject.transform.SetParent(transform, false);
            ringObject.transform.localPosition = new Vector3(0f, -0.48f, 0f);
            _ring = ringObject.AddComponent<LineRenderer>();
            _ring.useWorldSpace = false;
            _ring.loop = false;
            _ring.positionCount = 48;
            _ring.widthMultiplier = 0.1f;
            _ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _ring.receiveShadows = false;
            _ring.material = new Material(Shader.Find("Sprites/Default"));
            _ring.startColor = _pulseColor;
            _ring.endColor = _pulseColor;
            for (var i = 0; i < _ring.positionCount; i++)
            {
                var angle = i / (float)(_ring.positionCount - 1) * Mathf.PI * 2f;
                _ring.SetPosition(i, new Vector3(
                    Mathf.Cos(angle) * _radius,
                    0f,
                    Mathf.Sin(angle) * _radius));
            }

            _ring.enabled = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
