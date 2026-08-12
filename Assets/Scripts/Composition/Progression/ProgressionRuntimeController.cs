using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using Train.Architecture.Assets;
using Train.Architecture.Bootstrap;
using Train.Architecture.Events;
using Train.Architecture.Input;
using Train.Composition.Config;
using Train.Equipment.Application;
using Train.Equipment.Core;
using Train.Equipment.Data;
using Train.GameFlow.Application.Events;
using Train.GameFlow.Runtime;
using Train.GameFlow.Data;
using Train.Gameplay.Combat;
using Train.Gameplay.Combat.Application.Events;
using Train.Inventory.Application;
using Train.Inventory.Data;
using Train.Presentation.UI.Views;
using Train.WorldInteraction.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Train.Composition.Progression
{
    /// <summary>
    /// 把战斗事件接到掉落、金币、结算面板和商店。
    /// 这是第一版商业循环的组合层适配器，领域规则仍留在服务中。
    /// </summary>
    [DefaultExecutionOrder(-8500)]
    [DisallowMultipleComponent]
    public sealed class ProgressionRuntimeController : MonoBehaviour
    {
        private const string PickupLocation =
            AssetAddresses.WorldItemPickupPrefab;

        private readonly List<IInstanceLease> _lootLeases = new();
        private readonly HashSet<string> _rewardedLevels = new(
            StringComparer.Ordinal);

        private IProgressionService _progression;
        private IInventoryService _inventory;
        private IEquipmentService _equipment;
        private IAssetService _assets;
        private IEventBus _events;
        private ILubanConfigService _luban;
        private IDisposable _enemySubscription;
        private IDisposable _levelSubscription;
        private IDisposable _sceneSubscription;
        private CancellationTokenSource _lifetime;
        private ProgressionOverlay _overlay;
        private string _returnSceneLocation;

        private void Awake()
        {
            _lifetime = new CancellationTokenSource();
            DontDestroyOnLoad(gameObject);
            TryInitialize();
        }

        private void Update()
        {
            TryInitialize();
            _overlay?.Tick();
        }

        private void TryInitialize()
        {
            if (_overlay != null)
            {
                return;
            }

            var bootstrap = GameBootstrap.EnsureExists();
            if (!bootstrap.Context.Services.TryResolve<IProgressionService>(out var progression) ||
                !bootstrap.Context.Services.TryResolve<IInventoryService>(out var inventory) ||
                !bootstrap.Context.Services.TryResolve<IEquipmentService>(out var equipment))
            {
                return;
            }

            _progression = progression;
            _inventory = inventory;
            _equipment = equipment;
            _assets = bootstrap.Context.Assets;
            _events = bootstrap.Context.Events;
            bootstrap.Context.Services.TryResolve<ILubanConfigService>(out _luban);
            bootstrap.Context.Services.TryResolve<IInputModeService>(out var inputMode);
            _enemySubscription = _events.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
            _levelSubscription = _events.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
            _sceneSubscription = _events.Subscribe<LevelSceneLoadedEvent>(OnSceneLoaded);
            _overlay = ProgressionOverlay.Create(
                _events,
                _progression,
                _inventory,
                _equipment,
                inputMode,
                LoadNextNode,
                OpenShopScene,
                ReturnFromShop);
            _overlay.RenderWallet(_progression.Snapshot);
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent message)
        {
            if (_progression == null)
            {
                return;
            }

            var coins = message.EnemyArchetypeId.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0
                ? 120
                : message.EnemyArchetypeId.IndexOf("elite", StringComparison.OrdinalIgnoreCase) >= 0
                    ? 55
                    : 28;
            _progression.AddCoins(coins, $"击败敌人：{message.EnemyArchetypeId}");
            _overlay?.ShowCombatToast($"+{coins} 金币", "敌人已击破");
            SpawnLoot(message);
        }

        private void OnLevelCompleted(LevelCompletedEvent message)
        {
            if (_progression == null || !_rewardedLevels.Add(message.LevelId))
            {
                return;
            }

            _progression.TryGetNode(message.LevelId, out var completedNode);
            _progression.CompleteLevel(message.LevelId, out var nextNode);
            var rewards = ResolveLevelRewards(message.LevelId, completedNode.IsBoss);
            var rewardSummary = new List<string>(rewards.Count);
            foreach (var reward in rewards)
            {
                if (_inventory != null &&
                    _inventory.TryAdd(reward.ItemId, reward.Count))
                {
                    var displayName = ResolveItemDisplayName(reward.ItemId);
                    rewardSummary.Add($"{displayName} ×{reward.Count}");
                }
            }

            _overlay?.ShowCompletion(
                message.LevelId,
                message.ElapsedSeconds,
                completedNode.DisplayName,
                string.Join("\n", rewardSummary),
                nextNode);
        }

        private void OnSceneLoaded(LevelSceneLoadedEvent message)
        {
            ClearLootLeases();
            _overlay?.HideAllPanels();
            if (_progression != null && _progression.TryGetNode(message.LevelId, out var node))
            {
                _overlay?.ShowLevelBanner(node);
            }

            _overlay?.RefreshBossBar();
        }

        /// <summary>由商店 NPC 的对话完成事件调用，打开常驻商店界面。</summary>
        public void OpenShopFromNpc()
        {
            TryInitialize();
            _overlay?.ShowShopFromNpc();
        }

        /// <summary>从结算面板进入独立商店场景，并记住返回的战斗场景。</summary>
        private void OpenShopScene()
        {
            _returnSceneLocation = SceneManager.GetActiveScene().path;
            _overlay?.HideAllPanels();
            SceneManager.LoadSceneAsync(AssetLocations.ShopScene, LoadSceneMode.Single);
        }

        /// <summary>关闭商店后返回进入商店前的战斗场景。</summary>
        private async void ReturnFromShop()
        {
            var location = string.IsNullOrWhiteSpace(_returnSceneLocation)
                ? AssetLocations.CombatArenaScene
                : _returnSceneLocation;
            _overlay?.HideAllPanels();
            var operation = SceneManager.LoadSceneAsync(location, LoadSceneMode.Single);
            while (operation != null && !operation.isDone)
            {
                await Task.Yield();
            }

            var level = FindFirstObjectByType<LevelRuntimeController>();
            if (level != null && level.IsWaitingForStart)
            {
                await level.PrepareAsync(_lifetime.Token);
                level.StartLevel();
            }
        }

        /// <summary>从当前关卡 SO 读取奖励；旧场景未配置时使用最小兜底奖励。</summary>
        private List<RewardGrant> ResolveLevelRewards(string levelId, bool isBoss)
        {
            var result = new List<RewardGrant>();
            var levelRuntime = FindFirstObjectByType<LevelRuntimeController>();
            if (levelRuntime != null &&
                levelRuntime.Definition != null &&
                string.Equals(levelRuntime.Definition.LevelId, levelId, StringComparison.Ordinal))
            {
                foreach (var reward in levelRuntime.Definition.Rewards)
                {
                    if (reward != null && !string.IsNullOrWhiteSpace(reward.ItemId))
                    {
                        result.Add(new RewardGrant(reward.ItemId, reward.Count));
                    }
                }

                return result;
            }

            if (result.Count == 0)
            {
                result.Add(new RewardGrant(
                    isBoss ? "upgrade_module" : "training_chip",
                    isBoss ? 8 : 6));
            }

            return result;
        }

        /// <summary>通过物品目录把稳定 ID 转成玩家可读的中文名称。</summary>
        private string ResolveItemDisplayName(string itemId)
        {
            if (_inventory != null &&
                _inventory.TryGetDefinition(itemId, out var definition) &&
                definition != null &&
                !string.IsNullOrWhiteSpace(definition.DisplayName))
            {
                return definition.DisplayName;
            }

            return itemId switch
            {
                "training_chip" => "训练芯片",
                "healing_canister" => "急救罐",
                "upgrade_module" => "强化模块",
                "city_token" => "城市场景代币",
                "thunder_ring" => "雷鸣指环",
                "thunder_blade" => "雷鸣刀",
                _ => itemId
            };
        }

        private readonly struct RewardGrant
        {
            public RewardGrant(string itemId, int count)
            {
                ItemId = itemId;
                Count = count;
            }

            public string ItemId { get; }
            public int Count { get; }
        }

        private void SpawnLoot(EnemyDefeatedEvent message)
        {
            if (_assets == null || !_assets.IsInitialized)
            {
                return;
            }

            var itemId = message.EnemyArchetypeId.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0
                ? "upgrade_module"
                : "training_chip";
            var position = ResolveEnemyPosition(message.EnemyInstanceId);
            SpawnLootAsync(itemId, position, message.EnemyInstanceId, _lifetime.Token);
        }

        private async void SpawnLootAsync(
            string itemId,
            Vector3 position,
            string instanceId,
            CancellationToken cancellationToken)
        {
            try
            {
                var lease = await _assets.InstantiateAsync(
                    PickupLocation,
                    position: position + Vector3.up * 0.35f,
                    cancellationToken: cancellationToken);
                var pickup = lease.Instance.GetComponent<WorldItemPickup>();
                if (pickup == null)
                {
                    lease.Dispose();
                    return;
                }

                pickup.Configure(
                    $"loot_{instanceId}_{itemId}",
                    itemId,
                    itemId == "upgrade_module" ? 2 : 3,
                    25);
                _lootLeases.Add(lease);
            }
            catch (OperationCanceledException)
            {
                // 场景切换或退出时不再生成旧场景掉落。
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static Vector3 ResolveEnemyPosition(string instanceId)
        {
            var identityName = $"Enemy_{instanceId}";
            var identity = GameObject.Find(identityName);
            return identity != null ? identity.transform.position : Vector3.zero;
        }

        private void LoadNextNode(RunNode node)
        {
            if (string.IsNullOrWhiteSpace(node.ScenePath))
            {
                return;
            }

            ClearLootLeases();
            _overlay?.HideAllPanels();
            LoadSceneByAddressAsync(node.ScenePath);
        }

        /// <summary>通过统一资源服务按 YooAsset 地址加载下一关，流程层不直接调用 SceneManager。</summary>
        private async void LoadSceneByAddressAsync(string address)
        {
            try
            {
                if (_assets == null)
                {
                    throw new InvalidOperationException("关卡切换需要已初始化的 YooAsset 服务。");
                }

                await _assets.LoadSceneAsync(address, LoadSceneMode.Single, true, _lifetime.Token);
            }
            catch (OperationCanceledException)
            {
                // 场景切换或退出 PlayMode 时正常取消。
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private void ClearLootLeases()
        {
            for (var i = _lootLeases.Count - 1; i >= 0; i--)
            {
                _lootLeases[i]?.Dispose();
            }

            _lootLeases.Clear();
        }

        private void OnDestroy()
        {
            _enemySubscription?.Dispose();
            _levelSubscription?.Dispose();
            _sceneSubscription?.Dispose();
            ClearLootLeases();
            _overlay?.Dispose();
            _lifetime?.Cancel();
            _lifetime?.Dispose();
        }
    }

    /// <summary>
    /// 运行时生成的结算与商店界面，不依赖具体场景预制体，避免每张地图重复维护 UI。
    /// </summary>
    internal sealed class ProgressionOverlay
    {
        private readonly IEventBus _events;
        private readonly IProgressionService _progression;
        private readonly IInventoryService _inventory;
        private readonly IEquipmentService _equipment;
        private readonly IInputModeService _inputMode;
        private readonly Action<RunNode> _loadNext;
        private readonly Action _openShopScene;
        private readonly Action _returnFromShop;
        private readonly GameObject _root;
        private readonly GameObject _walletPlate;
        private readonly Image _walletIcon;
        private readonly TMP_Text _wallet;
        private readonly TMP_Text _toast;
        private readonly GameObject _completion;
        private readonly TMP_Text _completionSummary;
        private readonly Button _shopButton;
        private readonly Button _nextButton;
        private readonly GameObject _shop;
        private readonly TMP_Text _shopStatus;
        private readonly GameObject _bossBar;
        private readonly Image _bossFill;
        private readonly TMP_Text _bossLabel;
        private IDisposable _modalLease;
        private Health _bossHealth;
        private float _toastHideAt;

        private ProgressionOverlay(
            IEventBus events,
            IProgressionService progression,
            IInventoryService inventory,
            IEquipmentService equipment,
            IInputModeService inputMode,
            Action<RunNode> loadNext,
            Action openShopScene,
            Action returnFromShop)
        {
            _events = events;
            _progression = progression;
            _inventory = inventory;
            _equipment = equipment;
            _inputMode = inputMode;
            _loadNext = loadNext;
            _openShopScene = openShopScene;
            _returnFromShop = returnFromShop;

            _root = new GameObject("[ProgressionOverlay]");
            UnityEngine.Object.DontDestroyOnLoad(_root);
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 250;
            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            _root.AddComponent<GraphicRaycaster>();

            // 玩家状态面板占据左上角 48~166 像素，金币放到其下方，避免与生命条和生命数字重叠。
            _walletPlate = CreatePanel(_root.transform, "[CurrencyPlate]");
            SetRect(_walletPlate.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -174f), new Vector2(224f, 46f));
            _walletPlate.GetComponent<Image>().color = new Color32(10, 20, 32, 220);

            var iconObject = new GameObject("CurrencyIcon");
            iconObject.transform.SetParent(_walletPlate.transform, false);
            _walletIcon = iconObject.AddComponent<Image>();
            _walletIcon.sprite = Resources.Load<Sprite>("UI/coin");
            _walletIcon.color = Color.white;
            _walletIcon.preserveAspect = true;
            SetRect(_walletIcon.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(10f, 0f), new Vector2(30f, 30f));

            _wallet = CreateLabel(_walletPlate.transform, "金币 0150", 22, TextAlignmentOptions.MidlineLeft);
            SetRect(_wallet.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, .5f), new Vector2(50f, 0f), new Vector2(-58f, 0f));
            _wallet.color = new Color32(255, 214, 120, 255);

            _toast = CreateLabel(_root.transform, string.Empty, 22, TextAlignmentOptions.Center);
            SetRect(_toast.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -250f), new Vector2(620f, 52f));
            _toast.color = new Color32(120, 235, 235, 255);

            _completion = CreatePanel(_root.transform, "[MissionResult]");
            SetRect(_completion.GetComponent<RectTransform>(), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(1000f, 700f));
            var completionTitle = CreateLabel(_completion.transform, "任务完成", 34, TextAlignmentOptions.Top);
            // 标题固定在弹窗顶部，给关卡名称和通关信息留出清晰的垂直间距。
            SetRect(completionTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -36f), new Vector2(0f, 48f));
            _completionSummary = CreateLabel(_completion.transform, string.Empty, 26, TextAlignmentOptions.Center);
            _completionSummary.textWrappingMode = TextWrappingModes.Normal;
            SetRect(_completionSummary.rectTransform, new Vector2(.07f, .31f), new Vector2(.93f, .77f), new Vector2(.5f, .54f), Vector2.zero, Vector2.zero);
            _shopButton = CreateButton(_completion.transform, "打开商店", 24);
            SetRect(_shopButton.GetComponent<RectTransform>(), new Vector2(.08f, .10f), new Vector2(.46f, .25f), new Vector2(.27f, .175f), Vector2.zero, Vector2.zero);
            _nextButton = CreateButton(_completion.transform, "下一关", 24);
            SetRect(_nextButton.GetComponent<RectTransform>(), new Vector2(.54f, .10f), new Vector2(.92f, .25f), new Vector2(.73f, .175f), Vector2.zero, Vector2.zero);
            _shopButton.onClick.AddListener(ShowShop);
            _nextButton.onClick.AddListener(LoadNext);

            _shop = CreatePanel(_root.transform, "[Shop]");
            SetRect(_shop.GetComponent<RectTransform>(), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(860f, 650f));
            var shopTitle = CreateLabel(_shop.transform, "战地商店", 32, TextAlignmentOptions.Top);
            SetRect(shopTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -50f), new Vector2(0f, 50f));
            _shopStatus = CreateLabel(_shop.transform, string.Empty, 18, TextAlignmentOptions.Center);
            SetRect(_shopStatus.rectTransform, new Vector2(.08f, .08f), new Vector2(.92f, .18f), new Vector2(.5f, .13f), Vector2.zero, Vector2.zero);
            var close = CreateButton(_shop.transform, "返回", 20);
            SetRect(close.GetComponent<RectTransform>(), new Vector2(.32f, .18f), new Vector2(.68f, .28f), new Vector2(.5f, .23f), Vector2.zero, Vector2.zero);
            close.onClick.AddListener(HideShop);

            _bossBar = CreatePanel(_root.transform, "[BossHealth]");
            SetRect(_bossBar.GetComponent<RectTransform>(), new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -116f), new Vector2(650f, 72f));
            var bossBackground = _bossBar.GetComponent<Image>();
            bossBackground.color = new Color32(24, 14, 34, 244);
            _bossLabel = CreateLabel(_bossBar.transform, "首领 · 核心熔炉", 18, TextAlignmentOptions.Top);
            SetRect(_bossLabel.rectTransform, new Vector2(.04f, .50f), new Vector2(.96f, .96f), new Vector2(.5f, 1f), Vector2.zero, Vector2.zero);
            var bossFillRoot = new GameObject("BossFill");
            bossFillRoot.transform.SetParent(_bossBar.transform, false);
            var bossFillRect = bossFillRoot.AddComponent<RectTransform>();
            SetRect(bossFillRect, new Vector2(.04f, .16f), new Vector2(.96f, .43f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            var bossFillBackground = bossFillRoot.AddComponent<Image>();
            bossFillBackground.color = new Color32(62, 33, 76, 255);
            var fill = new GameObject("Current");
            fill.transform.SetParent(bossFillRoot.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            SetRect(fillRect, Vector2.zero, Vector2.one, new Vector2(0f, .5f), Vector2.zero, Vector2.zero);
            _bossFill = fill.AddComponent<Image>();
            _bossFill.color = new Color32(255, 112, 205, 255);
            _bossFill.type = Image.Type.Filled;
            _bossFill.fillMethod = Image.FillMethod.Horizontal;
            _bossFill.fillOrigin = 0;

            _events.Subscribe<CurrencyChangedEvent>(message => RenderWallet(_progression.Snapshot));
            _events.Subscribe<EntityDamagedEvent>(OnEntityDamaged);
            HideAllPanels();
            _bossBar.SetActive(false);
        }

        public static ProgressionOverlay Create(
            IEventBus events,
            IProgressionService progression,
            IInventoryService inventory,
            IEquipmentService equipment,
            IInputModeService inputMode,
            Action<RunNode> loadNext,
            Action openShopScene,
            Action returnFromShop)
        {
            return new ProgressionOverlay(
                events,
                progression,
                inventory,
                equipment,
                inputMode,
                loadNext,
                openShopScene,
                returnFromShop);
        }

        public void RenderWallet(RunProgressSnapshot snapshot)
        {
            _wallet.text = $"金币  {snapshot.Coins:0000}";
        }

        public void Tick()
        {
            SyncWalletVisibility();
            if (_toast.gameObject.activeSelf && Time.unscaledTime >= _toastHideAt)
            {
                _toast.text = string.Empty;
                _toast.gameObject.SetActive(false);
            }

            if (_bossHealth == null)
            {
                RefreshBossBar();
            }
            else
            {
                RenderBossBar();
            }
        }

        public void ShowCombatToast(string title, string subtitle)
        {
            _toast.text = $"{title}   //   {subtitle}";
            _toast.gameObject.SetActive(true);
            _toastHideAt = Time.unscaledTime + 4f;
        }

        public void ShowLevelBanner(RunNode node)
        {
            RenderWallet(_progression.Snapshot);
            ShowCombatToast(node.DisplayName, node.IsBoss ? "首领节点已激活" : "战区已载入");
        }

        public void ShowCompletion(
            string levelId,
            float elapsed,
            string levelDisplayName,
            string rewardSummary,
            RunNode nextNode)
        {
            AcquireModal();
            _completion.SetActive(true);
            _shop.SetActive(false);
            _completionSummary.text =
                $"{levelDisplayName}\n通关用时  {elapsed:0.0}s\n\n" +
                $"战利品已写入仓库：\n{(string.IsNullOrWhiteSpace(rewardSummary) ? "无" : rewardSummary)}\n\n" +
                $"下一节点：{(string.IsNullOrWhiteSpace(nextNode.LevelId) ? "本轮完成" : nextNode.DisplayName)}";
            _nextButton.interactable = !string.IsNullOrWhiteSpace(nextNode.ScenePath);
            RenderWallet(_progression.Snapshot);
        }

        /// <summary>打开仓库、装备或任务页面时隐藏独立 HUD 钱包，避免遮挡页面内容。</summary>
        private void SyncWalletVisibility()
        {
            var root = UnityEngine.Object.FindFirstObjectByType<GameUIRootView>();
            var menuOpen = root != null &&
                           root.MenuNavigationRoot != null &&
                           root.MenuNavigationRoot.activeSelf;
            if (_walletPlate != null)
            {
                _walletPlate.SetActive(!menuOpen);
            }
        }

        public void RefreshBossBar()
        {
            if (_bossBar == null)
            {
                return;
            }

            _bossHealth = null;
            var identities = UnityEngine.Object.FindObjectsByType<EnemyIdentity>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (var identity in identities)
            {
                if (identity == null ||
                    identity.ArchetypeId.IndexOf("boss", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                var health = identity.GetComponentInParent<Health>();
                if (health != null && health.IsAlive)
                {
                    _bossHealth = health;
                    _bossLabel.text = $"核心熔炉 · {identity.ArchetypeId}";
                    break;
                }
            }

            _bossBar.SetActive(_bossHealth != null);
            RenderBossBar();
        }

        private void OnEntityDamaged(EntityDamagedEvent message)
        {
            if (message.Health != _bossHealth)
            {
                return;
            }

            RenderBossBar();
        }

        private void RenderBossBar()
        {
            if (_bossFill == null || _bossHealth == null)
            {
                return;
            }

            _bossFill.fillAmount = Mathf.Clamp01(
                _bossHealth.CurrentHealth / Mathf.Max(1f, _bossHealth.MaxHealth));
            var ratio = _bossFill.fillAmount;
            var phaseLabel = ratio <= .33f
                ? "过载阶段 · 3"
                : ratio <= .66f
                    ? "系统突破 · 2"
                    : "核心熔炉 · 1";
            _bossLabel.text = $"{phaseLabel} · Rusk 原型机";
            if (!_bossHealth.IsAlive)
            {
                _bossBar.SetActive(false);
            }
        }

        public void HideAllPanels()
        {
            ReleaseModal();
            _completion.SetActive(false);
            _shop.SetActive(false);
        }

        private void ShowShop()
        {
            _openShopScene?.Invoke();
        }

        /// <summary>商店场景 NPC 对话完成后打开商店内容面板。</summary>
        public void ShowShopFromNpc()
        {
            AcquireModal();
            _completion.SetActive(false);
            _shop.SetActive(true);
            _shopStatus.text = $"金币  {_progression.Snapshot.Coins:0000}  // 选择装备，购买后自动放入背包并穿戴。";
            var oldRows = _shop.transform.Find("Rows");
            if (oldRows != null)
            {
                UnityEngine.Object.Destroy(oldRows.gameObject);
            }

            var rows = new GameObject("Rows");
            rows.transform.SetParent(_shop.transform, false);
            var rect = rows.AddComponent<RectTransform>();
            SetRect(rect, new Vector2(.08f, .30f), new Vector2(.92f, .84f), new Vector2(.5f, .57f), Vector2.zero, Vector2.zero);
            var layout = rows.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;
            var count = Mathf.Min(5, _equipment.Catalog.Count);
            for (var i = 0; i < count; i++)
            {
                var definition = _equipment.Catalog[i];
                var cost = 120 + (int)definition.Rarity * 90;
                var ownsItem = _inventory.GetTotalQuantity(definition.ItemId) > 0;
                var canStore = CanStore(definition.ItemId, 1);
                var label = ownsItem
                    ? $"{definition.DisplayName}   已拥有"
                    : canStore
                        ? $"{definition.DisplayName}   金币 {cost}    [{definition.Rarity}]"
                        : $"{definition.DisplayName}   背包无可用空间";
                var button = CreateButton(rows.transform, label, 20);
                button.GetComponent<Image>().color = GetShopButtonColor(definition.Rarity);
                button.interactable = !ownsItem &&
                                      canStore &&
                                      _progression.Snapshot.Coins >= cost;
                var buttonLabel = button.GetComponentInChildren<TMP_Text>();
                if (buttonLabel != null)
                {
                    buttonLabel.fontStyle = FontStyles.Bold;
                }
                button.onClick.AddListener(() => Buy(definition, cost));
            }
        }

        private void Buy(EquipmentItemDefinition definition, int cost)
        {
            if (_inventory.GetTotalQuantity(definition.ItemId) > 0)
            {
                _shopStatus.text = "该装备已经拥有，不能重复购买。";
                return;
            }

            if (!CanStore(definition.ItemId, 1))
            {
                _shopStatus.text = "背包没有可用空间，或该物品已达到堆叠上限。";
                return;
            }

            if (!_progression.TrySpendCoins(cost, $"购买：{definition.DisplayName}"))
            {
                _shopStatus.text = "金币不足。";
                return;
            }

            if (!_inventory.TryAdd(definition.ItemId, 1))
            {
                _progression.AddCoins(cost, "背包不足退款");
                _shopStatus.text = "背包已满，金币已退回。";
                return;
            }

            _equipment.Equip(definition.ItemId, definition.GetDefaultSlot());
            _shopStatus.text = $"已购买并装备：{definition.DisplayName}  // 余额 {_progression.Snapshot.Coins:0000}";
            RenderWallet(_progression.Snapshot);
        }

        private void HideShop()
        {
            _returnFromShop?.Invoke();
        }

        private void LoadNext()
        {
            ReleaseModal();
            if (_progression.Snapshot.CurrentNodeIndex + 1 < _progression.Nodes.Count)
            {
                _loadNext(_progression.Nodes[_progression.Snapshot.CurrentNodeIndex + 1]);
            }
        }

        /// <summary>打开结算或商店时申请模态输入，释放后恢复角色与镜头控制。</summary>
        private void AcquireModal()
        {
            _modalLease ??= _inputMode?.AcquireModal("ProgressionOverlay");
        }

        private void ReleaseModal()
        {
            _modalLease?.Dispose();
            _modalLease = null;
        }

        /// <summary>预检查物品是否有足够堆叠容量，避免把所有失败都误报成背包已满。</summary>
        private bool CanStore(string itemId, int quantity)
        {
            if (quantity <= 0 ||
                !_inventory.TryGetDefinition(itemId, out var definition) ||
                definition == null)
            {
                return false;
            }

            var remaining = quantity;
            foreach (var slot in _inventory.Snapshot.Slots)
            {
                if (slot.IsEmpty)
                {
                    remaining -= definition.MaxStack;
                }
                else if (string.Equals(slot.ItemId, itemId, StringComparison.Ordinal))
                {
                    remaining -= Math.Max(0, definition.MaxStack - slot.Quantity);
                }

                if (remaining <= 0)
                {
                    return true;
                }
            }

            return false;
        }

        public void Dispose()
        {
            ReleaseModal();
            UnityEngine.Object.Destroy(_root);
        }

        private static GameObject CreatePanel(Transform parent, string name)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var image = panel.AddComponent<Image>();
            image.color = new Color32(9, 16, 29, 248);
            return panel;
        }

        private static TMP_Text CreateLabel(Transform parent, string text, float size, TextAlignmentOptions alignment)
        {
            var label = new GameObject("Label");
            label.transform.SetParent(parent, false);
            var tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = alignment;
            tmp.color = new Color32(220, 235, 242, 255);
            return tmp;
        }

        private static Button CreateButton(Transform parent, string text, float size)
        {
            var buttonRoot = new GameObject("Button");
            buttonRoot.transform.SetParent(parent, false);
            var image = buttonRoot.AddComponent<Image>();
            image.color = new Color32(24, 62, 78, 255);
            var button = buttonRoot.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color32(45, 128, 140, 255);
            colors.pressedColor = new Color32(100, 194, 190, 255);
            button.colors = colors;
            var label = CreateLabel(buttonRoot.transform, text, size, TextAlignmentOptions.Center);
            SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            return button;
        }

        private static Color GetShopButtonColor(EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Rare => new Color32(28, 76, 120, 255),
                EquipmentRarity.Elite => new Color32(73, 45, 112, 255),
                EquipmentRarity.Epic => new Color32(112, 45, 91, 255),
                EquipmentRarity.Legendary => new Color32(132, 74, 24, 255),
                _ => new Color32(30, 67, 78, 255)
            };
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
