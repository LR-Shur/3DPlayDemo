using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Train.Architecture.Assets
{
    /// <summary>
    /// 统一资源加载服务，屏蔽 YooAsset 等底层实现的差异。
    /// </summary>
    public interface IAssetService : IDisposable
    {
        bool IsInitialized { get; }
        string PackageName { get; }

        Task InitializeAsync(CancellationToken cancellationToken = default);

        Task<IAssetLease<TAsset>> LoadAsync<TAsset>(
            string location,
            CancellationToken cancellationToken = default)
            where TAsset : UnityEngine.Object;

        Task<IInstanceLease> InstantiateAsync(
            string location,
            Transform parent = null,
            Vector3? position = null,
            Quaternion? rotation = null,
            CancellationToken cancellationToken = default);

        Task<ISceneLease> LoadSceneAsync(
            string location,
            LoadSceneMode mode = LoadSceneMode.Single,
            bool activateOnLoad = true,
            CancellationToken cancellationToken = default);
    }
}
