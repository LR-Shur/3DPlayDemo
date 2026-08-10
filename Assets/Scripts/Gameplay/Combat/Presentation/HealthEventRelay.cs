using Train.Architecture.Bootstrap;
using Train.Architecture.Events;
using Train.Gameplay.Combat.Application.Events;
using UnityEngine;

namespace Train.Gameplay.Combat.Presentation
{
    /// <summary>
    /// Translates local Health callbacks into scene/application events.
    /// Health itself stays independent from the global event system.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class HealthEventRelay : MonoBehaviour
    {
        private Health _health;
        private IEventBus _events;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _events = SceneBootstrap.ResolveEvents(this);
        }

        private void OnEnable()
        {
            _health ??= GetComponent<Health>();
            _events ??= SceneBootstrap.ResolveEvents(this);
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
            _health.Revived += OnRevived;
        }

        private void OnDisable()
        {
            if (_health == null)
            {
                return;
            }

            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
            _health.Revived -= OnRevived;
        }

        private void OnDamaged(DamageInfo damage, DamageResult result)
        {
            _events.Publish(new EntityDamagedEvent(_health, damage, result));
        }

        private void OnDied(DamageInfo killingBlow)
        {
            _events.Publish(new EntityDiedEvent(_health, killingBlow));
        }

        private void OnRevived()
        {
            _events.Publish(new EntityRevivedEvent(_health));
        }
    }
}
