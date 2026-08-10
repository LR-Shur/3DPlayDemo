using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Train.Architecture.Assets;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;

namespace Train.Infrastructure.Assets
{
    /// <summary>
    /// 基于 YooAsset 的统一资源服务实现，负责初始化、加载与释放租约。
    /// </summary>
    public sealed class YooAssetService : IAssetService
    {
        private readonly YooAssetServiceOptions _options;
        private readonly List<IDisposable> _ownedLeases = new();
        private ResourcePackage _package;
        private Task _initializationTask;
        private bool _ready;
        private bool _disposed;

        public YooAssetService(YooAssetServiceOptions options = null)
        {
            _options = options ?? new YooAssetServiceOptions();
        }

        public bool IsInitialized =>
            !_disposed &&
            _ready &&
            _package != null &&
            _package.InitializeStatus == EOperationStatus.Succeeded;

        public string PackageName => _options.PackageName;

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (_initializationTask == null ||
                _initializationTask.IsFaulted ||
                _initializationTask.IsCanceled)
            {
                _initializationTask = InitializeCoreAsync(cancellationToken);
            }

            return _initializationTask;
        }

        public async Task<IAssetLease<TAsset>> LoadAsync<TAsset>(
            string location,
            CancellationToken cancellationToken = default)
            where TAsset : UnityEngine.Object
        {
            ValidateLocation(location);
            await EnsureReadyAsync(cancellationToken);

            var handle = _package.LoadAssetAsync<TAsset>(location);
            try
            {
                await handle;
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch
            {
                handle.Release();
                throw;
            }

            if (handle.Status != EOperationStatus.Succeeded)
            {
                var error = handle.Error;
                handle.Release();
                throw new AssetServiceException(
                    $"Failed to load asset '{location}' from package '{PackageName}': {error}");
            }

            var lease = new YooAssetLease<TAsset>(
                location,
                handle,
                OnLeaseDisposed);
            _ownedLeases.Add(lease);
            return lease;
        }

        public async Task<IInstanceLease> InstantiateAsync(
            string location,
            Transform parent = null,
            Vector3? position = null,
            Quaternion? rotation = null,
            CancellationToken cancellationToken = default)
        {
            ValidateLocation(location);
            await EnsureReadyAsync(cancellationToken);

            var handle = _package.LoadAssetAsync<GameObject>(location);
            var options = position.HasValue
                ? new InstantiateOptions(
                    true,
                    parent,
                    position.Value,
                    rotation ?? Quaternion.identity)
                : new InstantiateOptions(true, parent, false);
            var operation = handle.InstantiateAsync(options);
            try
            {
                await operation;
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch
            {
                if (operation.Result != null)
                {
                    UnityEngine.Object.Destroy(operation.Result);
                }

                handle.Release();
                throw;
            }

            if (operation.Status != EOperationStatus.Succeeded ||
                operation.Result == null)
            {
                var error = operation.Error;
                handle.Release();
                throw new AssetServiceException(
                    $"Failed to instantiate '{location}' from package '{PackageName}': {error}");
            }

            var lease = new YooInstanceLease(
                location,
                operation.Result,
                handle,
                OnLeaseDisposed);
            _ownedLeases.Add(lease);
            return lease;
        }

        public async Task<ISceneLease> LoadSceneAsync(
            string location,
            LoadSceneMode mode = LoadSceneMode.Single,
            bool activateOnLoad = true,
            CancellationToken cancellationToken = default)
        {
            ValidateLocation(location);
            await EnsureReadyAsync(cancellationToken);

            var handle = _package.LoadSceneAsync(
                location,
                mode,
                LocalPhysicsMode.None,
                activateOnLoad);
            try
            {
                await handle;
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch
            {
                if (handle.Status == EOperationStatus.Succeeded)
                {
                    await handle.UnloadSceneAsync();
                }
                else
                {
                    handle.Release();
                }

                throw;
            }

            if (handle.Status != EOperationStatus.Succeeded)
            {
                var error = handle.Error;
                handle.Release();
                throw new AssetServiceException(
                    $"Failed to load scene '{location}' from package '{PackageName}': {error}");
            }

            var lease = new YooSceneLease(
                location,
                handle,
                OnLeaseDisposed);
            _ownedLeases.Add(lease);
            return lease;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            for (var index = _ownedLeases.Count - 1; index >= 0; index--)
            {
                _ownedLeases[index]?.Dispose();
            }

            _ownedLeases.Clear();
            _disposed = true;
        }

        private async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            if (!YooAssets.IsInitialized)
            {
                YooAssets.Initialize();
            }

            if (!YooAssets.TryGetPackage(PackageName, out _package))
            {
                _package = YooAssets.CreatePackage(PackageName);
            }

            if (_package.InitializeStatus == EOperationStatus.Succeeded)
            {
                await LoadManifestAsync(cancellationToken);
                _ready = true;
                return;
            }

            Debug.Log(
                $"[Assets] Initializing YooAsset package '{PackageName}' " +
                $"in {_options.PlayMode} mode.");
            var operation = _package.InitializePackageAsync(CreateInitializeOptions());
            await operation;
            cancellationToken.ThrowIfCancellationRequested();

            if (operation.Status != EOperationStatus.Succeeded)
            {
                throw new AssetServiceException(
                    $"Failed to initialize YooAsset package '{PackageName}': {operation.Error}");
            }

            Debug.Log(
                $"[Assets] Package '{PackageName}' file system is ready.");
            await LoadManifestAsync(cancellationToken);
            _ready = true;
            Debug.Log(
                $"[Assets] Package '{PackageName}' manifest is ready.");
        }

        private async Task LoadManifestAsync(CancellationToken cancellationToken)
        {
            Debug.Log(
                $"[Assets] Requesting package version for '{PackageName}'.");
            var versionOperation = _package.RequestPackageVersionAsync();
            await versionOperation;
            cancellationToken.ThrowIfCancellationRequested();
            if (versionOperation.Status != EOperationStatus.Succeeded)
            {
                throw new AssetServiceException(
                    $"Failed to request package version for '{PackageName}': " +
                    versionOperation.Error);
            }

            var manifestOptions = new LoadPackageManifestOptions(
                versionOperation.PackageVersion,
                60);
            var manifestOperation = _package.LoadPackageManifestAsync(manifestOptions);
            Debug.Log(
                $"[Assets] Loading manifest '{versionOperation.PackageVersion}' " +
                $"for '{PackageName}'.");
            await manifestOperation;
            cancellationToken.ThrowIfCancellationRequested();
            if (manifestOperation.Status != EOperationStatus.Succeeded)
            {
                throw new AssetServiceException(
                    $"Failed to load package manifest for '{PackageName}': " +
                    manifestOperation.Error);
            }
        }

        private InitializePackageOptions CreateInitializeOptions()
        {
            switch (_options.PlayMode)
            {
                case AssetPlayMode.EditorSimulate:
#if UNITY_EDITOR
                    var buildResult = EditorSimulateBuildInvoker.Build(
                        PackageName,
                        (int)EBundleType.VirtualAssetBundle);
                    var editorOptions = new EditorSimulateModeOptions
                    {
                        EditorFileSystemParameters =
                            FileSystemParameters.CreateDefaultEditorFileSystemParameters(
                                buildResult.PackageRootDirectory)
                    };
                    return editorOptions;
#else
                    throw new AssetServiceException(
                        "EditorSimulate mode is only available inside the Unity Editor.");
#endif

                case AssetPlayMode.Offline:
                    return new OfflinePlayModeOptions
                    {
                        BuiltinFileSystemParameters =
                            FileSystemParameters.CreateDefaultBuiltinFileSystemParameters()
                    };

                case AssetPlayMode.Host:
                    var remoteService = new RemoteService(
                        _options.PrimaryHostUrl,
                        _options.FallbackHostUrl);
                    return new HostPlayModeOptions
                    {
                        BuiltinFileSystemParameters =
                            FileSystemParameters.CreateDefaultBuiltinFileSystemParameters(),
                        CacheFileSystemParameters =
                            FileSystemParameters.CreateDefaultSandboxFileSystemParameters(
                                remoteService)
                    };

                case AssetPlayMode.Web:
                    return new WebPlayModeOptions
                    {
                        WebServerFileSystemParameters =
                            FileSystemParameters.CreateDefaultWebServerFileSystemParameters()
                    };

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async Task EnsureReadyAsync(CancellationToken cancellationToken)
        {
            await InitializeAsync(cancellationToken);
            if (!IsInitialized)
            {
                throw new AssetServiceException(
                    $"Asset service package '{PackageName}' is not initialized.");
            }
        }

        private void OnLeaseDisposed(IDisposable lease)
        {
            _ownedLeases.Remove(lease);
        }

        private static void ValidateLocation(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                throw new ArgumentException(
                    "Asset location cannot be empty.",
                    nameof(location));
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(YooAssetService));
            }
        }

        /// <summary>
        /// 提供远端下载地址解析的私有实现。
        /// </summary>
        private sealed class RemoteService : IRemoteService
        {
            private readonly string _primary;
            private readonly string _fallback;

            public RemoteService(string primary, string fallback)
            {
                _primary = primary?.TrimEnd('/');
                _fallback = fallback?.TrimEnd('/');
            }

            public IReadOnlyList<string> GetRemoteUrls(string fileName)
            {
                return new[]
                {
                    $"{_primary}/{fileName}",
                    $"{_fallback}/{fileName}"
                };
            }
        }
    }
}
