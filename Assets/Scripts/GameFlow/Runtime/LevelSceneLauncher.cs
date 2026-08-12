using System;
using System.Threading;
using System.Threading.Tasks;
using Train.Architecture.Assets;
using Train.Architecture.Bootstrap;
using Train.GameFlow.Application.Events;
using Train.GameFlow.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Train.GameFlow.Runtime
{
    /// <summary>
    /// 由轻量启动场景使用的常驻关卡加载器。
    /// 在关卡被替换前持续持有关卡配置与场景租约。
    /// </summary>
    [DefaultExecutionOrder(-8000)]
    [DisallowMultipleComponent]
    public sealed class LevelSceneLauncher : MonoBehaviour
    {
        [SerializeField] private string _levelDefinitionLocation =
            AssetAddresses.CombatArenaDefinition;

        private static LevelSceneLauncher _instance;
        private CancellationTokenSource _lifetime;
        private IAssetLease<LevelDefinition> _definitionLease;
        private ISceneLease _sceneLease;
        private Task _loadTask;

        /// <summary>获取当前存活的关卡加载器实例。</summary>
        public static LevelSceneLauncher Instance => _instance;

        /// <summary>获取当前已加载的关卡配置。</summary>
        public LevelDefinition CurrentDefinition => _definitionLease?.Asset;

        /// <summary>获取当前是否正在执行关卡加载任务。</summary>
        public bool IsLoading => _loadTask != null && !_loadTask.IsCompleted;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _lifetime = new CancellationTokenSource();
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _loadTask = LoadLevelAsync(
                _levelDefinitionLocation,
                _lifetime.Token);
            Observe(_loadTask);
        }

        /// <summary>
        /// 卸载当前关卡并按已配置的关卡地址重新加载。
        /// </summary>
        public Task ReloadAsync(CancellationToken cancellationToken = default)
        {
            if (IsLoading)
            {
                return _loadTask;
            }

            _loadTask = ReloadCoreAsync(cancellationToken);
            Observe(_loadTask);
            return _loadTask;
        }

        private async Task ReloadCoreAsync(CancellationToken cancellationToken)
        {
            if (_sceneLease != null)
            {
                var transitionScene =
                    SceneManager.CreateScene("[LevelTransition]");
                SceneManager.SetActiveScene(transitionScene);
                await _sceneLease.UnloadAsync();
                _sceneLease = null;
            }

            var location = string.IsNullOrWhiteSpace(_levelDefinitionLocation)
                ? AssetAddresses.CombatArenaDefinition
                : _levelDefinitionLocation;
            await LoadLevelAsync(location, cancellationToken);
        }

        private async Task LoadLevelAsync(
            string definitionLocation,
            CancellationToken cancellationToken)
        {
            var game = GameBootstrap.EnsureExists();
            var assets = game.Context.Assets;
            if (assets == null)
            {
                throw new InvalidOperationException(
                    "LevelSceneLauncher requires an installed IAssetService.");
            }

            await assets.InitializeAsync(cancellationToken);

            _definitionLease?.Dispose();
            _definitionLease = await assets.LoadAsync<LevelDefinition>(
                definitionLocation,
                cancellationToken);

            var definition = _definitionLease.Asset;
            if (definition == null)
            {
                throw new InvalidOperationException(
                    $"Level definition '{definitionLocation}' loaded without an asset.");
            }

            var sceneLocation = string.IsNullOrWhiteSpace(definition.SceneLocation)
                ? AssetLocations.CombatArenaScene
                : definition.SceneLocation;
            _sceneLease = await assets.LoadSceneAsync(
                sceneLocation,
                LoadSceneMode.Single,
                true,
                cancellationToken);

            game.Context.Events.Publish(
                new LevelSceneLoadedEvent(
                    definition.LevelId,
                    sceneLocation));
        }

        private static async void Observe(Task task)
        {
            try
            {
                await task;
            }
            catch (AssetServiceException exception)
                when (GameBootstrap.IsApplicationQuitting ||
                      exception.Message.IndexOf(
                          "aborted",
                          StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // 退出 PlayMode 时资源调度器先于关卡加载任务关闭，
                // 这是正常取消，不写入控制台错误。
            }
            catch (OperationCanceledException)
            {
                // Normal when play mode exits or the launcher is replaced.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void OnDestroy()
        {
            if (_instance != this)
            {
                return;
            }

            _lifetime?.Cancel();
            _lifetime?.Dispose();
            _lifetime = null;

            // 应用退出时 YooAsset 的全局调度器可能已经先行关闭。
            // 此时操作系统会回收全部资源，不再发起异步场景卸载。
            if (!GameBootstrap.IsApplicationQuitting)
            {
                _sceneLease?.Dispose();
                _definitionLease?.Dispose();
            }

            _sceneLease = null;
            _definitionLease = null;
            _instance = null;
        }
    }
}
