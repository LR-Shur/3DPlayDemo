using System;
using System.Threading;
using System.Threading.Tasks;
using Train.Architecture.Assets;
using Train.Architecture.Bootstrap;
using Train.Architecture.Input;
using Train.Characters.Application;
using Train.Characters.Data;
using Train.Characters.Events;
using Train.Composition.Events;
using Train.Dialogue.Application;
using Train.Dialogue.Data;
using Train.Dialogue.Events;
using Train.Equipment.Application;
using Train.Equipment.Data;
using Train.Equipment.Events;
using Train.GameFlow.Application;
using Train.Composition.Config;
using Train.Inventory.Application;
using Train.Inventory.Data;
using Train.Inventory.Events;
using Train.Presentation.UI.Events;
using Train.Presentation.UI.Runtime;
using Train.Quest.Application;
using Train.Quest.Data;
using Train.Quest.Events;
using Train.Composition.Progression;
using UnityEngine;

namespace Train.Composition
{
    /// <summary>
    /// 常驻游戏组合根。
    /// 它按依赖顺序安装资源、背包、装备和 UI 服务，使任意场景都能单独打开测试。
    /// </summary>
    [DefaultExecutionOrder(-9000)]
    [DisallowMultipleComponent]
    public sealed class GameApplicationStartup : MonoBehaviour
    {
        private const int AssetServiceWaitFrames = 240;

        private CancellationTokenSource _lifetime;
        private Task _initializationTask;

        /// <summary>全部应用服务是否已经安装完成。</summary>
        public bool IsReady { get; private set; }

        /// <summary>获取本次异步启动任务，供启动场景和自动化测试等待。</summary>
        public Task InitializationTask =>
            _initializationTask ?? Task.CompletedTask;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AttachToBootstrap()
        {
            var bootstrap = GameBootstrap.EnsureExists();
            var startup = bootstrap.GetComponent<GameApplicationStartup>();
            if (startup == null)
            {
                startup =
                    bootstrap.gameObject.AddComponent<GameApplicationStartup>();
            }

            // 编辑器在 PlayMode 内发生脚本域重载时，原组件仍可能存在，
            // 但非序列化的启动字段会被清空，因此这里必须主动自愈。
            startup.EnsureRuntimeInitialized();
        }

        private void Awake()
        {
            EnsureRuntimeInitialized();
        }

        private void OnEnable()
        {
            EnsureRuntimeInitialized();
        }

        private void Start()
        {
            EnsureRuntimeInitialized();
        }

        /// <summary>
        /// 幂等恢复组合根的运行时字段与服务。
        /// 除了正常启动外，也支持编辑器 PlayMode 脚本域重载后的自愈。
        /// </summary>
        private void EnsureRuntimeInitialized()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (_lifetime == null || _lifetime.IsCancellationRequested)
            {
                _lifetime?.Dispose();
                _lifetime = new CancellationTokenSource();
            }

            var services = GameBootstrap.Instance.Context.Services;
            // 先注册 Luban 服务，供物品、装备、奖励和敌人原型读取。
            if (!services.TryResolve<ILubanConfigService>(out var lubanConfig))
            {
                lubanConfig = new LubanConfigService();
                services.Install<ILubanConfigService>(lubanConfig);
            }

            if (!services.TryResolve<IEnemyArchetypeProvider>(out _))
            {
                services.Install<IEnemyArchetypeProvider>(
                    new LubanEnemyArchetypeProvider(lubanConfig));
            }

            if (!services.TryResolve<ILevelSessionRegistry>(out _))
            {
                services.Install<ILevelSessionRegistry>(
                    new LevelSessionRegistry(
                        GameBootstrap.Instance.Context.Events));
            }

            if (!services.TryResolve<IInputModeService>(out _))
            {
                services.Install<IInputModeService>(
                    new InputModeService(
                        GameBootstrap.Instance.Context.Events));
            }

            if (!services.TryResolve<IProgressionService>(out _))
            {
                var progressionSettings = Resources.Load<ProgressionSettings>(
                    ProgressionSettings.ResourcesLocation);
                services.Install<IProgressionService>(
                    new ProgressionService(
                        GameBootstrap.Instance.Context.Events,
                        progressionSettings));
            }

            if (gameObject.GetComponent<ProgressionRuntimeController>() == null)
            {
                gameObject.AddComponent<ProgressionRuntimeController>();
            }

            if (gameObject.GetComponent<CombatFeedbackRuntimeController>() == null)
            {
                gameObject.AddComponent<CombatFeedbackRuntimeController>();
            }

            if (gameObject.GetComponent<DialogueLevelStartController>() == null)
            {
                gameObject.AddComponent<DialogueLevelStartController>();
            }

            if (gameObject.GetComponent<RunSaveRuntimeController>() == null)
            {
                gameObject.AddComponent<RunSaveRuntimeController>();
            }

            if (gameObject.GetComponent<EquipmentCombatRuntimeController>() == null)
            {
                gameObject.AddComponent<EquipmentCombatRuntimeController>();
            }

            if (gameObject.GetComponent<ActiveItemRuntimeController>() == null)
            {
                gameObject.AddComponent<ActiveItemRuntimeController>();
            }

            if (_initializationTask != null)
            {
                return;
            }

            _initializationTask = InitializeAsync(_lifetime.Token);
            ObserveInitialization(_initializationTask);
        }

        private async Task InitializeAsync(CancellationToken cancellationToken)
        {
            var game = GameBootstrap.Instance;

            try
            {
                var luban = game.Context.Services.Resolve<ILubanConfigService>();
                await luban.InitializeAsync(cancellationToken);

                var assets = await WaitForAssetServiceAsync(
                    game,
                    cancellationToken);
                await assets.InitializeAsync(cancellationToken);

                await InstallInventoryAsync(
                    game,
                    assets,
                    cancellationToken);
                await InstallEquipmentAsync(
                    game,
                    assets,
                    cancellationToken);
                await InstallQuestAsync(
                    game,
                    assets,
                    cancellationToken);
                await InstallCharactersAsync(
                    game,
                    assets,
                    cancellationToken);
                await InstallDialogueAsync(
                    game,
                    assets,
                    cancellationToken);
                await InstallUIAsync(
                    game,
                    assets,
                    cancellationToken);

                IsReady = true;
                game.Context.Events.Publish(
                    new GameApplicationReadyEvent());
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                // Normal shutdown while an asynchronous startup step is running.
            }
            catch (AssetServiceException exception)
                when (GameBootstrap.IsApplicationQuitting ||
                      exception.Message.IndexOf(
                          "aborted",
                          StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // 退出时 YooAsset 会先关闭调度器，abort 属于正常取消。
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                game.Context.Events.Publish(
                    new GameApplicationFailedEvent(exception.Message));
            }
        }

        /// <summary>
        /// 通过统一资源服务加载装备配置，并把装备 Server 安装到服务注册表。
        /// </summary>
        private static async Task InstallEquipmentAsync(
            GameBootstrap game,
            IAssetService assets,
            CancellationToken cancellationToken)
        {
            if (game.Context.Services.TryResolve<IEquipmentService>(out _))
            {
                return;
            }

            IAssetLease<EquipmentSettings> settingsLease = null;
            try
            {
                settingsLease =
                    await assets.LoadAsync<EquipmentSettings>(
                        AssetLocations.EquipmentSettings,
                        cancellationToken);
                if (game.Context.Services.TryResolve<ILubanConfigService>(
                        out var luban) &&
                    luban.IsReady)
                {
                    var runtimeSettings =
                        LubanRuntimeSettingsFactory.CreateEquipmentSettings(
                            luban.Tables,
                            settingsLease.Asset);
                    settingsLease.Dispose();
                    settingsLease =
                        LubanRuntimeSettingsFactory.CreateLease(
                            runtimeSettings,
                            "luban://equipment");
                }

                var service = new EquipmentService(
                    settingsLease.Asset,
                    game.Context.Events,
                    settingsLease);
                settingsLease = null;
                game.Context.Services.Install<IEquipmentService>(service);
                game.Context.Events.Publish(
                    new EquipmentSystemReadyEvent(
                        service.Catalog.Count,
                        service.Snapshot.Revision));
            }
            catch (Exception exception)
            {
                settingsLease?.Dispose();
                game.Context.Events.Publish(
                    new EquipmentSystemFailedEvent(exception.Message));
                throw;
            }
        }

        /// <summary>
        /// 加载常驻 UI 根并安装 UI 服务。
        /// </summary>
        private static async Task InstallUIAsync(
            GameBootstrap game,
            IAssetService assets,
            CancellationToken cancellationToken)
        {
            if (game.Context.Services.TryResolve<IUIService>(out _))
            {
                return;
            }

            IInstanceLease rootLease = null;
            try
            {
                rootLease = await assets.InstantiateAsync(
                    AssetLocations.GameUiRoot,
                    cancellationToken: cancellationToken);
                var service = new UIService(
                    rootLease,
                    game.Context.Services.Resolve<IInventoryService>(),
                    game.Context.Services.Resolve<IEquipmentService>(),
                    game.Context.Services.Resolve<IQuestService>(),
                    game.Context.Services.Resolve<ICharacterRosterService>(),
                    game.Context.Services.Resolve<IDialogueService>(),
                    game.Context.Services.Resolve<ILevelSessionRegistry>(),
                    game.Context.Events,
                    game.Context.Services.Resolve<IInputModeService>());
                rootLease = null;
                game.Context.Services.Install<IUIService>(service);
                game.Context.Events.Publish(new UISystemReadyEvent());
            }
            catch (Exception exception)
            {
                rootLease?.Dispose();
                game.Context.Events.Publish(
                    new UISystemFailedEvent(exception.Message));
                throw;
            }
        }

        private static async Task<IAssetService> WaitForAssetServiceAsync(
            GameBootstrap game,
            CancellationToken cancellationToken)
        {
            for (var frame = 0;
                 frame < AssetServiceWaitFrames;
                 frame++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (game.Context.Assets != null)
                {
                    return game.Context.Assets;
                }

                await Task.Yield();
            }

            throw new InvalidOperationException(
                "Game application startup timed out while waiting for the " +
                "asset service. Ensure YooAssetRuntimeInstaller is included.");
        }

        /// <summary>
        /// 通过统一资源服务加载任务配置，并把任务 Server 安装到服务注册表。
        /// </summary>
        private static async Task InstallQuestAsync(
            GameBootstrap game,
            IAssetService assets,
            CancellationToken cancellationToken)
        {
            if (game.Context.Services.TryResolve<IQuestService>(out _))
            {
                return;
            }

            IAssetLease<QuestSettings> settingsLease = null;
            try
            {
                settingsLease =
                    await assets.LoadAsync<QuestSettings>(
                        AssetLocations.QuestSettings,
                        cancellationToken);
                var service = new QuestService(
                    settingsLease.Asset,
                    game.Context.Services.Resolve<IInventoryService>(),
                    game.Context.Events,
                    settingsLease);
                settingsLease = null;
                game.Context.Services.Install<IQuestService>(service);
                game.Context.Events.Publish(
                    new QuestSystemReadyEvent(
                        service.Catalog.Count,
                        service.Snapshots.Count,
                        service.TrackedQuestId));
            }
            catch (Exception exception)
            {
                settingsLease?.Dispose();
                game.Context.Events.Publish(
                    new CharacterSystemFailedEvent(exception.Message));
                throw;
            }
        }

        /// <summary>
        /// 通过统一资源服务加载角色名册，并把角色 Server 安装到服务注册表。
        /// </summary>
        private static async Task InstallCharactersAsync(
            GameBootstrap game,
            IAssetService assets,
            CancellationToken cancellationToken)
        {
            if (game.Context.Services.TryResolve<ICharacterRosterService>(out _))
            {
                return;
            }

            IAssetLease<CharacterSettings> settingsLease = null;
            try
            {
                settingsLease =
                    await assets.LoadAsync<CharacterSettings>(
                        AssetLocations.CharacterSettings,
                        cancellationToken);
                var service = new CharacterRosterService(
                    settingsLease.Asset,
                    game.Context.Events,
                    settingsLease);
                settingsLease = null;
                game.Context.Services.Install<ICharacterRosterService>(service);

                var snapshot = service.Snapshot;
                game.Context.Events.Publish(
                    new CharacterRosterReadyEvent(
                        service.Catalog.Count,
                        snapshot.UnlockedCount,
                        snapshot.SelectedCharacterId,
                        snapshot.Revision));
            }
            catch (Exception exception)
            {
                settingsLease?.Dispose();
                game.Context.Events.Publish(
                    new QuestSystemFailedEvent(exception.Message));
                throw;
            }
        }

        /// <summary>
        /// 通过统一资源服务加载对话图，并把对话 Server 安装到服务注册表。
        /// </summary>
        private static async Task InstallDialogueAsync(
            GameBootstrap game,
            IAssetService assets,
            CancellationToken cancellationToken)
        {
            if (game.Context.Services.TryResolve<IDialogueService>(out _))
            {
                return;
            }

            IAssetLease<DialogueSettings> settingsLease = null;
            try
            {
                settingsLease =
                    await assets.LoadAsync<DialogueSettings>(
                        AssetLocations.DialogueSettings,
                        cancellationToken);
                var service = new DialogueService(
                    settingsLease.Asset,
                    game.Context.Events,
                    settingsLease: settingsLease);
                settingsLease = null;
                game.Context.Services.Install<IDialogueService>(service);
                game.Context.Events.Publish(
                    new DialogueSystemReadyEvent(service.Catalog.Count));
            }
            catch (Exception exception)
            {
                settingsLease?.Dispose();
                game.Context.Events.Publish(
                    new DialogueSystemFailedEvent(exception.Message));
                throw;
            }
        }

        private static async Task InstallInventoryAsync(
            GameBootstrap game,
            IAssetService assets,
            CancellationToken cancellationToken)
        {
            if (game.Context.Services.TryResolve<IInventoryService>(out _))
            {
                return;
            }

            IAssetLease<InventorySettings> settingsLease = null;
            try
            {
                settingsLease =
                    await assets.LoadAsync<InventorySettings>(
                        AssetLocations.InventorySettings,
                        cancellationToken);
                if (game.Context.Services.TryResolve<ILubanConfigService>(
                        out var luban) &&
                    luban.IsReady)
                {
                    var runtimeSettings =
                        LubanRuntimeSettingsFactory.CreateInventorySettings(
                            luban.Tables,
                            settingsLease.Asset);
                    settingsLease.Dispose();
                    settingsLease =
                        LubanRuntimeSettingsFactory.CreateLease(
                            runtimeSettings,
                            "luban://items");
                }

                var service = new InventoryService(
                    settingsLease.Asset,
                    game.Context.Events,
                    settingsLease);
                settingsLease = null;
                game.Context.Services.Install<IInventoryService>(service);

                var snapshot = service.Snapshot;
                game.Context.Events.Publish(
                    new InventorySystemReadyEvent(
                        snapshot.Capacity,
                        snapshot.Revision));
            }
            catch (Exception exception)
            {
                settingsLease?.Dispose();
                game.Context.Events.Publish(
                    new InventorySystemFailedEvent(exception.Message));
                throw;
            }
        }

        private static async void ObserveInitialization(Task initialization)
        {
            try
            {
                await initialization;
            }
            catch (Exception exception)
            {
                // InitializeAsync normally reports errors itself. This is a final
                // guard against unobserved task exceptions.
                Debug.LogException(exception);
            }
        }

        private void OnDestroy()
        {
            _lifetime?.Cancel();
            _lifetime?.Dispose();
            _lifetime = null;
        }
    }
}

