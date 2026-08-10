using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Train.Architecture.Assets;
using Train.Architecture.Bootstrap;
using Train.Architecture.Services;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Train.Tests.EditMode.Architecture
{
    public sealed class GameContextTests
    {
        [Test]
        public void InstallAssetService_ExposesServiceAndDisposesItWithContext()
        {
            var service = new FakeAssetService();
            var context = new GameContext();

            context.InstallAssetService(service);

            Assert.That(context.Assets, Is.SameAs(service));
            context.Dispose();
            Assert.That(service.WasDisposed, Is.True);
        }

        [Test]
        public void InstallAssetService_WhenDifferentServiceAlreadyExists_Throws()
        {
            using var context = new GameContext();
            context.InstallAssetService(new FakeAssetService());

            Assert.Throws<System.InvalidOperationException>(
                () => context.InstallAssetService(new FakeAssetService()));
        }

        [Test]
        public void Services_ResolveInstalledInterface_AndDisposeOwnedService()
        {
            var service = new FakeApplicationService();
            var context = new GameContext();

            context.Services.Install<IFakeApplicationService>(service);

            Assert.That(
                context.Services.Resolve<IFakeApplicationService>(),
                Is.SameAs(service));
            context.Dispose();
            Assert.That(service.WasDisposed, Is.True);
        }

        [Test]
        public void Services_WhenMissing_ThrowsActionableException()
        {
            using var context = new GameContext();

            Assert.Throws<System.InvalidOperationException>(
                () => context.Services.Resolve<IFakeApplicationService>());
        }

        private interface IFakeApplicationService
        {
        }

        private sealed class FakeApplicationService :
            IFakeApplicationService,
            System.IDisposable
        {
            public bool WasDisposed { get; private set; }

            public void Dispose()
            {
                WasDisposed = true;
            }
        }

        private sealed class FakeAssetService : IAssetService
        {
            public bool WasDisposed { get; private set; }
            public bool IsInitialized => true;
            public string PackageName => "Fake";

            public Task InitializeAsync(
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task<IAssetLease<TAsset>> LoadAsync<TAsset>(
                string location,
                CancellationToken cancellationToken = default)
                where TAsset : Object
            {
                throw new System.NotSupportedException();
            }

            public Task<IInstanceLease> InstantiateAsync(
                string location,
                Transform parent = null,
                Vector3? position = null,
                Quaternion? rotation = null,
                CancellationToken cancellationToken = default)
            {
                throw new System.NotSupportedException();
            }

            public Task<ISceneLease> LoadSceneAsync(
                string location,
                LoadSceneMode mode = LoadSceneMode.Single,
                bool activateOnLoad = true,
                CancellationToken cancellationToken = default)
            {
                throw new System.NotSupportedException();
            }

            public void Dispose()
            {
                WasDisposed = true;
            }
        }
    }
}
