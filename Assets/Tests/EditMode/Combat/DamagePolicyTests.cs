using NUnit.Framework;
using Train.Gameplay.Combat;
using Train.Gameplay.Combat.Factions;
using UnityEngine;

namespace Train.Tests.EditMode.Combat
{
    public sealed class DamagePolicyTests
    {
        private GameObject _targetObject;
        private Health _health;
        private FactionMember _targetFaction;

        [SetUp]
        public void SetUp()
        {
            _targetObject = new GameObject("DamagePolicyTarget");
            _health = _targetObject.AddComponent<Health>();
            _health.SetMaxHealth(100f);
            _targetFaction = _targetObject.AddComponent<FactionMember>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_targetObject);
        }

        [Test]
        public void CanDamage_WhenFactionsMatch_ReturnsFalse()
        {
            var source = new DamageSourceStub(CombatFaction.Enemy);
            var target = new FactionStub(CombatFaction.Enemy);

            Assert.That(DamagePolicy.CanDamage(source, target), Is.False);
        }

        [Test]
        public void CanDamage_WhenFactionsDiffer_ReturnsTrue()
        {
            var source = new DamageSourceStub(CombatFaction.Player);
            var target = new FactionStub(CombatFaction.Enemy);

            Assert.That(DamagePolicy.CanDamage(source, target), Is.True);
        }

        [TestCase(CombatFaction.Neutral, CombatFaction.Player)]
        [TestCase(CombatFaction.Enemy, CombatFaction.Neutral)]
        [TestCase(CombatFaction.Neutral, CombatFaction.Neutral)]
        public void CanDamage_WhenEitherFactionIsNeutral_ReturnsTrue(
            CombatFaction source,
            CombatFaction target)
        {
            Assert.That(DamagePolicy.CanDamage(source, target), Is.True);
        }

        [Test]
        public void CanDamage_WhenFactionIsMissing_RejectsDamage()
        {
            Assert.That(
                DamagePolicy.CanDamage(null, new FactionStub(CombatFaction.Enemy)),
                Is.False);
        }

        [Test]
        public void Apply_WhenFactionsMatch_DoesNotChangeHealth()
        {
            _targetFaction.Configure(CombatFaction.Enemy);
            var source = new DamageSourceStub(CombatFaction.Enemy);

            var result = DamageHandler.Apply(
                _health,
                CreateDamage(25f, source),
                _targetFaction);

            Assert.That(result.AppliedDamage, Is.Zero);
            Assert.That(_health.CurrentHealth, Is.EqualTo(100f));
        }

        [Test]
        public void Apply_WhenFactionsDiffer_ChangesHealth()
        {
            _targetFaction.Configure(CombatFaction.Enemy);
            var source = new DamageSourceStub(CombatFaction.Player);

            var result = DamageHandler.Apply(
                _health,
                CreateDamage(25f, source),
                _targetFaction);

            Assert.That(result.AppliedDamage, Is.EqualTo(25f));
            Assert.That(_health.CurrentHealth, Is.EqualTo(75f));
        }

        private static DamageInfo CreateDamage(float amount, IDamageSource source)
        {
            return new DamageInfo(
                amount,
                source,
                Vector3.zero,
                Vector3.forward);
        }

        private sealed class DamageSourceStub : IDamageSource, IFactionMember
        {
            public DamageSourceStub(CombatFaction faction)
            {
                Faction = faction;
            }

            public Transform SourceTransform => null;
            public Transform OwnerTransform => null;
            public float Damage => 25f;
            public DamageType DamageType => DamageType.Physical;
            public bool IsDamageActive => true;
            public CombatFaction Faction { get; }
        }

        private sealed class FactionStub : IFactionMember
        {
            public FactionStub(CombatFaction faction)
            {
                Faction = faction;
            }

            public CombatFaction Faction { get; }
        }
    }
}
