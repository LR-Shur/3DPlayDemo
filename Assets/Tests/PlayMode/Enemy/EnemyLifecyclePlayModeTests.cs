using System.Collections;
using NUnit.Framework;
using Train.Gameplay.Enemy.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Train.Tests.PlayMode.Enemy
{
    public sealed class EnemyLifecyclePlayModeTests
    {
        [UnityTest]
        public IEnumerator DespawnAfter_WhenDelayIsZero_DestroysEnemy()
        {
            var enemy = new GameObject("EnemyLifecycleTestTarget");
            var lifecycle = enemy.AddComponent<EnemyLifecycle>();

            lifecycle.DespawnAfter(0f);
            yield return null;
            yield return null;

            Assert.That(enemy == null, Is.True);
        }
    }
}
