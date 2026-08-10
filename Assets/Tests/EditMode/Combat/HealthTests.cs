using NUnit.Framework;
using Train.Gameplay.Combat;
using UnityEngine;

namespace Train.Tests.EditMode.Combat
{
    public sealed class HealthTests
    {
        private GameObject _target;
        private Health _health;

        [SetUp]
        public void SetUp()
        {
            _target = new GameObject("HealthTestTarget");
            _health = _target.AddComponent<Health>();
            _health.SetMaxHealth(100f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_target);
        }

        [Test]
        public void TakeDamage_WhenDamageIsLethal_KillsTargetOnce()
        {
            var deathCount = 0;
            _health.Died += _ => deathCount++;
            var damage = CreateDamage(150f);

            var result = _health.TakeDamage(damage);

            Assert.That(result.Killed, Is.True);
            Assert.That(result.AppliedDamage, Is.EqualTo(100f));
            Assert.That(_health.CurrentHealth, Is.Zero);
            Assert.That(deathCount, Is.EqualTo(1));
        }

        [TestCase(10f, 90f)]
        [TestCase(25f, 75f)]
        [TestCase(100f, 0f)]
        public void TakeDamage_SubtractsExpectedHealth(
            float damageAmount,
            float expectedHealth)
        {
            _health.TakeDamage(CreateDamage(damageAmount));

            Assert.That(_health.CurrentHealth, Is.EqualTo(expectedHealth));
        }

        [Test]
        public void Revive_AfterDeath_RestoresFullHealth()
        {
            _health.TakeDamage(CreateDamage(100f));

            _health.Revive();

            Assert.That(_health.IsAlive, Is.True);
            Assert.That(_health.CurrentHealth, Is.EqualTo(100f));
        }

        private static DamageInfo CreateDamage(float amount)
        {
            return new DamageInfo(
                amount,
                null,
                Vector3.zero,
                Vector3.forward);
        }
    }
}
