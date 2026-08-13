using Train.Buffs.Core;
using Train.Gameplay.Combat.Buffs;
using Train.Gameplay.Combat.Factions;
using Train.Gameplay.Enemy.Core;
using UnityEngine;

namespace Train.Gameplay.Combat.ActiveItems
{
    /// <summary>向前投掷并在落点爆炸的主动手雷。</summary>
    public sealed class ActiveGrenadeProjectile : MonoBehaviour
    {
        private Vector3 _direction;
        private float _radius;
        private float _damage;
        private DamageType _damageType;
        private string _buffId;
        private float _duration;
        private float _magnitude;
        private int _stackAmount;
        private int _maxStacks;
        private float _elapsed;
        private bool _detonated;

        public void Initialize(
            Vector3 direction,
            float radius,
            float damage,
            DamageType damageType,
            string buffId,
            float duration,
            float magnitude,
            int stackAmount,
            int maxStacks)
        {
            _direction = direction.normalized;
            _radius = radius;
            _damage = damage;
            _damageType = damageType;
            _buffId = buffId;
            _duration = duration;
            _magnitude = magnitude;
            _stackAmount = stackAmount;
            _maxStacks = maxStacks;
        }

        private void Awake()
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "GrenadeVisual";
            body.transform.SetParent(transform, false);
            body.transform.localScale = Vector3.one * .18f;
            Destroy(body.GetComponent<Collider>());
            body.GetComponent<MeshRenderer>().sharedMaterial =
                new Material(Shader.Find("Sprites/Default")) { color = new Color(.95f, .25f, .1f) };
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            transform.position += _direction * 7f * Time.deltaTime;
            transform.position += Vector3.down * 4f * Time.deltaTime * _elapsed;
            if (_elapsed >= .65f)
            {
                Detonate();
            }
        }

        private void Detonate()
        {
            if (_detonated) return;
            _detonated = true;
            var origin = transform.position;
            foreach (var target in FindObjectsByType<Health>(FindObjectsSortMode.None))
            {
                if (target == null || !target.IsAlive ||
                    Vector3.Distance(origin, target.transform.position) > _radius)
                {
                    continue;
                }

                var faction = FactionResolver.FindInParents(target.transform);
                if (faction != null && faction.Faction != CombatFaction.Enemy)
                {
                    continue;
                }

                var result = DamageHandler.Apply(target, new DamageInfo(
                    _damage, null, target.transform.position, target.transform.position - origin,
                    0f, _damageType), faction);
                if (result.AppliedDamage > 0f && !string.IsNullOrWhiteSpace(_buffId))
                {
                    target.GetComponentInParent<BuffHandleComponent>()?.Apply(new BuffInfo(
                        _buffId, "item.grenade", _duration, _magnitude, _stackAmount, _maxStacks));
                }
            }

            ActiveCryoPulseVisual.Spawn(origin, _radius);
            Destroy(gameObject);
        }
    }
}
