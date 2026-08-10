using System.Collections;
using NUnit.Framework;
using Train.Architecture.Bootstrap;
using Train.Gameplay.Combat;
using Train.Gameplay.Combat.Application.Events;
using Train.Gameplay.Combat.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace Train.Tests.PlayMode.Combat
{
    public sealed class HealthEventRelayPlayModeTests
    {
        [UnityTest]
        public IEnumerator LethalDamage_PublishesEntityDiedEvent()
        {
            var target = new GameObject("HealthRelayTestTarget");
            var health = target.AddComponent<Health>();
            health.SetMaxHealth(10f);
            target.AddComponent<HealthEventRelay>();

            var eventCount = 0;
            var bus = SceneBootstrap.ResolveEvents(health);
            using var subscription =
                bus.Subscribe<EntityDiedEvent>(_ => eventCount++);

            health.TakeDamage(new DamageInfo(
                10f,
                null,
                Vector3.zero,
                Vector3.forward));
            yield return null;

            Assert.That(eventCount, Is.EqualTo(1));
            Object.Destroy(target);
        }
    }
}
