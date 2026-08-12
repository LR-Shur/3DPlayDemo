using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Train.Architecture.Assets;
using Train.Architecture.Bootstrap;
using Train.Architecture.Events;
using Train.Architecture.Interaction;
using Train.GameFlow.Application;
using Train.GameFlow.Application.Events;
using Train.GameFlow.Core;
using Train.GameFlow.Data;
using Train.Gameplay.Combat;
using Train.Gameplay.Combat.Application.Events;
using Train.Gameplay.Combat.Factions;
using Train.Gameplay.Camera;
using Train.Gameplay.Enemy.Core;
using Train.Gameplay.Enemy.Combat;
using Train.Gameplay.Enemy.Data;
using Train.Gameplay.Player.Application.Events;
using Train.Gameplay.Player.Core;
using Train.Gameplay.Player.Input;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Train.GameFlow.Runtime
{
    /// <summary>
    /// 纯关卡流程状态机的 Unity 场景适配器。
    /// 它负责管理场景角色、将底层死亡消息转换为语义事件，
    /// 并使 Unity 与 YooAsset 引用不会进入流程核心层。
    /// </summary>
    [DefaultExecutionOrder(-7000)]
    [DisallowMultipleComponent]
    public sealed class LevelRuntimeController :
        MonoBehaviour,
        ILevelFlowActions,
        ILevelReadModel,
        ICombatInteractionGate
    {
        [SerializeField] private LevelDefinition _definition;
        [SerializeField] private Transform _spawnedActorsRoot;
        [SerializeField] private PlayerInputReader _playerInput;
        [SerializeField] private PlayerCombat _playerCombat;
        [SerializeField] private Health _playerHealth;
        [SerializeField] private Transform _playerSpawnPoint;
        [SerializeField] private string _playerPrefabLocation =
            AssetAddresses.PlayerPrefab;
        [SerializeField] private string _cameraPrefabLocation =
            AssetAddresses.PlayerCameraPrefab;
        [SerializeField] private bool _autoStart;

        [Header("Runtime diagnostics")]
        [SerializeField] private string _currentPhaseName;
        [SerializeField] private int _defeatedEnemyCount;
        [SerializeField] private int _playerDeathCount;

        private readonly List<IInstanceLease> _enemyLeases = new();
        private readonly Dictionary<Health, EnemyIdentity> _enemyByHealth = new();
        private readonly HashSet<Health> _defeatedEnemies = new();
        private readonly List<EnemyController> _enemyControllers = new();

        private CancellationTokenSource _lifetime;
        private IEventBus _events;
        private IDisposable _deathSubscription;
        private IDisposable _respawnSubscription;
        private IAssetLease<LevelDefinition> _ownedDefinitionLease;
        private LevelFlowStateMachine _flow;
        private Task _startupTask;
        private bool _prepared;
        private bool _startRequested;
        private bool _resultRequested;
        private float _combatStartedAt;
        private ILevelSessionRegistry _sessionRegistry;
        private IInstanceLease _playerLease;
        private IInstanceLease _cameraLease;

        /// <summary>获取当前关卡配置。</summary>
        public LevelDefinition Definition => _definition;

        /// <summary>获取当前关卡阶段。</summary>
        public LevelPhase CurrentPhase =>
            _flow?.CurrentPhase ?? LevelPhase.None;

        /// <summary>供通用交互层读取的战斗阶段标记。</summary>
        public bool IsCombatActive => CurrentPhase == LevelPhase.Combat;

        /// <summary>获取当前关卡登记的敌人总数。</summary>
        public int EnemyCount => _enemyByHealth.Count;

        /// <summary>获取已经击败的敌人数。</summary>
        public int DefeatedEnemyCount => _defeatedEnemyCount;

        /// <summary>获取供表现层读取的当前关卡快照。</summary>
        public LevelReadSnapshot Snapshot => new(
            _definition != null ? _definition.LevelId : string.Empty,
            _definition != null ? _definition.DisplayName : string.Empty,
            CurrentPhase,
            _flow?.Outcome ?? LevelOutcome.None,
            EnemyCount,
            _defeatedEnemyCount,
            _playerHealth != null ? _playerHealth.CurrentHealth : 0f,
            _playerHealth != null ? _playerHealth.MaxHealth : 0f,
            _playerDeathCount);

        private void Awake()
        {
            _lifetime = new CancellationTokenSource();
            _events = SceneBootstrap.ResolveEvents(this);
            _spawnedActorsRoot ??= transform.Find("SpawnedActors");
            _playerSpawnPoint ??= ResolvePlayerSpawnPoint();
            ResolvePlayer();
            SetLocalCombatEnabled(false);
            if (GameBootstrap.Instance.Context.Services.TryResolve(
                    out _sessionRegistry))
            {
                _sessionRegistry.Attach(this);
            }
        }

        private void Start()
        {
            // 只有显式配置自动开始的关卡才在进入场景时启动；第一关由训练对话完成事件启动。
            if (_autoStart)
            {
                StartLevel();
            }
        }

        /// <summary>当前是否正在等待玩家发出开始指令。</summary>
        public bool IsWaitingForStart => !_startRequested && _startupTask == null;

        /// <summary>显式开始本关卡，供 UI 按钮、引导流程和自动化测试调用。</summary>
        public void StartLevel()
        {
            if (_startupTask != null)
            {
                return;
            }

            _startRequested = true;
            _startupTask = RunStartupFlowAsync(_lifetime.Token);
            Observe(_startupTask);
        }

        /// <summary>
        /// 在启动流程开始前注入关卡配置。
        /// </summary>
        /// <param name="definition">要运行的关卡配置。</param>
        public void Configure(LevelDefinition definition)
        {
            if (_startupTask != null)
            {
                throw new InvalidOperationException(
                    "A level definition cannot be replaced after startup begins.");
            }

            _definition = definition != null
                ? definition
                : throw new ArgumentNullException(nameof(definition));
        }

        /// <summary>
        /// 加载关卡配置并生成、登记本关参战对象。
        /// </summary>
        public async Task PrepareAsync(CancellationToken cancellationToken)
        {
            if (_prepared)
            {
                return;
            }

            await ResolveDefinitionAsync(cancellationToken);
            EnsureEventSubscriptions();

            var assets = GameBootstrap.Instance.Context.Assets;
            if (assets == null)
            {
                throw new InvalidOperationException(
                    "LevelRuntimeController requires an installed IAssetService.");
            }

            await assets.InitializeAsync(cancellationToken);

            // 玩家预制体的 Awake 会尝试读取 Camera.main；先确保主相机存在，避免动态实例化时序导致玩家被误判为无效。
            PlayerCameraRuntimeBinder.EnsureMainCamera();
            await EnsurePlayerAsync(assets, cancellationToken);
            ValidatePlayer();
            SetLocalCombatEnabled(false);
            await EnsurePlayerCameraAsync(assets, cancellationToken);

            var pendingLeases = new List<IInstanceLease>();
            var pendingActors = new List<(Health Health, EnemyIdentity Identity, EnemyController Controller)>();
            try
            {
                foreach (var spawn in _definition.EnemySpawns)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (spawn == null ||
                        string.IsNullOrWhiteSpace(spawn.PrefabLocation))
                    {
                        continue;
                    }

                    var lease = await InstantiateEnemyAsync(
                        assets,
                        spawn,
                        cancellationToken);
                    pendingLeases.Add(lease);

                    var instance = lease.Instance;
                    instance.name = $"Enemy_{spawn.SpawnId}";
                    var identity = instance.GetComponent<EnemyIdentity>();
                    if (identity == null)
                    {
                        identity = instance.AddComponent<EnemyIdentity>();
                    }

                    identity.Configure(spawn.SpawnId, spawn.ArchetypeId);

                    var faction = instance.GetComponent<FactionMember>();
                    if (faction == null)
                    {
                        faction = instance.AddComponent<FactionMember>();
                    }

                    faction.Configure(CombatFaction.Enemy);

                    var health = instance.GetComponent<Health>();
                    var controller = instance.GetComponent<EnemyController>();
                    if (health == null || controller == null)
                    {
                        throw new InvalidOperationException(
                            $"Enemy prefab '{spawn.PrefabLocation}' requires " +
                            "Health and EnemyController components.");
                    }

                    controller.enabled = false;
                    ApplyLubanArchetype(spawn, instance, health, controller);
                    pendingActors.Add((health, identity, controller));
                }
            }
            catch
            {
                foreach (var lease in pendingLeases)
                {
                    lease.Dispose();
                }

                throw;
            }

            foreach (var lease in pendingLeases)
            {
                _enemyLeases.Add(lease);
            }

            foreach (var actor in pendingActors)
            {
                TrackEnemy(actor.Health, actor.Identity, actor.Controller);
            }

            TrackSceneEnemies();
            EnsurePlayerFaction();
            _prepared = true;
        }

        /// <summary>
        /// 通过 YooAsset 实例化敌人；编辑器直接打开训练场且资源模拟包尚未准备好时，
        /// 回退到 AssetDatabase，保证“模拟训练”仍能刷出敌人。
        /// </summary>
        private async Task<IInstanceLease> InstantiateEnemyAsync(
            IAssetService assets,
            EnemySpawnDefinition spawn,
            CancellationToken cancellationToken)
        {
            var spawnTransform = ResolveEnemySpawnTransform(spawn);
            try
            {
                return await assets.InstantiateAsync(
                    spawn.PrefabLocation,
                    _spawnedActorsRoot,
                    spawnTransform.Position,
                    spawnTransform.Rotation,
                    cancellationToken);
            }
            catch (Exception exception)
                when (!(exception is OperationCanceledException))
            {
#if UNITY_EDITOR
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    spawn.PrefabLocation);
                if (prefab != null)
                {
                    var instance = UnityEngine.Object.Instantiate(
                        prefab,
                        spawnTransform.Position,
                        spawnTransform.Rotation,
                        _spawnedActorsRoot);
                    Debug.Log(
                        $"训练场资源服务未就绪，已使用编辑器直接实例化敌人：" +
                        $"{spawn.PrefabLocation}。原始原因：{exception.Message}",
                        this);
                    return new EditorInstanceLease(
                        spawn.PrefabLocation,
                        instance);
                }
#endif
                throw;
            }
        }

        /// <summary>
        /// 优先读取场景中同 SpawnId 的 EnemySpawnPoint Transform；
        /// 没有场景点位时回退到关卡 SO 保存的数值坐标。
        /// </summary>
        private (Vector3 Position, Quaternion Rotation) ResolveEnemySpawnTransform(
            EnemySpawnDefinition spawn)
        {
            var points = FindObjectsByType<EnemySpawnPoint>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (var point in points)
            {
                if (point == null ||
                    point.gameObject.scene != gameObject.scene ||
                    !string.Equals(point.SpawnId, spawn.SpawnId, StringComparison.Ordinal))
                {
                    continue;
                }

                return (point.transform.position, point.transform.rotation);
            }

            return (spawn.Position, spawn.Rotation);
        }

        /// <summary>
        /// 等待关卡配置中指定的开场时长。
        /// </summary>
        public async Task PlayIntroAsync(CancellationToken cancellationToken)
        {
            var duration = Mathf.Max(0f, _definition.IntroSeconds);
            if (duration <= 0f)
            {
                return;
            }

            await Task.Delay(
                TimeSpan.FromSeconds(duration),
                cancellationToken);
        }

        /// <summary>
        /// 同步启用或禁用本场景中的玩家输入和敌人控制器。
        /// </summary>
        public Task SetCombatEnabledAsync(
            bool enabled,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (enabled)
            {
                // 进入战斗时同时打开玩家和敌人；玩家控制仍由关卡结果阶段决定。
                SetPlayerCombatEnabled(true);
                SetEnemyCombatEnabled(true);
                _combatStartedAt = Time.time;
            }
            else
            {
                // Combat -> Cleared/Failed 时只停止敌人 AI。
                // 通关后玩家仍应能在场景中移动、拾取和探索。
                SetEnemyCombatEnabled(false);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 发布关卡通关或失败的语义事件。
        /// </summary>
        public Task PresentResultAsync(
            LevelOutcome outcome,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var elapsed = Mathf.Max(0f, Time.time - _combatStartedAt);
            if (outcome == LevelOutcome.Cleared)
            {
                _events.Publish(
                    new LevelCompletedEvent(
                        _definition.LevelId,
                        elapsed));
            }
            else if (outcome == LevelOutcome.Failed)
            {
                // 失败结果进入后锁定玩家，等待重试或退出流程。
                SetPlayerCombatEnabled(false);
                _events.Publish(
                    new LevelFailedEvent(
                        _definition.LevelId,
                        "player_death_limit"));
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 退出关卡阶段时停止本地战斗逻辑。
        /// </summary>
        public Task ExitAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SetLocalCombatEnabled(false);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 请求当前关卡流程进入退出阶段。
        /// </summary>
        public Task RequestExitAsync(CancellationToken cancellationToken = default)
        {
            return _flow != null
                ? _flow.ExitAsync(cancellationToken)
                : Task.CompletedTask;
        }

        private async Task RunStartupFlowAsync(CancellationToken cancellationToken)
        {
            _flow = new LevelFlowStateMachine(this);
            _flow.PhaseChanged += OnPhaseChanged;

            await _flow.StartAsync(cancellationToken);
            await _flow.ShowIntroAsync(cancellationToken);
            await _flow.BeginCombatAsync(cancellationToken);

            if (_enemyByHealth.Count == 0)
            {
                RequestResult(LevelOutcome.Cleared);
            }
        }

        private async Task ResolveDefinitionAsync(
            CancellationToken cancellationToken)
        {
            if (_definition != null)
            {
                return;
            }

            if (LevelSceneLauncher.Instance != null &&
                LevelSceneLauncher.Instance.CurrentDefinition != null)
            {
                _definition = LevelSceneLauncher.Instance.CurrentDefinition;
                return;
            }

            var assets = GameBootstrap.Instance.Context.Assets;
            if (assets == null)
            {
                throw new InvalidOperationException(
                    "Cannot resolve a level definition before IAssetService is installed.");
            }

            await assets.InitializeAsync(cancellationToken);
            _ownedDefinitionLease = await assets.LoadAsync<LevelDefinition>(
                AssetAddresses.CombatArenaDefinition,
                cancellationToken);
            _definition = _ownedDefinitionLease.Asset;
        }

        /// <summary>把 Luban 敌人原型的生命、攻击、防御和移动速度应用到实例。</summary>
        private static void ApplyLubanArchetype(
            EnemySpawnDefinition spawn,
            GameObject instance,
            Health health,
            EnemyController controller)
        {
            if (!GameBootstrap.Instance.Context.Services.TryResolve<IEnemyArchetypeProvider>(
                    out var provider) ||
                !provider.TryGet(spawn.ArchetypeId, out var data))
            {
                return;
            }

            health.SetMaxHealth(data.MaxHealth, true);
            var stats = instance.GetComponent<CombatStatModifierComponent>();
            if (stats == null)
            {
                stats = instance.AddComponent<CombatStatModifierComponent>();
            }

            stats.SetDefense(data.Defense);
            var melee = instance.GetComponentInChildren<EnemyMeleeCombat>(true);
            melee?.ConfigureDamage(data.Attack);

            var runtimeConfig = EnemyConfig.CreateRuntime(
                controller.Config,
                data.MoveSpeed);
            controller.ApplyRuntimeConfig(runtimeConfig);
        }

        private void ResolvePlayer()
        {
            if (_playerHealth != null &&
                _playerInput != null &&
                _playerCombat != null)
            {
                return;
            }

            GameObject player = null;
            try
            {
                player = GameObject.FindGameObjectWithTag("Player");
            }
            catch (UnityException)
            {
                // ValidatePlayer will provide the actionable error.
            }

            if (player == null)
            {
                return;
            }

            _playerHealth ??= player.GetComponent<Health>();
            _playerInput ??= player.GetComponent<PlayerInputReader>();
            _playerCombat ??= player.GetComponent<PlayerCombat>();
        }

        /// <summary>
        /// 确保场景存在可玩的玩家：优先复用场景中的 Player，否则通过 YooAsset 实例化。
        /// </summary>
        private async Task EnsurePlayerAsync(
            IAssetService assets,
            CancellationToken cancellationToken)
        {
            ResolvePlayer();
            var spawnPoint = ResolvePlayerSpawnPoint();
            if (_playerHealth == null ||
                _playerInput == null ||
                _playerCombat == null)
            {
                var location = string.IsNullOrWhiteSpace(_playerPrefabLocation)
                    ? AssetAddresses.PlayerPrefab
                    : _playerPrefabLocation;
                _playerLease = await assets.InstantiateAsync(
                    location,
                    position: spawnPoint != null
                        ? spawnPoint.position
                        : transform.position,
                    rotation: spawnPoint != null
                        ? spawnPoint.rotation
                        : transform.rotation,
                    cancellationToken: cancellationToken);

                var player = _playerLease.Instance;
                player.name = "Player";
                player.tag = "Player";
                ResolvePlayer();
            }

            if (_playerHealth == null)
            {
                return;
            }

            _playerHealth.transform.root.SetPositionAndRotation(
                spawnPoint != null ? spawnPoint.position : transform.position,
                spawnPoint != null ? spawnPoint.rotation : transform.rotation);
            _playerHealth.GetComponent<PlayerRespawnController>()
                ?.ConfigureSpawnPoint(spawnPoint);
        }

        /// <summary>确保主相机拥有绑定到当前玩家的 Cinemachine 虚拟相机。</summary>
        private async Task EnsurePlayerCameraAsync(
            IAssetService assets,
            CancellationToken cancellationToken)
        {
            PlayerCameraRuntimeBinder.EnsureMainCamera();
            var playerRoot = _playerHealth.transform.root;
            var target = playerRoot.Find("PlayerCameraTarget") ?? playerRoot;
            var virtualCamera =
                PlayerCameraRuntimeBinder.FindVirtualCamera(gameObject.scene);

            if (virtualCamera == null)
            {
                var location = string.IsNullOrWhiteSpace(_cameraPrefabLocation)
                    ? AssetAddresses.PlayerCameraPrefab
                    : _cameraPrefabLocation;
                _cameraLease = await assets.InstantiateAsync(
                    location,
                    cancellationToken: cancellationToken);
                virtualCamera = _cameraLease.Instance.GetComponent(
                    "CinemachineCamera");
            }

            if (!PlayerCameraRuntimeBinder.TryBind(
                    virtualCamera,
                    target,
                    _playerInput))
            {
                throw new InvalidOperationException(
                    "Player camera prefab requires a CinemachineCamera component.");
            }
        }

        /// <summary>读取关卡 SO 中配置的世界出生点，旧场景没有 SO 时才读取场景点位。</summary>
        private Transform ResolvePlayerSpawnPoint()
        {
            if (_playerSpawnPoint != null)
            {
                return _playerSpawnPoint;
            }

            // SO 保存的是世界坐标，不能使用 SetLocalPosition，否则会叠加 LevelRuntimeRoot 的场景偏移。
            if (_definition != null)
            {
                var runtimePoint = new GameObject("[RuntimePlayerSpawn]");
                runtimePoint.transform.SetParent(transform, false);
                runtimePoint.transform.SetPositionAndRotation(
                    _definition.PlayerSpawnPosition,
                    _definition.PlayerSpawnRotation);
                _playerSpawnPoint = runtimePoint.transform;
                return _playerSpawnPoint;
            }

            var point = GetComponentInChildren<PlayerSpawnPoint>(true);
            _playerSpawnPoint = point != null
                ? point.transform
                : transform.Find("Spawns/PlayerSpawn") ??
                  transform.Find("Spawns/PlayerSpawnPoint");
            if (_playerSpawnPoint != null)
            {
                return _playerSpawnPoint;
            }

            return null;
        }

        private void ValidatePlayer()
        {
            if (_playerHealth == null ||
                _playerInput == null ||
                _playerCombat == null)
            {
                throw new InvalidOperationException(
                    "Playable level requires a Player-tagged object with " +
                    "Health, PlayerInputReader and PlayerCombat.");
            }
        }

        private void EnsureEventSubscriptions()
        {
            _deathSubscription ??=
                _events.Subscribe<EntityDiedEvent>(OnEntityDied);
            _respawnSubscription ??=
                _events.Subscribe<PlayerRespawnCompletedEvent>(
                    OnPlayerRespawnCompleted);
        }

        private void TrackSceneEnemies()
        {
            foreach (var controller in FindObjectsByType<EnemyController>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (controller.gameObject.scene != gameObject.scene)
                {
                    continue;
                }

                var health = controller.GetComponent<Health>();
                if (health == null || _enemyByHealth.ContainsKey(health))
                {
                    continue;
                }

                var identity = controller.GetComponent<EnemyIdentity>();
                if (identity == null)
                {
                    identity =
                        controller.gameObject.AddComponent<EnemyIdentity>();
                }

                TrackEnemy(health, identity, controller);
            }
        }

        private void TrackEnemy(
            Health health,
            EnemyIdentity identity,
            EnemyController controller)
        {
            _enemyByHealth[health] = identity;
            if (!_enemyControllers.Contains(controller))
            {
                _enemyControllers.Add(controller);
            }

            controller.enabled = false;
        }

        private void EnsurePlayerFaction()
        {
            var root = _playerHealth.transform.root.gameObject;
            var faction = root.GetComponent<FactionMember>();
            if (faction == null)
            {
                faction = root.AddComponent<FactionMember>();
            }

            faction.Configure(CombatFaction.Player);
            _playerCombat.ConfigureFaction(CombatFaction.Player);
        }

        private void SetLocalCombatEnabled(bool enabled)
        {
            SetPlayerCombatEnabled(enabled);
            SetEnemyCombatEnabled(enabled);
        }

        /// <summary>
        /// 独立控制玩家输入与玩家战斗组件。
        /// 通关后不调用此方法的关闭分支，以便玩家继续探索场景。
        /// </summary>
        private void SetPlayerCombatEnabled(bool enabled)
        {
            if (_playerInput != null)
            {
                // 保持输入读取器启用，只切换动作地图，避免关卡尚未开始时
                // 无法读取“开始”指令，也避免启停组件造成输入回调竞态。
                _playerInput.enabled = true;
                _playerInput.SetExternalInputBlocked(!enabled);
            }

            if (_playerCombat != null)
            {
                if (!enabled)
                {
                    _playerCombat.EndAttack();
                }

                _playerCombat.enabled = enabled;
            }
        }

        /// <summary>
        /// 独立控制敌人 AI；已死亡敌人不会被重新启用。
        /// </summary>
        private void SetEnemyCombatEnabled(bool enabled)
        {
            foreach (var controller in _enemyControllers)
            {
                if (controller != null)
                {
                    var health = controller.GetComponent<Health>();
                    controller.enabled =
                        enabled && health != null && health.IsAlive;
                }
            }
        }

        private void OnEntityDied(EntityDiedEvent message)
        {
            if (_flow == null || _flow.CurrentPhase != LevelPhase.Combat)
            {
                return;
            }

            if (_enemyByHealth.TryGetValue(
                    message.Health,
                    out var identity))
            {
                if (!_defeatedEnemies.Add(message.Health))
                {
                    return;
                }

                _defeatedEnemyCount = _defeatedEnemies.Count;
                _events.Publish(
                    new EnemyDefeatedEvent(
                        _definition.LevelId,
                        identity.ArchetypeId,
                        identity.InstanceId,
                        ResolveKillerId(message.KillingBlow)));

                if (_defeatedEnemyCount >= _enemyByHealth.Count)
                {
                    RequestResult(LevelOutcome.Cleared);
                }

                return;
            }

            if (message.Health != _playerHealth)
            {
                return;
            }

            _playerDeathCount++;
            if (_definition.MaxPlayerDeaths > 0 &&
                _playerDeathCount >= _definition.MaxPlayerDeaths)
            {
                RequestResult(LevelOutcome.Failed);
            }
        }

        private void OnPlayerRespawnCompleted(
            PlayerRespawnCompletedEvent message)
        {
            if (_flow == null ||
                _flow.CurrentPhase == LevelPhase.Failed ||
                _flow.CurrentPhase == LevelPhase.Exiting)
            {
                SetLocalCombatEnabled(false);
            }
        }

        private void RequestResult(LevelOutcome outcome)
        {
            if (_resultRequested)
            {
                return;
            }

            _resultRequested = true;
            var task = outcome == LevelOutcome.Cleared
                ? _flow.CompleteAsync(_lifetime.Token)
                : _flow.FailAsync(_lifetime.Token);
            Observe(task);
        }

        private void OnPhaseChanged(LevelPhaseChanged change)
        {
            _currentPhaseName = change.Current.ToString();
            _events.Publish(
                new LevelPhaseChangedEvent(
                    _definition != null ? _definition.LevelId : string.Empty,
                    change.Previous,
                    change.Current,
                    change.Outcome));
        }

        private static string ResolveKillerId(DamageInfo killingBlow)
        {
            var owner = killingBlow.Source?.OwnerTransform;
            return owner != null
                ? owner.root.name
                : "environment";
        }

        private static async void Observe(Task task)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // Normal during scene unload or play-mode exit.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

#if UNITY_EDITOR
        /// <summary>编辑器直接实例化对象的轻量租约，释放时销毁对象。</summary>
        private sealed class EditorInstanceLease : IInstanceLease
        {
            public EditorInstanceLease(string location, GameObject instance)
            {
                Location = location;
                Instance = instance;
            }

            public string Location { get; }
            public GameObject Instance { get; private set; }
            public bool IsValid => Instance != null;

            public void Dispose()
            {
                if (Instance != null)
                {
                    UnityEngine.Object.Destroy(Instance);
                    Instance = null;
                }
            }
        }
#endif

        private void OnDestroy()
        {
            _lifetime?.Cancel();
            _lifetime?.Dispose();
            _lifetime = null;

            if (_flow != null)
            {
                _flow.PhaseChanged -= OnPhaseChanged;
            }

            _deathSubscription?.Dispose();
            _deathSubscription = null;
            _respawnSubscription?.Dispose();
            _respawnSubscription = null;

            foreach (var lease in _enemyLeases)
            {
                lease.Dispose();
            }

            _enemyLeases.Clear();
            _ownedDefinitionLease?.Dispose();
            _ownedDefinitionLease = null;
            _cameraLease?.Dispose();
            _cameraLease = null;
            _playerLease?.Dispose();
            _playerLease = null;
            _sessionRegistry?.Detach(this);
            _sessionRegistry = null;
        }
    }
}
