using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Train.Architecture.Assets;
using Train.Architecture.Bootstrap;
using Train.Gameplay.Enemy.Data;
using UnityEngine;
using UnityEngine.TestTools;

namespace Train.Tests.PlayMode.Assets
{
    public sealed class YooAssetServicePlayModeTests
    {
        [UnityTest]
        public IEnumerator DefaultPackage_LoadsEnemyConfig_AndReleasesLease()
        {
            var service = GameBootstrap.Instance.Context.Assets;
            Assert.That(service, Is.Not.Null);

            var initializeTask = service.InitializeAsync();
            while (!initializeTask.IsCompleted)
            {
                yield return null;
            }

            if (initializeTask.IsFaulted)
            {
                throw initializeTask.Exception;
            }

            var loadTask = service.LoadAsync<EnemyConfig>(
                AssetLocations.KnightEnemyConfig);
            while (!loadTask.IsCompleted)
            {
                yield return null;
            }

            if (loadTask.IsFaulted)
            {
                throw loadTask.Exception;
            }

            IAssetLease<EnemyConfig> lease = loadTask.Result;
            Assert.That(lease.Asset, Is.Not.Null);
            Assert.That(lease.IsValid, Is.True);

            lease.Dispose();
            Assert.That(lease.IsValid, Is.False);
        }

        [UnityTest]
        public IEnumerator InitializeAsync_WhenCalledTwice_RemainsReady()
        {
            var service = GameBootstrap.Instance.Context.Assets;
            Assert.That(service, Is.Not.Null);

            var first = service.InitializeAsync();
            var second = service.InitializeAsync();
            while (!first.IsCompleted || !second.IsCompleted)
            {
                yield return null;
            }

            if (first.IsFaulted)
            {
                throw first.Exception;
            }

            if (second.IsFaulted)
            {
                throw second.Exception;
            }

            Assert.That(service.IsInitialized, Is.True);
            Assert.That(service.PackageName, Is.EqualTo("DefaultPackage"));
        }

        [UnityTest]
        public IEnumerator LoadAsync_WhenLocationDoesNotExist_FailsClearly()
        {
            var service = GameBootstrap.Instance.Context.Assets;
            Assert.That(service, Is.Not.Null);

            LogAssert.Expect(
                LogType.Error,
                new Regex(
                    "Failed to load asset.*DefinitelyMissing\\.asset"));
            var loadTask = service.LoadAsync<EnemyConfig>(
                "Assets/Data/Enemies/DefinitelyMissing.asset");
            while (!loadTask.IsCompleted)
            {
                yield return null;
            }

            Assert.That(loadTask.IsFaulted, Is.True);
            Assert.That(
                loadTask.Exception?.GetBaseException(),
                Is.TypeOf<AssetServiceException>());
        }
    }
}
