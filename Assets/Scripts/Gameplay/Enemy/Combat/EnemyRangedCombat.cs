using Train.Gameplay.Combat;
using Train.Gameplay.Combat.Factions;
using Train.Gameplay.Enemy.Abstractions;
using UnityEngine;

namespace Train.Gameplay.Enemy.Combat
{
    /// <summary>远程敌人：蓄力完成后从武器口发射实体弹道。</summary>
    [DisallowMultipleComponent]
    public sealed class EnemyRangedCombat : MonoBehaviour, IEnemyCombat, IEnemyAttackTelegraph, IDamageSource, IFactionMember
    {
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private Transform _muzzle;
        [SerializeField, Min(.1f)] private float _attackRange = 7f;
        [SerializeField, Min(0f)] private float _damage = 24f;
        [SerializeField, Min(0f)] private float _attackCooldown = 1.5f;
        [SerializeField, Min(0f)] private float _impactForce = .5f;
        [SerializeField, Min(.1f)] private float _projectileSpeed = 8f;
        [SerializeField, Min(.1f)] private float _projectileLifetime = 4f;
        [SerializeField, Min(.01f)] private float _projectileRadius = .13f;
        [SerializeField] private DamageType _damageType = DamageType.Electric;
        [SerializeField] private LayerMask _damageLayers = ~0;
        [SerializeField] private CombatFaction _faction = CombatFaction.Enemy;
        [SerializeField] private bool _telegraphEnabled = true;
        [SerializeField] private Color _telegraphColor = new(.65f, .08f, 1f, .92f);

        private float _nextAttackTime;
        private Transform _target;
        private bool _damageActive;
        private bool _projectileLaunched;
        private LineRenderer _telegraphRing;
        private MeshRenderer _telegraphFill;
        private Material _telegraphMaterial;
        private float _telegraphStartedAt;
        private float _telegraphDuration;

        public bool CanStartAttack => Time.time >= _nextAttackTime;
        public float AttackRange => _attackRange;
        public Transform SourceTransform => _muzzle != null ? _muzzle : transform;
        public Transform OwnerTransform => transform;
        public float Damage => _damage;
        public DamageType DamageType => _damageType;
        public bool IsDamageActive => _damageActive;
        public CombatFaction Faction => _faction;

        private void Awake()
        {
            _muzzle ??= transform;
            CreateTelegraphVisuals();
        }

        public void SetAttackTarget(Transform target) => _target = target;

        public void BeginAttack()
        {
            _nextAttackTime = Time.time + _attackCooldown;
            _damageActive = false;
            _projectileLaunched = false;
        }

        public void SetDamageActive(bool active)
        {
            _damageActive = active;
            if (!active || _projectileLaunched || _projectilePrefab == null) return;

            var direction = _target != null
                ? (_target.position + Vector3.up * .9f - SourceTransform.position).normalized
                : transform.forward;
            var projectile = Instantiate(_projectilePrefab, SourceTransform.position, Quaternion.LookRotation(direction));
            var logic = projectile.GetComponent<EnemyProjectile>();
            if (logic == null)
            {
                Debug.LogError("敌人弹道预制体缺少 EnemyProjectile。", projectile);
                Destroy(projectile);
                return;
            }

            logic.Initialize(transform, direction, _damage, _impactForce, _damageType, _faction,
                _damageLayers, _projectileSpeed, _projectileLifetime, _projectileRadius);
            _projectileLaunched = true;
        }

        public void EndAttack()
        {
            _damageActive = false;
            EndTelegraph();
        }

        public void ConfigureDamage(float damage) => _damage = Mathf.Max(0f, damage);

        public void BeginTelegraph(float duration, float attackRange)
        {
            if (!_telegraphEnabled || duration <= .02f) return;
            _telegraphDuration = duration;
            _telegraphStartedAt = Time.time;
            SetTelegraphGeometry(attackRange);
            SetTelegraphColor(new Color(.14f, .01f, .12f, .24f));
            _telegraphRing.enabled = true;
            _telegraphFill.enabled = true;
        }

        public void EndTelegraph()
        {
            _telegraphStartedAt = 0f;
            if (_telegraphRing != null) _telegraphRing.enabled = false;
            if (_telegraphFill != null) _telegraphFill.enabled = false;
        }

        private void OnDisable() => EndAttack();

        private void Update()
        {
            if (_telegraphStartedAt <= 0f || _telegraphRing == null) return;
            var progress = Mathf.Clamp01((Time.time - _telegraphStartedAt) / Mathf.Max(.02f, _telegraphDuration));
            SetTelegraphColor(Color.Lerp(new Color(.14f, .01f, .12f, .24f), _telegraphColor, progress));
            _telegraphFill.transform.localScale = Vector3.one * Mathf.Lerp(.78f, 1f, progress);
        }

        private void CreateTelegraphVisuals()
        {
            var root = new GameObject("RangedAttackTelegraph");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = Vector3.up * .035f;
            var ringObject = new GameObject("RangedAttackRangeRing");
            ringObject.transform.SetParent(root.transform, false);
            _telegraphRing = ringObject.AddComponent<LineRenderer>();
            _telegraphRing.useWorldSpace = false;
            _telegraphRing.positionCount = 48;
            _telegraphRing.widthMultiplier = .06f;
            _telegraphRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _telegraphRing.receiveShadows = false;
            _telegraphMaterial = new Material(Shader.Find("Sprites/Default"));
            _telegraphRing.material = _telegraphMaterial;
            var fillObject = new GameObject("RangedAttackRangeFill");
            fillObject.transform.SetParent(root.transform, false);
            _telegraphFill = fillObject.AddComponent<MeshRenderer>();
            _telegraphFill.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _telegraphFill.receiveShadows = false;
            _telegraphFill.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            fillObject.AddComponent<MeshFilter>().sharedMesh = BuildDiscMesh(48);
            _telegraphRing.enabled = false;
            _telegraphFill.enabled = false;
        }

        private void SetTelegraphGeometry(float radius)
        {
            for (var i = 0; i < _telegraphRing.positionCount; i++)
            {
                var angle = i / (float)(_telegraphRing.positionCount - 1) * Mathf.PI * 2f;
                _telegraphRing.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
            _telegraphFill.transform.localScale = Vector3.one * radius;
        }

        private void SetTelegraphColor(Color color)
        {
            _telegraphRing.startColor = color;
            _telegraphRing.endColor = color;
            _telegraphFill.sharedMaterial.color = new Color(color.r, color.g, color.b, color.a * .22f);
        }

        private static Mesh BuildDiscMesh(int segments)
        {
            var mesh = new Mesh { name = "RangedAttackTelegraphDisc" };
            var vertices = new Vector3[segments + 1];
            var triangles = new int[segments * 3];
            for (var i = 0; i < segments; i++)
            {
                var angle = i / (float)segments * Mathf.PI * 2f;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % segments + 1;
            }
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
