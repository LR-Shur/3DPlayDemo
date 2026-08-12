using Train.Gameplay.Enemy.Core;
using UnityEngine;

namespace Train.Gameplay.Combat.ActiveItems
{
    /// <summary>重力诱导器：短距离投掷后生成重力井，持续把敌人拉到中心。</summary>
    public sealed class ActiveGravityGrenade : MonoBehaviour
    {
        private const float TravelSeconds = .38f;
        private const float FieldSeconds = 1.6f;

        private Vector3 _direction;
        private float _radius;
        private float _pullStrength;
        private float _duration;
        private float _elapsed;
        private bool _fieldActive;
        private LineRenderer _ring;
        private MeshRenderer _core;
        private Material _ringMaterial;
        private Material _coreMaterial;
        private ParticleSystem _particles;
        private TrailRenderer _trail;
        private readonly System.Collections.Generic.List<Transform> _orbiters = new();
        private LineRenderer _burstRing;
        private float _visualPulse;

        public void Initialize(Vector3 direction, float radius, float pullStrength, float duration)
        {
            _direction = direction.normalized;
            _radius = Mathf.Max(1f, radius);
            _pullStrength = Mathf.Max(.1f, pullStrength);
            _duration = Mathf.Clamp(duration, .3f, FieldSeconds);
            _elapsed = 0f;
            UpdateRingGeometry();
            UpdateBurstRingGeometry();
        }

        private void Awake()
        {
            CreateVisuals();
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            if (!_fieldActive)
            {
                transform.position += _direction * (6f * Time.deltaTime);
                transform.Rotate(Vector3.up, 240f * Time.deltaTime, Space.World);
                if (_elapsed >= TravelSeconds)
                {
                    _fieldActive = true;
                    _elapsed = 0f;
                    transform.position += Vector3.down * .85f;
                    SetFieldVisuals(true);
                    _burstRing.enabled = true;
                    _particles.Play();
                }

                return;
            }

            foreach (var enemy in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            {
                if (enemy == null ||
                    Vector3.Distance(transform.position, enemy.transform.position) > _radius)
                {
                    continue;
                }

                enemy.ApplyPull(transform.position, _pullStrength);
            }

            var pulse = 1f + Mathf.Sin(Time.time * 8f) * .06f;
            _visualPulse += Time.deltaTime * 1.8f;
            _ringMaterial.SetFloat("_Pulse", _visualPulse);
            _coreMaterial.SetFloat("_Pulse", _visualPulse);
            _ring.transform.localScale = Vector3.one * pulse;
            _core.transform.localScale = Vector3.one * (.55f + pulse * .08f);
            _trail.time = .28f + Mathf.Sin(Time.time * 6f) * .04f;
            for (var i = 0; i < _orbiters.Count; i++)
            {
                var angle = Time.time * (1.2f + i * .15f) + i * Mathf.PI * 2f / _orbiters.Count;
                _orbiters[i].localPosition = new Vector3(
                    Mathf.Cos(angle) * (.5f + i * .08f),
                    .15f + Mathf.Sin(angle * 1.7f) * .18f,
                    Mathf.Sin(angle) * (.5f + i * .08f));
                _orbiters[i].Rotate(Vector3.one, 180f * Time.deltaTime, Space.Self);
            }
            var burstProgress = Mathf.Clamp01(_elapsed / .42f);
            _burstRing.transform.localScale = Vector3.one * Mathf.Lerp(.25f, 1f, burstProgress);
            _burstRing.startColor = new Color(.7f, .1f, 1f, 1f - burstProgress);
            _burstRing.endColor = _burstRing.startColor;
            if (_elapsed >= _duration)
            {
                Destroy(gameObject);
            }
        }

        private void CreateVisuals()
        {
            var coreObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            coreObject.name = "GravityCoreVisual";
            coreObject.transform.SetParent(transform, false);
            coreObject.transform.localScale = Vector3.one * .28f;
            Destroy(coreObject.GetComponent<Collider>());
            _core = coreObject.GetComponent<MeshRenderer>();
            _coreMaterial = new Material(Shader.Find("Train/Active Field") ?? Shader.Find("Universal Render Pipeline/Unlit"));
            _coreMaterial.color = new Color(.5f, .05f, 1f, 1f);
            _core.sharedMaterial = _coreMaterial;
            _trail = coreObject.AddComponent<TrailRenderer>();
            _trail.time = .28f;
            _trail.widthMultiplier = .18f;
            _trail.minVertexDistance = .02f;
            _trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _trail.receiveShadows = false;
            _trail.sharedMaterial = _coreMaterial;
            _particles = CreateParticles(transform, new Color(.7f, .12f, 1f, 1f));
            CreateOrbiters();

            var ringObject = new GameObject("GravityWellRing");
            ringObject.transform.SetParent(transform, false);
            ringObject.transform.localPosition = Vector3.down * .85f;
            _ring = ringObject.AddComponent<LineRenderer>();
            _ring.useWorldSpace = false;
            _ring.positionCount = 65;
            _ring.widthMultiplier = .07f;
            _ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _ring.receiveShadows = false;
            _ringMaterial = new Material(Shader.Find("Sprites/Default"));
            if (Shader.Find("Train/Active Field") != null)
            {
                _ringMaterial = new Material(Shader.Find("Train/Active Field"));
            }
            _ringMaterial.color = new Color(.6f, .08f, 1f, .9f);
            _ring.material = _ringMaterial;
            UpdateRingGeometry();
            _burstRing = CreateRing("GravityBurstRing", _radius * .8f, .13f, new Color(.8f, .16f, 1f, .9f));
            _burstRing.transform.localPosition = Vector3.down * .83f;
            _burstRing.enabled = false;
            UpdateBurstRingGeometry();

            SetFieldVisuals(false);
        }

        private void SetFieldVisuals(bool enabled)
        {
            _ring.enabled = enabled;
            _core.enabled = true;
            _core.transform.localScale = enabled ? Vector3.one * .4f : Vector3.one * .28f;
            _particles.gameObject.SetActive(enabled);
            for (var i = 0; i < _orbiters.Count; i++) _orbiters[i].gameObject.SetActive(enabled);
        }

        private void CreateOrbiters()
        {
            for (var i = 0; i < 6; i++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = $"GravityShard_{i:00}";
                shard.transform.SetParent(transform, false);
                shard.transform.localScale = new Vector3(.06f, .18f, .06f);
                Destroy(shard.GetComponent<Collider>());
                shard.GetComponent<MeshRenderer>().sharedMaterial = _coreMaterial;
                _orbiters.Add(shard.transform);
                shard.SetActive(false);
            }
        }

        private static LineRenderer CreateRing(string name, float radius, float width, Color color)
        {
            var ringObject = new GameObject(name);
            var ring = ringObject.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.positionCount = 65;
            ring.widthMultiplier = width;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;
            ring.material = new Material(Shader.Find("Sprites/Default"));
            ring.startColor = color;
            ring.endColor = color;
            for (var i = 0; i < ring.positionCount; i++)
            {
                var angle = i / 64f * Mathf.PI * 2f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
            return ring;
        }

        private static ParticleSystem CreateParticles(Transform parent, Color color)
        {
            var particleObject = new GameObject("GravityEnergyParticles");
            particleObject.transform.SetParent(parent, false);
            var particles = particleObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.35f, .8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.3f, 1.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(.035f, .12f);
            main.startColor = color;
            main.maxParticles = 42;
            var emission = particles.emission;
            emission.rateOverTime = 30f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .3f;
            var renderer = particleObject.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            return particles;
        }

        private void UpdateRingGeometry()
        {
            if (_ring == null)
            {
                return;
            }

            for (var i = 0; i < _ring.positionCount; i++)
            {
                var angle = i / 64f * Mathf.PI * 2f;
                _ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * _radius, 0f, Mathf.Sin(angle) * _radius));
            }
        }

        private void UpdateBurstRingGeometry()
        {
            if (_burstRing == null)
            {
                return;
            }

            for (var i = 0; i < _burstRing.positionCount; i++)
            {
                var angle = i / 64f * Mathf.PI * 2f;
                _burstRing.SetPosition(i, new Vector3(
                    Mathf.Cos(angle) * _radius * .8f,
                    0f,
                    Mathf.Sin(angle) * _radius * .8f));
            }
        }

        private void OnDestroy()
        {
            if (_ringMaterial != null) Destroy(_ringMaterial);
            if (_coreMaterial != null) Destroy(_coreMaterial);
            if (_burstRing != null && _burstRing.material != null) Destroy(_burstRing.material);
        }
    }
}
