using NUnit.Framework;
using Train.Gameplay.Combat;
using Train.Gameplay.Enemy.Data;
using Train.Gameplay.Enemy.States;
using UnityEngine;

namespace Train.Tests.EditMode.Enemy
{
    public sealed class EnemyHitStateTests
    {
        [Test]
        public void GetHitReactionDuration_HeavyHitKeepsLongerLayerThanLightHit()
        {
            var config = ScriptableObject.CreateInstance<EnemyConfig>();
            try
            {
                var light = config.GetHitReactionDuration(10f, DamageType.Physical);
                var heavy = config.GetHitReactionDuration(40f, DamageType.Physical);

                Assert.That(light, Is.InRange(.18f, .24f));
                Assert.That(heavy, Is.InRange(.32f, .42f));
                Assert.That(heavy, Is.GreaterThan(light));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void RefreshDuration_PreservesElapsedAndLimitsEachExtension()
        {
            const float elapsed = .1f;
            var refreshed = EnemyHitState.CalculateRefreshedDuration(
                elapsed,
                .24f,
                .42f);
            var repeated = EnemyHitState.CalculateRefreshedDuration(
                elapsed,
                refreshed,
                .42f);

            Assert.That(refreshed, Is.EqualTo(.36f).Within(.0001f));
            Assert.That(repeated - refreshed, Is.LessThanOrEqualTo(.12f + .0001f));
            Assert.That(repeated - elapsed, Is.LessThanOrEqualTo(.42f));
        }
    }
}
