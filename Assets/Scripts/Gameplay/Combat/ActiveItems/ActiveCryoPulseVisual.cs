using UnityEngine;

namespace Train.Gameplay.Combat.ActiveItems
{
    /// <summary>冻结道具的冰霜脉冲环，用于明确显示本次范围。</summary>
    public sealed class ActiveCryoPulseVisual : MonoBehaviour
    {
        private float _radius;
        private float _elapsed;
        private LineRenderer _ring;
        private LineRenderer _innerRing;
        private Material _material;
        private ParticleSystem _particles;
        private readonly System.Collections.Generic.List<Transform> _iceShards = new();

        public static void Spawn(Vector3 position, float radius)
        {
            var visual = new GameObject("CryoBeacon_Pulse");
            visual.transform.position = position + Vector3.up * .04f;
            var pulse = visual.AddComponent<ActiveCryoPulseVisual>();
            pulse._radius = Mathf.Max(1f, radius);
        }

        private void Awake()
        {
            var ringObject = new GameObject("CryoPulseRing");
            ringObject.transform.SetParent(transform, false);
            _ring = ringObject.AddComponent<LineRenderer>();
            _ring.useWorldSpace = false;
            _ring.positionCount = 65;
            _ring.widthMultiplier = .09f;
            _ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _ring.receiveShadows = false;
            _material = new Material(Shader.Find("Sprites/Default"));
            if (Shader.Find("Train/Cryo Pulse") != null)
            {
                _material = new Material(Shader.Find("Train/Cryo Pulse"));
            }
            _material.color = new Color(.08f, .65f, 1f, .95f);
            _ring.material = _material;

            _innerRing = CreateRing("CryoPulseInnerRing", .7f, .055f, new Color(.5f, .9f, 1f, .9f));
            _innerRing.transform.SetParent(transform, false);
            _particles = CreateParticles();
            for (var i = 0; i < 10; i++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = $"CryoShard_{i:00}";
                shard.transform.SetParent(transform, false);
                var angle = i * Mathf.PI * 2f / 10f;
                shard.transform.localPosition = new Vector3(Mathf.Cos(angle) * .55f, .08f + (i % 3) * .12f, Mathf.Sin(angle) * .55f);
                shard.transform.localScale = new Vector3(.04f, .22f, .04f);
                Destroy(shard.GetComponent<Collider>());
                shard.GetComponent<MeshRenderer>().sharedMaterial = _material;
                _iceShards.Add(shard.transform);
            }
        }

        private void Start()
        {
            for (var i = 0; i < _ring.positionCount; i++)
            {
                var angle = i / 64f * Mathf.PI * 2f;
                _ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * _radius, 0f, Mathf.Sin(angle) * _radius));
                _innerRing.SetPosition(i, new Vector3(Mathf.Cos(angle) * _radius, 0f, Mathf.Sin(angle) * _radius));
            }
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(_elapsed / .55f);
            _ring.transform.localScale = Vector3.one * Mathf.Lerp(.1f, 1.15f, progress);
            _innerRing.transform.localScale = Vector3.one * Mathf.Lerp(1.35f, .25f, progress);
            _material.color = new Color(.08f, .65f, 1f, 1f - progress);
            _material.SetFloat("_Pulse", _elapsed * 2.5f);
            _particles.transform.localScale = Vector3.one * Mathf.Lerp(.4f, 1.5f, progress);
            for (var i = 0; i < _iceShards.Count; i++)
            {
                _iceShards[i].Rotate(Vector3.up, (140f + i * 11f) * Time.deltaTime, Space.Self);
                _iceShards[i].localScale = Vector3.one * (1f + Mathf.Sin(_elapsed * 9f + i) * .18f);
            }
            if (_elapsed >= .55f)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }

        private LineRenderer CreateRing(string name, float radius, float width, Color color)
        {
            var ringObject = new GameObject(name);
            var ring = ringObject.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.positionCount = 65;
            ring.widthMultiplier = width;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;
            ring.material = _material;
            ring.startColor = color;
            ring.endColor = color;
            for (var i = 0; i < ring.positionCount; i++)
            {
                var angle = i / 64f * Mathf.PI * 2f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
            return ring;
        }

        private ParticleSystem CreateParticles()
        {
            var particleObject = new GameObject("CryoMistParticles");
            particleObject.transform.SetParent(transform, false);
            var particles = particleObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.3f, .9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.4f, 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(.025f, .11f);
            main.startColor = new Color(.4f, .9f, 1f, .95f);
            main.maxParticles = 80;
            var emission = particles.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 48) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = Mathf.Max(.2f, _radius * .25f);
            var renderer = particleObject.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = _material;
            return particles;
        }
    }
}
