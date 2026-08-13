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
    public sealed class EnemyMeleeCombat : MonoBehaviour, IEnemyCombat, IEnemyAttackTelegraph, IDamageSource, IFactionMember
    {
        [SerializeField] private Collider _hitVolume;
        [SerializeField, Min(0f)] private float _damage = 20f;
        [SerializeField, Min(0f)] private float _attackCooldown = 1.1f;
        [SerializeField, Min(0f)] private float _impactForce = 2f;
        [SerializeField] private DamageType _damageType = DamageType.Physical;
        [SerializeField] private LayerMask _damageLayers = ~0;
        [SerializeField] private CombatFaction _faction = CombatFaction.Enemy;
        [Header("Attack Telegraph")]
        [SerializeField] private bool _telegraphEnabled = true;
        [SerializeField, Min(0.1f)] private float _telegraphRadiusMultiplier = 1f;
        [SerializeField] private Color _telegraphColor = new(0.9f, 0.03f, 0.03f, 0.92f);

        private readonly Collider[] _overlaps = new Collider[24];
        private float _nextAttackTime;
        private bool _damageActive;
        private bool _damageAppliedThisAttack;
        private float _weaponAttackRange = .9f;
        private float _baseDamage;
        private float _baseAttackCooldown;
        private LineRenderer _telegraphRing;
        private MeshRenderer _telegraphFill;
        private Material _telegraphMaterial;
        private float _telegraphStartedAt;
        private float _telegraphDuration;
        private float _telegraphRadius;

        public bool CanStartAttack => Time.time >= _nextAttackTime;
        public float AttackRange => _weaponAttackRange;
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
            _hitVolume ??= FindWeaponCollider();
            _weaponAttackRange = CalculateWeaponRange();
            if (_hitVolume != null)
            {
                _hitVolume.enabled = false;
            }

            CreateTelegraphVisuals();
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
            if (_hitVolume != null)
            {
                _hitVolume.enabled = active;
            }

            if (active && !_damageAppliedThisAttack)
            {
                // 只有真正结算出伤害后才锁定本次攻击，避免命中检测过早导致整段攻击“空挥”。
                _damageAppliedThisAttack = ApplyWeaponHit();
            }
        }

        public void EndAttack()
        {
            _damageActive = false;
            if (_hitVolume != null)
            {
                _hitVolume.enabled = false;
            }

            EndTelegraph();
        }

        public void SetAttackTarget(Transform target)
        {
        }

        public void BeginTelegraph(float duration, float attackRange)
        {
            if (!_telegraphEnabled || duration <= 0.02f)
            {
                EndTelegraph();
                return;
            }

            _telegraphDuration = duration;
            _telegraphRadius = Mathf.Max(0.65f, attackRange * _telegraphRadiusMultiplier);
            _telegraphStartedAt = Time.time;
            SetTelegraphGeometry(_telegraphRadius);
            SetTelegraphColor(new Color(.16f, .005f, .005f, .3f));
            _telegraphRing.enabled = true;
            _telegraphFill.enabled = true;
        }

        public void EndTelegraph()
        {
            _telegraphStartedAt = 0f;
            if (_telegraphRing != null) _telegraphRing.enabled = false;
            if (_telegraphFill != null) _telegraphFill.enabled = false;
        }

        public void ConfigureFaction(CombatFaction faction)
        {
            _faction = faction;
        }

        /// <summary>应用 Luban 敌人原型的攻击力覆盖。</summary>
        public void ConfigureDamage(float damage)
        {
            _baseDamage = Mathf.Max(0f, damage);
            _damage = _baseDamage;
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

        private bool ApplyWeaponHit()
        {
            if (_hitVolume == null)
            {
                return false;
            }

            var bounds = _hitVolume.bounds;
            var center = bounds.center;
            var radius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
            return TryApplyWeaponHit(center, radius);
        }

        private float CalculateWeaponRange()
        {
            if (_hitVolume == null) return .9f;
            var bounds = _hitVolume.bounds;
            var horizontalCenter = bounds.center;
            horizontalCenter.y = transform.position.y;
            var horizontalExtents = Mathf.Max(bounds.extents.x, bounds.extents.z);
            return Mathf.Max(.65f, Vector3.Distance(transform.position, horizontalCenter) + horizontalExtents);
        }

        private Collider FindWeaponCollider()
        {
            var colliders = GetComponentsInChildren<Collider>(true);
            foreach (var candidate in colliders)
            {
                if (candidate.transform == transform) continue;
                if (candidate.name.Contains("Sword") || candidate.name.Contains("Weapon")) return candidate;
            }

            foreach (var candidate in colliders)
            {
                if (candidate.transform != transform) return candidate;
            }

            return GetComponent<Collider>();
        }

        /// <summary>在指定范围内查找玩家生命组件并结算一次伤害。</summary>
        private bool TryApplyWeaponHit(Vector3 center, float radius)
        {
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

                var damageable = hit.GetComponentInParent<Health>();
                if (damageable == null || !damageable.IsAlive)
                {
                    continue;
                }

                var targetFaction = FactionResolver.FindInParents(damageable.transform);
                if (!DamagePolicy.CanDamage(this, targetFaction))
                {
                    continue;
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
                var result = DamageHandler.Apply(damageable, info, targetFaction);
                if (result.AppliedDamage > 0f)
                {
                    return true;
                }
            }

            return false;
        }

        private void Update()
        {
            if (_telegraphStartedAt <= 0f || _telegraphRing == null)
            {
                return;
            }

            var progress = Mathf.Clamp01((Time.time - _telegraphStartedAt) / Mathf.Max(.02f, _telegraphDuration));
            var dark = new Color(.16f, .005f, .005f, .28f);
            var bright = new Color(_telegraphColor.r, _telegraphColor.g, _telegraphColor.b, .85f);
            SetTelegraphColor(Color.Lerp(dark, bright, progress));
            _telegraphFill.transform.localScale = Vector3.one * Mathf.Lerp(.78f, 1f, progress);
        }

        private void CreateTelegraphVisuals()
        {
            var root = new GameObject("AttackTelegraph");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = Vector3.up * .035f;

            var ringObject = new GameObject("AttackRangeRing");
            ringObject.transform.SetParent(root.transform, false);
            _telegraphRing = ringObject.AddComponent<LineRenderer>();
            _telegraphRing.useWorldSpace = false;
            _telegraphRing.positionCount = 48;
            _telegraphRing.widthMultiplier = .075f;
            _telegraphRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _telegraphRing.receiveShadows = false;
            _telegraphMaterial = new Material(Shader.Find("Sprites/Default"));
            _telegraphRing.material = _telegraphMaterial;

            var fillObject = new GameObject("AttackRangeFill");
            fillObject.transform.SetParent(root.transform, false);
            _telegraphFill = fillObject.AddComponent<MeshRenderer>();
            _telegraphFill.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _telegraphFill.receiveShadows = false;
            _telegraphFill.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            var filter = fillObject.AddComponent<MeshFilter>();
            filter.sharedMesh = BuildDiscMesh(48);
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
            var mesh = new Mesh { name = "AttackTelegraphDisc" };
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
