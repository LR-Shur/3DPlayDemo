using Train.Gameplay.Combat;
using Train.Gameplay.Combat.Factions;
using UnityEngine;

namespace Train.Gameplay.Enemy.Combat
{
    /// <summary>新敌人的轻量特殊攻击：无人机为远程电击，冰晶猎手为近距离冰霜爆发。</summary>
    [DisallowMultipleComponent]
    public sealed class EnemySpecialAbilityController : MonoBehaviour, IDamageSource
    {
        // 新敌人能力共用同一套预警和阵营结算。
        [SerializeField] private DamageType _damageType = DamageType.Electric;
        [SerializeField] private Color _color = new(.1f, .8f, 1f, .9f);
        [SerializeField, Min(1f)] private float _interval = 5f;
        [SerializeField, Min(.1f)] private float _radius = 2.8f;
        [SerializeField, Min(0f)] private float _damage = 18f;
        [SerializeField, Min(.1f)] private float _telegraph = 0.8f;
        private float _next;
        private float _started;
        private LineRenderer _ring;
        private readonly Collider[] _hits = new Collider[16];
        public Transform SourceTransform => transform;
        public Transform OwnerTransform => transform;
        public float Damage => _damage;
        public DamageType DamageType => _damageType;
        public bool IsDamageActive { get; private set; }

        private void Awake() { _next = Time.time + 2.5f; _ring = CreateRing(); CreateThemeLook(); }
        private void Update()
        {
            if (_started <= 0f) { if (Time.time >= _next) { _started = Time.time; _ring.enabled = true; } return; }
            var elapsed = Time.time - _started; var total = _telegraph + .25f; var progress = Mathf.Clamp01(elapsed / total); _ring.transform.localScale = Vector3.one * Mathf.Lerp(.25f, 1f, progress); IsDamageActive = elapsed >= _telegraph && elapsed < total;
            if (IsDamageActive) { ApplyDamage(); IsDamageActive = false; }
            if (elapsed >= total) { _started = 0f; _next = Time.time + _interval; _ring.enabled = false; _ring.transform.localScale = Vector3.one; }
        }
        private void ApplyDamage()
        {
            var count = Physics.OverlapSphereNonAlloc(transform.position, _radius, _hits, ~0, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var health = _hits[i]?.GetComponentInParent<Health>();
                if (health == null || !health.IsAlive) continue;
                var faction = FactionResolver.FindInParents(health.transform);
                if (faction == null || faction.Faction != CombatFaction.Player) continue;
                DamageHandler.Apply(
                    health,
                    new DamageInfo(_damage, this, health.transform.position, health.transform.position - transform.position, 2f, _damageType),
                    faction);
            }
        }
        private LineRenderer CreateRing()
        {
            var go = new GameObject("SpecialAttackWarning"); go.transform.SetParent(transform, false); go.transform.localPosition = Vector3.up * .08f; var ring = go.AddComponent<LineRenderer>(); ring.useWorldSpace = false; ring.positionCount = 32; ring.widthMultiplier = .08f; ring.material = new Material(Shader.Find("Sprites/Default")); ring.startColor = _color; ring.endColor = _color; for (var i = 0; i < ring.positionCount; i++) { var a = i * Mathf.PI * 2f / (ring.positionCount - 1); ring.SetPosition(i, new Vector3(Mathf.Cos(a) * _radius, 0f, Mathf.Sin(a) * _radius)); } ring.enabled = false; return ring;
        }

        private void CreateThemeLook()
        {
            var root = new GameObject(_damageType == DamageType.Electric ? "ArcDroneShell" : "CryoHoundShell");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = Vector3.up * 1.35f;
            if (_damageType == DamageType.Electric)
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                body.transform.SetParent(root.transform, false);
                body.transform.localScale = new Vector3(.95f, .42f, .95f);
                PaintTheme(body, _color);
                for (var i = 0; i < 3; i++)
                {
                    var arc = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    arc.transform.SetParent(root.transform, false);
                    arc.transform.localPosition = new Vector3(Mathf.Cos(i * 2.1f) * .7f, 0f, Mathf.Sin(i * 2.1f) * .7f);
                    arc.transform.localScale = new Vector3(.08f, .08f, .75f);
                    arc.transform.localRotation = Quaternion.Euler(0f, i * 60f, 35f);
                    PaintTheme(arc, new Color(1f, .7f, .15f));
                }
            }
            else
            {
                for (var i = 0; i < 4; i++)
                {
                    var crystal = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    crystal.transform.SetParent(root.transform, false);
                    crystal.transform.localPosition = new Vector3(Mathf.Cos(i * 1.57f) * .55f, .2f, Mathf.Sin(i * 1.57f) * .55f);
                    crystal.transform.localScale = new Vector3(.18f, .85f, .18f);
                    crystal.transform.localRotation = Quaternion.Euler(0f, i * 35f, i % 2 == 0 ? 20f : -20f);
                    PaintTheme(crystal, _color);
                }
            }
        }

        private static void PaintTheme(GameObject target, Color color)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer == null) return;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            renderer.sharedMaterial = material;
            target.GetComponent<Collider>().enabled = false;
        }
    }
}
