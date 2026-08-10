using System.Collections;
using NUnit.Framework;
using Train.Architecture.Assets;
using Train.Architecture.Bootstrap;
using Train.GameFlow.Core;
using Train.GameFlow.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Train.Tests.PlayMode.GameFlow
{
    public sealed class PlayableLevelSceneTests
    {
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
    }
}
