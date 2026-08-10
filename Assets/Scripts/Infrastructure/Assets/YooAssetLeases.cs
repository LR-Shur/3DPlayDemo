using System;
using System.Threading.Tasks;
using Train.Architecture.Assets;
using UnityEngine;
using YooAsset;
using UnityScene = UnityEngine.SceneManagement.Scene;
using YooSceneHandle = YooAsset.SceneHandle;

namespace Train.Infrastructure.Assets
{
    /// <summary>
    /// 持有单个 YooAsset 资源句柄的租约，释放时归还句柄。
    /// </summary>
    internal sealed class YooAssetLease<TAsset> : IAssetLease<TAsset>
        where TAsset : UnityEngine.Object
    {
        private AssetHandle _handle;
        private Action<IDisposable> _onDisposed;

        public YooAssetLease(
            string location,
            AssetHandle handle,
            Action<IDisposable> onDisposed)
        {
            Location = location;
            _handle = handle;
            _onDisposed = onDisposed;
        }

        public string Location { get; }
        public TAsset Asset => _handle?.GetAssetObject<TAsset>();
        public bool IsValid => _handle != null && _handle.IsValid;

        public void Dispose()
        {
            if (_handle == null)
            {
                return;
            }

            _handle.Release();
            _handle = null;
            var callback = _onDisposed;
            _onDisposed = null;
            callback?.Invoke(this);
        }
    }

    /// <summary>
    /// 持有已实例化对象句柄的租约，释放时销毁实例并归还句柄。
    /// </summary>
    internal sealed class YooInstanceLease : IInstanceLease
    {
        private AssetHandle _handle;
        private Action<IDisposable> _onDisposed;

        public YooInstanceLease(
            string location,
            GameObject instance,
            AssetHandle handle,
            Action<IDisposable> onDisposed)
        {
            Location = location;
            Instance = instance;
            _handle = handle;
            _onDisposed = onDisposed;
        }

        public string Location { get; }
        public GameObject Instance { get; private set; }
        public bool IsValid =>
            Instance != null &&
            _handle != null &&
            _handle.IsValid;

        public void Dispose()
        {
            if (Instance != null)
            {
                UnityEngine.Object.Destroy(Instance);
                Instance = null;
            }

            _handle?.Release();
            _handle = null;
            var callback = _onDisposed;
            _onDisposed = null;
            callback?.Invoke(this);
        }
    }

    /// <summary>
    /// 持有场景加载句柄的租约，释放时异步卸载场景。
    /// </summary>
    internal sealed class YooSceneLease : ISceneLease
    {
        private YooSceneHandle _handle;
        private Action<IDisposable> _onDisposed;
        private Task _unloadTask;

        public YooSceneLease(
            string location,
            YooSceneHandle handle,
            Action<IDisposable> onDisposed)
        {
            Location = location;
            _handle = handle;
            _onDisposed = onDisposed;
        }

        public string Location { get; }
        public UnityScene Scene => _handle?.SceneObject ?? default;
        public bool IsValid => _handle != null && _handle.IsValid;

        public Task UnloadAsync()
        {
            return _unloadTask ??= UnloadCoreAsync();
        }

        public void Dispose()
        {
            if (_handle == null)
            {
                return;
            }

            _ = UnloadSafelyAsync();
        }

        private async Task UnloadCoreAsync()
        {
            if (_handle == null)
            {
                return;
            }

            var operation = _handle.UnloadSceneAsync();
            await operation;
            _handle = null;

            var callback = _onDisposed;
            _onDisposed = null;
            callback?.Invoke(this);

            if (operation.Status != EOperationStatus.Succeeded)
            {
                throw new AssetServiceException(
                    $"Failed to unload scene '{Location}': {operation.Error}");
            }
        }

        private async Task UnloadSafelyAsync()
        {
            try
            {
                await UnloadAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
