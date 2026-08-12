using Train.Gameplay.Combat;
using Train.Gameplay.Combat.Factions;
using UnityEngine;

namespace Train.Gameplay.Enemy.Combat
{
    /// <summary>敌人发射的实体弹道，沿途检测并命中玩家。</summary>
    [DisallowMultipleComponent]
    public sealed class EnemyProjectile : MonoBehaviour, IDamageSource, IFactionMember
    {
        [SerializeField, Min(.1f)] private float _speed = 8f;
        [SerializeField, Min(.01f)] private float _radius = .12f;
        [SerializeField, Min(.1f)] private float _lifeSeconds = 4f;
        [SerializeField, Min(0f)] private float _damage = 24f;
        [SerializeField, Min(0f)] private float _impactForce = .5f;
        [SerializeField] private DamageType _damageType = DamageType.Electric;
        [SerializeField] private LayerMask _damageLayers = ~0;
        [SerializeField] private CombatFaction _faction = CombatFaction.Enemy;

        private Transform _ownerTransform;
        private Vector3 _direction;
        private float _expiresAt;

        public Transform SourceTransform => transform;
        public Transform OwnerTransform => _ownerTransform;
        public float Damage => _damage;
        public DamageType DamageType => _damageType;
        public bool IsDamageActive => isActiveAndEnabled;
        public CombatFaction Faction => _faction;

        public void Initialize(
            Transform owner,
            Vector3 direction,
            float damage,
            float impactForce,
            DamageType damageType,
            CombatFaction faction,
            LayerMask damageLayers,
            float speed,
            float lifeSeconds,
            float radius)
        {
            _ownerTransform = owner;
            _direction = direction.sqrMagnitude > .001f ? direction.normalized : transform.forward;
            _damage = Mathf.Max(0f, damage);
            _impactForce = Mathf.Max(0f, impactForce);
            _damageType = damageType;
            _faction = faction;
            _damageLayers = damageLayers;
            _speed = Mathf.Max(.1f, speed);
            _lifeSeconds = Mathf.Max(.1f, lifeSeconds);
            _radius = Mathf.Max(.01f, radius);
            _expiresAt = Time.time + _lifeSeconds;
            transform.forward = _direction;
        }

        private void Awake()
        {
            _expiresAt = Time.time + _lifeSeconds;
        }

        private void Update()
        {
            if (Time.time >= _expiresAt)
            {
                Destroy(gameObject);
                return;
            }

            var distance = _speed * Time.deltaTime;
            if (TryHit(distance)) return;
            transform.position += _direction * distance;
        }

        private bool TryHit(float distance)
        {
            if (!Physics.SphereCast(transform.position, _radius, _direction, out var hit, distance,
                    _damageLayers, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            if (hit.collider == null ||
                (_ownerTransform != null &&
                 (hit.collider.transform == _ownerTransform || hit.collider.transform.IsChildOf(_ownerTransform))))
            {
                return false;
            }

            var health = hit.collider.GetComponentInParent<Health>();
            if (health == null || !health.IsAlive) return false;

            var targetFaction = FactionResolver.FindInParents(health.transform);
            if (!DamagePolicy.CanDamage(this, targetFaction)) return false;

            DamageHandler.Apply(
                health,
                new DamageInfo(_damage, this, hit.point, _direction, _impactForce, _damageType),
                targetFaction);
            Destroy(gameObject);
            return true;
        }
    }
}
