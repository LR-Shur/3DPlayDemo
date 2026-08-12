using Train.Gameplay.Combat.Factions;
using UnityEngine;

namespace Train.Gameplay.Combat
{
    /// <summary>
    /// 关卡场地危险区：周期性蓄能、显示范围预警、释放一次元素伤害。
    /// 通过颜色和粒子形状区分电弧、冰霜等不同场地主题。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FieldHazardController : MonoBehaviour, IDamageSource, IFactionMember
    {
        [SerializeField, Min(1f)] private float _interval = 5.5f;
        [SerializeField, Min(0f)] private float _telegraphSeconds = 1.1f;
        [SerializeField, Min(0f)] private float _activeSeconds = 0.3f;
        [SerializeField, Min(1f)] private float _radius = 3.2f;
        [SerializeField, Min(0f)] private float _damage = 18f;
        [SerializeField, Min(0f)] private float _impactForce = 1f;
        [SerializeField] private DamageType _damageType = DamageType.Electric;
        [SerializeField] private Color _effectColor = new(0.15f, 0.85f, 1f, 0.9f);
        [SerializeField] private bool _verticalBeam = true;

        private readonly Collider[] _overlaps = new Collider[24];
        private LineRenderer _ring;
        private ParticleSystem _burst;
        private Light _light;
        private float _nextActivationTime;
        private float _activationStartedAt;
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
            CreateVisuals();
            _nextActivationTime = Time.time + 2f;
        }

        private void OnDisable()
        {
            IsDamageActive = false;
            if (_ring != null)
            {
                _ring.enabled = false;
            }

            if (_light != null)
            {
                _light.enabled = false;
            }
        }

        private void Update()
        {
            if (_health != null && !_health.IsAlive)
            {
                IsDamageActive = false;
                _ring.enabled = false;
                _light.enabled = false;
                return;
            }

            if (_activationStartedAt <= 0f)
            {
                if (Time.time >= _nextActivationTime)
                {
                    BeginActivation();
                }

                return;
            }

            var elapsed = Time.time - _activationStartedAt;
            var total = _telegraphSeconds + _activeSeconds;
            var progress = Mathf.Clamp01(elapsed / total);
            _ring.enabled = elapsed < total;
            _ring.transform.localScale = Vector3.one * Mathf.Lerp(0.35f, 1f, progress);
            _light.enabled = elapsed < total;
            _light.intensity = Mathf.Lerp(0.4f, 7f, progress);
            IsDamageActive = elapsed >= _telegraphSeconds && elapsed < total;

            if (IsDamageActive && !_damageApplied)
            {
                _damageApplied = ApplyDamage();
            }

            if (elapsed >= total)
            {
                _activationStartedAt = 0f;
                _nextActivationTime = Time.time + _interval;
                IsDamageActive = false;
                _ring.enabled = false;
                _light.enabled = false;
            }
        }

        private void BeginActivation()
        {
            _activationStartedAt = Time.time;
            _damageApplied = false;
            _ring.enabled = true;
            _light.enabled = true;
            _burst.Emit(24);
        }

        private bool ApplyDamage()
        {
            var count = Physics.OverlapSphereNonAlloc(
                transform.position,
                _radius,
                _overlaps,
                ~0,
                QueryTriggerInteraction.Ignore);
            var applied = false;
            for (var i = 0; i < count; i++)
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

                var info = new DamageInfo(
                    _damage,
                    this,
                    target.transform.position,
                    target.transform.position - transform.position,
                    _impactForce,
                    _damageType);
                var result = DamageHandler.Apply(target, info, targetFaction);
                applied |= result.AppliedDamage > 0f;
            }

            return applied;
        }

        private void CreateVisuals()
        {
            var ringObject = new GameObject("HazardWarningRing");
            ringObject.transform.SetParent(transform, false);
            ringObject.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            _ring = ringObject.AddComponent<LineRenderer>();
            _ring.useWorldSpace = false;
            _ring.positionCount = 40;
            _ring.widthMultiplier = 0.09f;
            _ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _ring.material = new Material(Shader.Find("Sprites/Default"));
            _ring.startColor = _effectColor;
            _ring.endColor = _effectColor;
            for (var i = 0; i < _ring.positionCount; i++)
            {
                var angle = i / (float)(_ring.positionCount - 1) * Mathf.PI * 2f;
                _ring.SetPosition(i, new Vector3(
                    Mathf.Cos(angle) * _radius,
                    0f,
                    Mathf.Sin(angle) * _radius));
            }

            var burstObject = new GameObject("HazardBurst");
            burstObject.transform.SetParent(transform, false);
            _burst = burstObject.AddComponent<ParticleSystem>();
            _burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _burst.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.5f;
            main.startLifetime = 0.5f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startSize3D = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.gravityModifier = 0f;
            main.startColor = _effectColor;
            main.maxParticles = 48;
            var emission = _burst.emission;
            emission.enabled = false;
            var shape = _burst.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = _radius * 0.35f;
            var renderer = _burst.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.material = new Material(Shader.Find("Sprites/Default"));

            var lightObject = new GameObject("HazardLight");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = Vector3.up * 1.2f;
            _light = lightObject.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.range = _radius * 1.8f;
            _light.color = _effectColor;
            _light.shadows = LightShadows.None;
            _ring.enabled = false;
            _light.enabled = false;

            if (_verticalBeam)
            {
                _light.transform.localScale = new Vector3(1f, 2.5f, 1f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(_effectColor.r, _effectColor.g, _effectColor.b, 0.25f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
