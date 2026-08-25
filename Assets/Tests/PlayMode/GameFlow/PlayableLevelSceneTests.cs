using System.Collections.Generic;
using System.Collections;
using NUnit.Framework;
using Train.Architecture.Assets;
using Train.Architecture.Bootstrap;
using Train.Gameplay.Combat;
using Train.Gameplay.Enemy.Core;
using Train.GameFlow.Core;
using Train.GameFlow.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Train.Tests.PlayMode.GameFlow
{
    public sealed class PlayableLevelSceneTests
    {
        public static string LastNoInputMeasurement { get; private set; }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator CombatArena_LoadsSpawnsAndEntersCombat()
        {
            var assets = GameBootstrap.Instance.Context.Assets;
            Assert.That(assets, Is.Not.Null);

            var loadTask = assets.LoadSceneAsync(
                AssetLocations.CombatArenaScene,
                LoadSceneMode.Additive);
            while (!loadTask.IsCompleted)
            {
                yield return null;
            }

            if (loadTask.IsFaulted)
            {
                throw loadTask.Exception;
            }

            var lease = loadTask.Result;
            Assert.That(lease.IsValid, Is.True);
            Assert.That(lease.Scene.isLoaded, Is.True);

            LevelRuntimeController runtime = null;
            foreach (var root in lease.Scene.GetRootGameObjects())
            {
                runtime = root.GetComponentInChildren<LevelRuntimeController>(
                    true);
                if (runtime != null)
                {
                    break;
                }
            }

            Assert.That(runtime, Is.Not.Null);
            runtime.StartLevel();
            var timeoutAt = Time.realtimeSinceStartup + 10f;
            while (runtime.CurrentPhase != LevelPhase.Combat &&
                   Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }

            Assert.That(runtime.CurrentPhase, Is.EqualTo(LevelPhase.Combat));
            Assert.That(runtime.EnemyCount, Is.EqualTo(3));

            var unloadTask = lease.UnloadAsync();
            while (!unloadTask.IsCompleted)
            {
                yield return null;
            }

            if (unloadTask.IsFaulted)
            {
                throw unloadTask.Exception;
            }

            Assert.That(lease.IsValid, Is.False);
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator CombatArena_NoInput_DoesNotDriftWhileEnemiesAttack()
        {
            var assets = GameBootstrap.Instance.Context.Assets;
            Assert.That(assets, Is.Not.Null);

            var loadTask = assets.LoadSceneAsync(
                AssetLocations.CombatArenaScene,
                LoadSceneMode.Additive);
            while (!loadTask.IsCompleted)
            {
                yield return null;
            }

            if (loadTask.IsFaulted)
            {
                throw loadTask.Exception;
            }

            var lease = loadTask.Result;
            LevelRuntimeController runtime = null;
            foreach (var root in lease.Scene.GetRootGameObjects())
            {
                runtime = root.GetComponentInChildren<LevelRuntimeController>(true);
                if (runtime != null)
                {
                    break;
                }
            }

            Assert.That(runtime, Is.Not.Null);
            runtime.StartLevel();
            var combatTimeout = Time.realtimeSinceStartup + 10f;
            while (runtime.CurrentPhase != LevelPhase.Combat &&
                   Time.realtimeSinceStartup < combatTimeout)
            {
                yield return null;
            }

            Assert.That(runtime.CurrentPhase, Is.EqualTo(LevelPhase.Combat));

            var player = GameObject.FindGameObjectWithTag("Player");
            var playerHealth = player != null ? player.GetComponent<Health>() : null;
            var playerBody = player != null ? player.GetComponent<Collider>() : null;
            Assert.That(playerHealth, Is.Not.Null);
            Assert.That(playerBody, Is.Not.Null);

            var enemies = new List<EnemyController>();
            foreach (var enemy in Object.FindObjectsByType<EnemyController>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (enemy.gameObject.scene == lease.Scene)
                {
                    enemies.Add(enemy);
                }
            }

            Assert.That(enemies, Has.Count.EqualTo(3));
            foreach (var enemy in enemies)
            {
                var enemyBody = enemy.GetComponent<Collider>();
                Assert.That(enemyBody, Is.Not.Null);
                Assert.That(Physics.GetIgnoreCollision(playerBody, enemyBody), Is.True);
            }

            var startPosition = player.transform.position;
            var maximumHorizontalDrift = 0f;
            var minimumPlayerY = player.transform.position.y;
            var attackCount = 0;
            var damageCount = 0;
            var previousStates = new Dictionary<EnemyController, string>();
            System.Action<DamageInfo, DamageResult> onDamaged =
                (_, result) =>
                {
                    if (result.AppliedDamage > 0f)
                    {
                        damageCount++;
                    }
                };
            playerHealth.Damaged += onDamaged;

            var endAt = Time.realtimeSinceStartup + 12f;
            while (Time.realtimeSinceStartup < endAt)
            {
                var offset = player.transform.position - startPosition;
                offset.y = 0f;
                maximumHorizontalDrift = Mathf.Max(
                    maximumHorizontalDrift,
                    new Vector2(offset.x, offset.z).magnitude);
                minimumPlayerY = Mathf.Min(minimumPlayerY, player.transform.position.y);

                foreach (var enemy in enemies)
                {
                    var state = enemy != null ? enemy.CurrentStateName : string.Empty;
                    if (state == "EnemyAttackState" &&
                        (!previousStates.TryGetValue(enemy, out var previous) ||
                         previous != state))
                    {
                        attackCount++;
                    }

                    previousStates[enemy] = state;
                }

                yield return null;
            }

            playerHealth.Damaged -= onDamaged;
            LastNoInputMeasurement =
                $"maxPlayerXZDrift={maximumHorizontalDrift:F3}, " +
                $"minimumPlayerY={minimumPlayerY:F3}, " +
                $"enemyAttackTransitions={attackCount}, " +
                $"playerDamageEvents={damageCount}, " +
                $"playerDeathCount={runtime.Snapshot.PlayerDeathCount}, " +
                $"edgeDropObserved={minimumPlayerY < 0f}";
            Debug.Log(
                $"[Level_Combat_001] 12s no-input measurement: " +
                LastNoInputMeasurement);

            Assert.That(maximumHorizontalDrift, Is.LessThan(.25f));
            Assert.That(attackCount, Is.GreaterThan(0));
            Assert.That(damageCount, Is.GreaterThan(0));

            var unloadTask = lease.UnloadAsync();
            while (!unloadTask.IsCompleted)
            {
                yield return null;
            }

            if (unloadTask.IsFaulted)
            {
                throw unloadTask.Exception;
            }

            Assert.That(lease.IsValid, Is.False);
        }
    }
}
