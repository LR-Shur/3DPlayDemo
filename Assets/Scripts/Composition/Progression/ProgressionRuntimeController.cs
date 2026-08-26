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
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Train.Composition.Progression
{
    /// <summary>
    /// 鎶婃垬鏂椾簨浠舵帴鍒版帀钀姐€侀噾甯併€佺粨绠楅潰鏉垮拰鍟嗗簵銆?
    /// 杩欐槸绗竴鐗堝晢涓氬惊鐜殑缁勫悎灞傞€傞厤鍣紝棰嗗煙瑙勫垯浠嶇暀鍦ㄦ湇鍔′腑銆?
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
        private BuildLootResolver _lootResolver;
        private IAssetService _assets;
        private IEventBus _events;
        private ILubanConfigService _luban;
        private IDisposable _enemySubscription;
        private IDisposable _levelSubscription;
        private IDisposable _sceneSubscription;
        private CancellationTokenSource _lifetime;
        private ProgressionOverlay _overlay;
        private string _returnSceneLocation;
        private bool _shopPanelOpenedFromNpc;
        private GameObject _levelPortal;

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
            _lootResolver = new BuildLootResolver(
                _equipment.Catalog,
                _inventory.Catalog);
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
            _progression.AddCoins(coins, $"Enemy defeated: {message.EnemyArchetypeId}");
            _overlay?.ShowCombatToast($"+{coins} 金币", "敌人已击败");
            SpawnLoot(message);
        }

        private void OnLevelCompleted(LevelCompletedEvent message)
        {
            if (_progression == null || !_rewardedLevels.Add(message.LevelId))
            {
                return;
            }

            if (!_progression.TryGetNode(message.LevelId, out var completedNode))
            {
                return;
            }
            _progression.CompleteLevel(message.LevelId, out var nextNode);
            var rewards = ResolveLevelRewards(message.LevelId);
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
            CreateLevelPortal();
        }

        private void OnSceneLoaded(LevelSceneLoadedEvent message)
        {
            DestroyLevelPortal();
            ClearLootLeases();
            _overlay?.HideAllPanels();
            if (_progression != null && _progression.TryGetNode(message.LevelId, out var node))
            {
                _overlay?.ShowLevelBanner(node);
            }

            _overlay?.RefreshBossBar();
        }

        /// <summary>鐢卞晢搴?NPC 鐨勫璇濆畬鎴愪簨浠惰皟鐢紝鎵撳紑甯搁┗鍟嗗簵鐣岄潰銆?/summary>
        public void OpenShopFromNpc()
        {
            TryInitialize();
            _shopPanelOpenedFromNpc = true;
            _overlay?.ShowShopFromNpc();
        }

        /// <summary>鐢辨垬鏂楀満鏅紶閫侀棬杩涘叆鍟嗗簵鍦烘櫙銆?/summary>
        public void EnterShopFromPortal()
        {
            _shopPanelOpenedFromNpc = false;
            _returnSceneLocation = ResolveCurrentSceneAddress();
            _overlay?.HideAllPanels();
            SceneManager.LoadSceneAsync(AssetLocations.ShopScene, LoadSceneMode.Single);
        }

        /// <summary>鐢卞晢搴椾紶閫侀棬杩涘叆娴佺▼琛ㄤ腑閰嶇疆鐨勪笅涓€鍏炽€?/summary>
        public void EnterNextLevelFromPortal()
        {
            if (_progression == null)
            {
                TryInitialize();
            }

            var nextIndex = _progression == null
                ? -1
                : _progression.Snapshot.CurrentNodeIndex + 1;
            if (_progression == null || nextIndex < 0 || nextIndex >= _progression.Nodes.Count)
            {
                return;
            }

            LoadNextNode(_progression.Nodes[nextIndex]);
        }

        /// <summary>浠庣粨绠楅潰鏉胯繘鍏ョ嫭绔嬪晢搴楀満鏅紝骞惰浣忚繑鍥炵殑鎴樻枟鍦烘櫙銆?/summary>
        private void OpenShopScene()
        {
            _shopPanelOpenedFromNpc = false;
            _returnSceneLocation = ResolveCurrentSceneAddress();
            _overlay?.HideAllPanels();
            SceneManager.LoadSceneAsync(AssetLocations.ShopScene, LoadSceneMode.Single);
        }

        /// <summary>鍏抽棴鍟嗗簵鍚庤繑鍥炶繘鍏ュ晢搴楀墠鐨勬垬鏂楀満鏅€?/summary>
        private async void ReturnFromShop()
        {
            if (_shopPanelOpenedFromNpc)
            {
                _shopPanelOpenedFromNpc = false;
                _overlay?.HideShopPanel();
                return;
            }

            var location = string.IsNullOrWhiteSpace(_returnSceneLocation)
                ? ResolveCurrentSceneAddress()
                : _returnSceneLocation;
            _overlay?.HideAllPanels();
            if (_assets == null)
            {
                Debug.LogError("返回战斗关卡需要已初始化的资源服务。", this);
                return;
            }

            await _assets.LoadSceneAsync(
                location,
                LoadSceneMode.Single,
                true,
                _lifetime.Token);

            var level = FindFirstObjectByType<LevelRuntimeController>();
            if (level != null && level.IsWaitingForStart)
            {
                await level.PrepareAsync(_lifetime.Token);
                level.StartLevel();
            }
        }

        private string ResolveCurrentSceneAddress()
        {
            if (_progression != null &&
                _progression.TryGetNode(
                    _progression.Snapshot.CurrentLevelId,
                    out var node) &&
                !string.IsNullOrWhiteSpace(node.ScenePath))
            {
                return node.ScenePath;
            }

            return AssetAddresses.CombatArenaScene;
        }

        /// <summary>浠庡綋鍓嶅叧鍗?SO 璇诲彇濂栧姳銆?/summary>
        private List<RewardGrant> ResolveLevelRewards(string levelId)
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

            return result;
        }

        /// <summary>閫氳繃鐗╁搧鐩綍鎶婄ǔ瀹?ID 杞垚鐜╁鍙鐨勪腑鏂囧悕绉般€?/summary>
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
                "healing_canister" => "应急治疗罐",
                "upgrade_module" => "高能升级模组",
                "city_token" => "都市代币",
                "thunder_ring" => "雷环",
                "thunder_blade" => "雷鸣刃",
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
            if (_assets == null ||
                !_assets.IsInitialized ||
                _lootResolver == null ||
                _inventory == null)
            {
                return;
            }

            var loot = _lootResolver.Resolve(
                message.EnemyArchetypeId,
                message.EnemyInstanceId);
            if (!loot.IsValid ||
                !_inventory.TryGetDefinition(loot.ItemId, out var definition) ||
                definition == null)
            {
                return;
            }

            var position = ResolveEnemyPosition(message.EnemyInstanceId);
            SpawnLootAsync(loot, position, message.EnemyInstanceId, _lifetime.Token);
        }

        private async void SpawnLootAsync(
            BuildLootResult loot,
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
                    $"loot_{instanceId}_{loot.ItemId}",
                    loot.ItemId,
                    loot.Quantity,
                    loot.IsEquipment ? 35 : 25);
                _lootLeases.Add(lease);
            }
            catch (OperationCanceledException)
            {
                // 鍦烘櫙鍒囨崲鎴栭€€鍑烘椂涓嶅啀鐢熸垚鏃у満鏅帀钀姐€?
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

        /// <summary>閫氬叧鍚庡湪鐜╁鍓嶆柟鍒涘缓鍓嶅線鍟嗗簵鐨勪紶閫侀棬銆?/summary>
        private void CreateLevelPortal()
        {
            DestroyLevelPortal();
            var player = GameObject.FindGameObjectWithTag("Player");
            var position = player != null
                ? player.transform.position + player.transform.forward * 2.5f
                : Vector3.zero;
            var portal = new GameObject("LevelPortal");
            portal.transform.position = position;
            portal.transform.rotation = Quaternion.identity;
            var collider = portal.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 1.2f;
            var worldPortal = portal.AddComponent<WorldPortal>();
            worldPortal.Configure(
                "portal_to_shop",
                "商店传送门",
                "前往商店",
                EnterShopFromPortal);
            _levelPortal = portal;
        }

        /// <summary>鍦烘櫙鍒囨崲鏃舵竻鐞嗘棫浼犻€侀棬銆?/summary>
        private void DestroyLevelPortal()
        {
            if (_levelPortal != null)
            {
                Destroy(_levelPortal);
                _levelPortal = null;
            }
        }

        /// <summary>閫氳繃缁熶竴璧勬簮鏈嶅姟鎸?YooAsset 鍦板潃鍔犺浇涓嬩竴鍏筹紝娴佺▼灞備笉鐩存帴璋冪敤 SceneManager銆?/summary>
        private async void LoadSceneByAddressAsync(string address)
        {
            try
            {
                if (_assets == null)
                {
                    throw new InvalidOperationException("切换关卡需要已初始化的资源服务。");
                }

                await _assets.LoadSceneAsync(address, LoadSceneMode.Single, true, _lifetime.Token);
                await Task.Yield();
                var level = FindFirstObjectByType<LevelRuntimeController>();
                if (level != null)
                {
                    await level.PrepareAsync(_lifetime.Token);
                    if (level.IsWaitingForStart)
                    {
                        level.StartLevel();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 鍦烘櫙鍒囨崲鎴栭€€鍑?PlayMode 鏃舵甯稿彇娑堛€?
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
            DestroyLevelPortal();
            _lifetime?.Cancel();
            _lifetime?.Dispose();
        }
    }

    /// <summary>
    /// 杩愯鏃剁敓鎴愮殑缁撶畻涓庡晢搴楃晫闈紝涓嶄緷璧栧叿浣撳満鏅鍒朵綋锛岄伩鍏嶆瘡寮犲湴鍥鹃噸澶嶇淮鎶?UI銆?
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
        private readonly Button _completionCloseButton;
        private readonly GameObject _shop;
        private readonly TMP_Text _shopStatus;
        private readonly TMP_Text _shopBalance;
        private readonly TMP_Text _shopDetailName;
        private readonly TMP_Text _shopDetailDescription;
        private readonly TMP_Text _shopDetailStats;
        private readonly TMP_Text _shopDetailRarity;
        private readonly Button _shopAction;
        private readonly Button _shopSellAction;
        private readonly Image _shopDetailIcon;
        private readonly ShopDetailRefreshMotion _shopDetailRefresh;
        private readonly ShopOverlayMotion _shopMotion;
        private ItemDefinition _selectedShopItem;
        private int _selectedShopCost;
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

            // 鐜╁鐘舵€侀潰鏉垮崰鎹乏涓婅 48~166 鍍忕礌锛岄噾甯佹斁鍒板叾涓嬫柟锛岄伩鍏嶄笌鐢熷懡鏉″拰鐢熷懡鏁板瓧閲嶅彔銆?
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

            _wallet = CreateLabel(_walletPlate.transform, "金币 0000", 22, TextAlignmentOptions.MidlineLeft);
            SetRect(_wallet.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, .5f), new Vector2(50f, 0f), new Vector2(-58f, 0f));
            _wallet.color = new Color32(255, 214, 120, 255);

            _toast = CreateLabel(_root.transform, string.Empty, 22, TextAlignmentOptions.Center);
            SetRect(_toast.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -250f), new Vector2(620f, 52f));
            _toast.color = new Color32(120, 235, 235, 255);

            _completion = CreatePanel(_root.transform, "[MissionResult]");
            SetRect(_completion.GetComponent<RectTransform>(), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(1000f, 700f));
            var completionTitle = CreateLabel(_completion.transform, "任务完成", 34, TextAlignmentOptions.Top);
            // 鏍囬鍥哄畾鍦ㄥ脊绐楅《閮紝缁欏叧鍗″悕绉板拰閫氬叧淇℃伅鐣欏嚭娓呮櫚鐨勫瀭鐩撮棿璺濄€?
            SetRect(completionTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -36f), new Vector2(0f, 48f));
            _completionSummary = CreateLabel(_completion.transform, string.Empty, 26, TextAlignmentOptions.Center);
            _completionSummary.textWrappingMode = TextWrappingModes.Normal;
            SetRect(_completionSummary.rectTransform, new Vector2(.07f, .31f), new Vector2(.93f, .77f), new Vector2(.5f, .54f), Vector2.zero, Vector2.zero);
            _shopButton = CreateButton(_completion.transform, "打开商店", 24);
            SetRect(_shopButton.GetComponent<RectTransform>(), new Vector2(.08f, .10f), new Vector2(.46f, .25f), new Vector2(.27f, .175f), Vector2.zero, Vector2.zero);
            _nextButton = CreateButton(_completion.transform, "下一关", 24);
            SetRect(_nextButton.GetComponent<RectTransform>(), new Vector2(.54f, .10f), new Vector2(.92f, .25f), new Vector2(.73f, .175f), Vector2.zero, Vector2.zero);
            _shopButton.gameObject.SetActive(false);
            _nextButton.gameObject.SetActive(false);
            _completionCloseButton = CreateButton(_completion.transform, "关闭结算", 24);
            SetRect(_completionCloseButton.GetComponent<RectTransform>(), new Vector2(.30f, .10f), new Vector2(.70f, .25f), new Vector2(.5f, .175f), Vector2.zero, Vector2.zero);
            _completionCloseButton.onClick.AddListener(CloseCompletion);

            _shop = CreatePanel(_root.transform, "[Shop]");
            SetRect(_shop.GetComponent<RectTransform>(), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(1540f, 900f));
            _shop.GetComponent<Image>().color = new Color32(7, 13, 25, 250);
            _shop.AddComponent<CanvasGroup>();
            _shopMotion = _shop.AddComponent<ShopOverlayMotion>();
            _shopMotion.Configure(_shop.GetComponent<RectTransform>());
            var shopTopLine = CreatePanel(_shop.transform, "TopLine");
            SetRect(shopTopLine.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -86f), new Vector2(0f, 3f));
            shopTopLine.GetComponent<Image>().color = new Color32(75, 219, 226, 255);
            var shopTitle = CreateLabel(_shop.transform, "补给商店", 36, TextAlignmentOptions.MidlineLeft);
            SetRect(shopTitle.rectTransform, new Vector2(0f, 1f), new Vector2(.45f, 1f), new Vector2(0f, 1f), new Vector2(52f, -32f), new Vector2(0f, 50f));
            shopTitle.fontStyle = FontStyles.Bold;
            var shopSubtitle = CreateLabel(_shop.transform, "战斗补给 / 装备 / 消耗品", 17, TextAlignmentOptions.MidlineLeft);
            SetRect(shopSubtitle.rectTransform, new Vector2(0f, 1f), new Vector2(.55f, 1f), new Vector2(0f, 1f), new Vector2(54f, -69f), new Vector2(0f, 28f));
            shopSubtitle.color = new Color32(133, 164, 184, 255);
            var balancePlate = CreatePanel(_shop.transform, "BalancePlate");
            SetRect(balancePlate.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-150f, -28f), new Vector2(240f, 46f));
            balancePlate.GetComponent<Image>().color = new Color32(21, 38, 53, 255);
            _shopBalance = CreateLabel(balancePlate.transform, string.Empty, 21, TextAlignmentOptions.Center);
            SetRect(_shopBalance.rectTransform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            _shopBalance.color = new Color32(255, 216, 118, 255);

            var catalogPanel = CreatePanel(_shop.transform, "CatalogPanel");
            SetRect(catalogPanel.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(.64f, 1f), new Vector2(0f, .5f), new Vector2(42f, 24f), new Vector2(-30f, -172f));
            catalogPanel.GetComponent<Image>().color = new Color32(12, 24, 39, 235);
            var catalogTitle = CreateLabel(catalogPanel.transform, "商品目录", 20, TextAlignmentOptions.MidlineLeft);
            SetRect(catalogTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(22f, -18f), new Vector2(-44f, 32f));
            catalogTitle.color = new Color32(117, 231, 235, 255);
            var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportObject.transform.SetParent(catalogPanel.transform, false);
            var viewport = viewportObject.GetComponent<RectTransform>();
            SetRect(viewport, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), new Vector2(18f, -42f), new Vector2(-36f, -74f));
            viewportObject.GetComponent<Image>().color = new Color32(8, 16, 29, 120);
            viewportObject.GetComponent<Mask>().showMaskGraphic = false;
            var cardsObject = new GameObject("Cards", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            cardsObject.transform.SetParent(viewportObject.transform, false);
            var cardsRect = cardsObject.GetComponent<RectTransform>();
            SetRect(cardsRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -12f), new Vector2(-28f, 0f));
            var grid = cardsObject.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(252f, 154f);
            grid.spacing = new Vector2(14f, 14f);
            grid.padding = new RectOffset(14, 14, 14, 14);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperLeft;
            cardsObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = catalogPanel.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = cardsRect;
            scroll.vertical = true;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 34f;

            var detailPanel = CreatePanel(_shop.transform, "DetailPanel");
            SetRect(detailPanel.GetComponent<RectTransform>(), new Vector2(.64f, 0f), new Vector2(1f, 1f), new Vector2(0f, .5f), new Vector2(12f, 24f), new Vector2(-42f, -172f));
            detailPanel.GetComponent<Image>().color = new Color32(18, 33, 50, 245);
            var detailHeader = CreateLabel(detailPanel.transform, "商品详情", 20, TextAlignmentOptions.MidlineLeft);
            SetRect(detailHeader.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(28f, -18f), new Vector2(-56f, 32f));
            detailHeader.color = new Color32(117, 231, 235, 255);
            var iconPlate = CreatePanel(detailPanel.transform, "ItemIcon");
            SetRect(iconPlate.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -104f), new Vector2(100f, 100f));
            iconPlate.GetComponent<Image>().color = new Color32(30, 63, 80, 255);
            _shopDetailIcon = iconPlate.GetComponent<Image>();
            var iconPulse = iconPlate.AddComponent<GraphicPulse>();
            iconPulse.Configure(.92f, 1.05f, .8f);
            _shopDetailRefresh = iconPlate.AddComponent<ShopDetailRefreshMotion>();
            _shopDetailRefresh.Configure(iconPlate.GetComponent<RectTransform>());
            _shopDetailName = CreateLabel(detailPanel.transform, "选择商品", 28, TextAlignmentOptions.MidlineLeft);
            SetRect(_shopDetailName.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(150f, -78f), new Vector2(-178f, 42f));
            _shopDetailName.fontStyle = FontStyles.Bold;
            _shopDetailRarity = CreateLabel(detailPanel.transform, string.Empty, 16, TextAlignmentOptions.MidlineLeft);
            SetRect(_shopDetailRarity.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(150f, -111f), new Vector2(-178f, 28f));
            _shopDetailDescription = CreateLabel(detailPanel.transform, string.Empty, 18, TextAlignmentOptions.TopLeft);
            _shopDetailDescription.textWrappingMode = TextWrappingModes.Normal;
            SetRect(_shopDetailDescription.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(28f, -236f), new Vector2(-56f, 105f));
            _shopDetailDescription.color = new Color32(185, 202, 216, 255);
            _shopDetailStats = CreateLabel(detailPanel.transform, string.Empty, 17, TextAlignmentOptions.TopLeft);
            SetRect(_shopDetailStats.rectTransform, new Vector2(0f, 0f), new Vector2(1f, .36f), new Vector2(0f, 1f), new Vector2(28f, 0f), new Vector2(-56f, -16f));
            _shopDetailStats.color = new Color32(117, 231, 235, 255);
            _shopAction = CreateButton(detailPanel.transform, "购买", 19);
            SetRect(_shopAction.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(.5f, 0f), new Vector2(28f, 28f), new Vector2(-56f, 54f));
            _shopAction.GetComponent<Image>().color = new Color32(22, 128, 145, 255);
            _shopAction.onClick.AddListener(BuySelected);
            _shopSellAction = CreateButton(detailPanel.transform, "出售", 17);
            SetRect(_shopSellAction.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(28f, -32f), new Vector2(-42f, 44f));
            _shopSellAction.GetComponent<Image>().color = new Color32(82, 57, 99, 255);
            _shopSellAction.onClick.AddListener(SellSelected);
            _shopStatus = CreateLabel(_shop.transform, string.Empty, 18, TextAlignmentOptions.Center);
            SetRect(_shopStatus.rectTransform, new Vector2(.05f, 0f), new Vector2(.72f, 0f), new Vector2(.5f, 0f), new Vector2(0f, 32f), new Vector2(0f, 42f));
            _shopStatus.color = new Color32(140, 235, 220, 255);
            var close = CreateButton(_shop.transform, "关闭商店", 20);
            SetRect(close.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-42f, 28f), new Vector2(140f, 50f));
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
            _toast.text = $"{title}   ·   {subtitle}";
            _toast.gameObject.SetActive(true);
            _toastHideAt = Time.unscaledTime + 4f;
        }

        public void ShowLevelBanner(RunNode node)
        {
            RenderWallet(_progression.Snapshot);
            ShowCombatToast(node.DisplayName, node.IsBoss ? "首领已出现" : "战斗区域已加载");
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
            _shopMotion.HideImmediate();
            _shop.SetActive(false);
            _completionSummary.text = $"{levelDisplayName}\n通关用时：{elapsed:0.0} 秒\n\n获得奖励：\n{(string.IsNullOrWhiteSpace(rewardSummary) ? "无" : rewardSummary)}\n\n关闭本面板后，靠近传送门前往商店。";
            RenderWallet(_progression.Snapshot);
        }

        /// <summary>鎵撳紑浠撳簱銆佽澶囨垨浠诲姟椤甸潰鏃堕殣钘忕嫭绔?HUD 閽卞寘锛岄伩鍏嶉伄鎸￠〉闈㈠唴瀹广€?/summary>
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
                ? "危急阶段 / 3"
                : ratio <= .66f
                    ? "系统突破 / 2"
                    : "核心点火 / 1";
            _bossLabel.text = $"{phaseLabel} · 鲁斯克原型机";
            if (!_bossHealth.IsAlive)
            {
                _bossBar.SetActive(false);
            }
        }

        public void HideAllPanels()
        {
            ReleaseModal();
            _completion.SetActive(false);
            _shopMotion.HideImmediate();
            _shop.SetActive(false);
        }

        /// <summary>鍏抽棴 NPC 鎵撳紑鐨勫晢搴楅潰鏉匡紝浣嗕繚鐣欏綋鍓嶅満鏅€?/summary>
        public void HideShopPanel()
        {
            ReleaseModal();
            if (!_shop.activeSelf)
            {
                _shopMotion.HideImmediate();
                return;
            }

            _shopMotion.PlayClose(() => _shop.SetActive(false));
        }

        /// <summary>鍏抽棴鑳滃埄缁撶畻骞舵仮澶嶈鑹叉搷浣溿€?/summary>
        private void CloseCompletion()
        {
            _completion.SetActive(false);
            ReleaseModal();
        }

        private void ShowShop()
        {
            _openShopScene?.Invoke();
        }

        /// <summary>鍟嗗簵鍦烘櫙 NPC 瀵硅瘽瀹屾垚鍚庢墦寮€鍟嗗簵鍐呭闈㈡澘銆?/summary>
        public void ShowShopFromNpc()
        {
            ShowShopFromNpcFull();
        }

        private void ShowShopFromNpcFull()
        {
            AcquireModal();
            _completion.SetActive(false);
            _shop.SetActive(true);
            _shopMotion.PlayOpen();
            _shopBalance.text = $"金币  {_progression.Snapshot.Coins:0000}";
            _shopStatus.text = "选择商品查看详情，购买和出售会立即更新金币。";
            var cards = _shop.transform.Find("CatalogPanel/Viewport/Cards");
            if (cards == null)
            {
                return;
            }
            for (var i = cards.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(cards.GetChild(i).gameObject);
            }

            _selectedShopItem = null;
            _selectedShopCost = 0;
            RefreshShopDetail();
            foreach (var definition in _inventory.Catalog)
            {
                if (definition == null) continue;
                var cost = GetItemPrice(definition);
                CreateShopCard(definition, cost, cards);
            }
        }

        private void CreateShopCard(ItemDefinition definition, int cost, Transform parent)
        {
            var card = new GameObject($"ShopCard_{definition.ItemId}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            card.transform.SetParent(parent, false);
            var image = card.GetComponent<Image>();
            image.color = GetShopCardColor(definition.Rarity);
            var button = card.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color32(110, 231, 235, 255);
            colors.pressedColor = new Color32(160, 245, 238, 255);
            colors.selectedColor = new Color32(110, 231, 235, 255);
            button.colors = colors;
            button.onClick.AddListener(() => SelectShopItem(definition, cost));
            var buttonMotion = card.AddComponent<UiButtonMotion>();
            buttonMotion.Configure(1.012f, .978f);

            var accent = CreatePanel(card.transform, "Accent");
            SetRect(accent.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, .5f), Vector2.zero, new Vector2(5f, 0f));
            accent.GetComponent<Image>().color = GetRarityColor(definition.Rarity);
            var icon = CreatePanel(card.transform, "Icon");
            SetRect(icon.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(46f, 46f));
            icon.GetComponent<Image>().color = new Color32(12, 24, 39, 220);
            var iconText = CreateLabel(icon.transform, GetItemGlyph(definition.Category), 21, TextAlignmentOptions.Center);
            SetRect(iconText.rectTransform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            iconText.color = GetRarityColor(definition.Rarity);
            var name = CreateLabel(card.transform, definition.DisplayName, 18, TextAlignmentOptions.MidlineLeft);
            SetRect(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(78f, -18f), new Vector2(-90f, 34f));
            name.fontStyle = FontStyles.Bold;
            var rarity = CreateLabel(card.transform, $"{GetRarityName(definition.Rarity)}  ·  {GetCategoryName(definition.Category)}", 13, TextAlignmentOptions.MidlineLeft);
            SetRect(rarity.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(78f, -47f), new Vector2(-90f, 24f));
            rarity.color = new Color32(170, 194, 207, 255);
            var price = CreateLabel(card.transform, $"买入  {cost}  金币", 14, TextAlignmentOptions.MidlineLeft);
            SetRect(price.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(18f, 15f), new Vector2(-36f, 25f));
            price.color = new Color32(255, 216, 118, 255);
            var owned = CreateLabel(card.transform, $"持有  {_inventory.GetTotalQuantity(definition.ItemId)}", 13, TextAlignmentOptions.MidlineRight);
            SetRect(owned.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 15f), new Vector2(-116f, 25f));
            owned.color = new Color32(170, 194, 207, 255);
        }

        private void SelectShopItem(ItemDefinition definition, int cost)
        {
            _selectedShopItem = definition;
            _selectedShopCost = cost;
            RefreshShopDetail();
            _shopStatus.text = "选择商品查看详情，购买和出售会立即更新金币。";
            _shopDetailRefresh.Play();
        }

        private void RefreshShopDetail()
        {
            if (_selectedShopItem == null)
            {
                _shopDetailName.text = "选择商品";
                _shopDetailRarity.text = "商品信息";
                _shopDetailDescription.text = "从左侧目录选择补给、材料或装备。\n\n此处显示用途、价格和当前持有数量。";
                _shopDetailStats.text = string.Empty;
                _shopDetailIcon.color = new Color32(30, 63, 80, 255);
                _shopAction.interactable = false;
                _shopSellAction.interactable = false;
                return;
            }

            _shopDetailName.text = _selectedShopItem.DisplayName;
            _shopDetailRarity.text = $"{GetRarityName(_selectedShopItem.Rarity)} / {GetCategoryName(_selectedShopItem.Category)}";
            _shopDetailRarity.color = GetRarityColor(_selectedShopItem.Rarity);
            _shopDetailDescription.text = _selectedShopItem.Category == ItemCategory.Equipment &&
                                          _equipment.TryGetDefinition(_selectedShopItem.ItemId, out var equipmentDefinition)
                ? EquipmentDescriptionFormatter.Format(equipmentDefinition, _equipment)
                : string.IsNullOrWhiteSpace(_selectedShopItem.Description)
                    ? "战斗补给。"
                    : _selectedShopItem.Description;
            _shopDetailStats.text = $"购买价格    {_selectedShopCost} 金币\n出售价格    {_selectedShopCost / 2} 金币\n持有数量    {_inventory.GetTotalQuantity(_selectedShopItem.ItemId)}\n\n{(_selectedShopItem.MaxStack > 1 ? "可叠加" : "不可叠加")}";
            _shopDetailIcon.color = GetShopCardColor(_selectedShopItem.Rarity);
            _shopAction.interactable = CanStore(_selectedShopItem.ItemId, 1) && _progression.Snapshot.Coins >= _selectedShopCost;
            _shopSellAction.interactable = _inventory.GetTotalQuantity(_selectedShopItem.ItemId) > 0 && !IsEquipped(_selectedShopItem.ItemId);
        }

        private void BuySelected()
        {
            if (_selectedShopItem == null) return;
            Buy(_selectedShopItem, _selectedShopCost);
            _shopBalance.text = $"金币  {_progression.Snapshot.Coins:0000}";
            RefreshShopDetail();
        }

        private void SellSelected()
        {
            if (_selectedShopItem == null) return;
            Sell(_selectedShopItem, _selectedShopCost / 2);
            _shopBalance.text = $"金币  {_progression.Snapshot.Coins:0000}";
            RefreshShopDetail();
        }

        private void Buy(ItemDefinition definition, int cost)
        {
            if (!CanStore(definition.ItemId, 1) || !_progression.TrySpendCoins(cost, $"购买：{definition.DisplayName}"))
            {
                _shopStatus.text = "购买失败，金币已退回。";
                return;
            }
            if (!_inventory.TryAdd(definition.ItemId, 1))
            {
                _progression.AddCoins(cost, "购买失败退款");
                _shopStatus.text = "购买失败，金币已退回。";
                return;
            }
            _shopStatus.text = $"已购买：{definition.DisplayName}。";
            RenderWallet(_progression.Snapshot);
        }

        private void Sell(ItemDefinition definition, int value)
        {
            if (IsEquipped(definition.ItemId) || !_inventory.TryRemove(definition.ItemId, 1))
            {
                _shopStatus.text = "已装备物品不能出售，或持有数量不足。";
                return;
            }
            _progression.AddCoins(value, $"出售：{definition.DisplayName}");
            _shopStatus.text = $"已出售：{definition.DisplayName}，获得 {value} 金币。";
            RenderWallet(_progression.Snapshot);
        }

        private bool IsEquipped(string itemId)
        {
            foreach (var slot in _equipment.Snapshot.Slots)
            {
                if (slot.Item != null && string.Equals(slot.Item.ItemId, itemId, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static int GetItemPrice(ItemDefinition definition)
        {
            var seed = 0;
            foreach (var character in definition.ItemId)
            {
                seed = (seed * 31 + character) & 0x7fffffff;
            }
            return 60 + (int)definition.Rarity * 80 + seed % 140;
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

        /// <summary>鎵撳紑缁撶畻鎴栧晢搴楁椂鐢宠妯℃€佽緭鍏ワ紝閲婃斁鍚庢仮澶嶈鑹蹭笌闀滃ご鎺у埗銆?/summary>
        private void AcquireModal()
        {
            _modalLease ??= _inputMode?.AcquireModal("ProgressionOverlay");
        }

        private void ReleaseModal()
        {
            _modalLease?.Dispose();
            _modalLease = null;
        }

        /// <summary>棰勬鏌ョ墿鍝佹槸鍚︽湁瓒冲鍫嗗彔瀹归噺锛岄伩鍏嶆妸鎵€鏈夊け璐ラ兘璇姤鎴愯儗鍖呭凡婊°€?/summary>
        private bool CanStore(string itemId, int quantity)
        {
            return quantity > 0 &&
                   _inventory.TryGetDefinition(itemId, out var definition) &&
                   definition != null;
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
            tmp.font = TMP_Settings.defaultFontAsset;
            tmp.fontSize = size;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
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
            var buttonMotion = buttonRoot.AddComponent<UiButtonMotion>();
            buttonMotion.Configure();
            var label = CreateLabel(buttonRoot.transform, text, size, TextAlignmentOptions.Center);
            SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            return button;
        }

        private static Color GetShopItemColor(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Rare => new Color32(28, 76, 120, 255),
                ItemRarity.Elite => new Color32(73, 45, 112, 255),
                ItemRarity.Epic => new Color32(112, 45, 91, 255),
                ItemRarity.Legendary => new Color32(132, 74, 24, 255),
                _ => new Color32(30, 67, 78, 255)
            };
        }

        private static Color GetShopCardColor(ItemRarity rarity)
        {
            var baseColor = GetShopItemColor(rarity);
            return new Color(baseColor.r * .7f, baseColor.g * .7f, baseColor.b * .7f, .98f);
        }

        private static Color GetRarityColor(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Rare => new Color32(76, 181, 255, 255),
                ItemRarity.Elite => new Color32(177, 117, 255, 255),
                ItemRarity.Epic => new Color32(255, 105, 200, 255),
                ItemRarity.Legendary => new Color32(255, 188, 78, 255),
                _ => new Color32(180, 202, 214, 255)
            };
        }

        private static string GetRarityName(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Rare: return "稀有";
                case ItemRarity.Elite: return "精英";
                case ItemRarity.Epic: return "史诗";
                case ItemRarity.Legendary: return "传说";
                default: return "普通";
            }
        }

        private static string GetCategoryName(ItemCategory category)
        {
            return category switch
            {
                ItemCategory.Consumable => "消耗品",
                ItemCategory.Equipment => "装备",
                ItemCategory.Material => "材料",
                ItemCategory.Quest => "任务物品",
                _ => "其他"
            };
        }

        private static string GetItemGlyph(ItemCategory category)
        {
            return category switch
            {
                ItemCategory.Consumable => "+",
                ItemCategory.Equipment => "E",
                ItemCategory.Material => "M",
                ItemCategory.Quest => "!",
                _ => "-"
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

    /// <summary>鍟嗗簵闈㈡澘鐨勬墦寮€涓庡叧闂繃娓★紝浣跨敤 unscaled time 鍏煎鏆傚仠涓庡満鏅垏鎹€銆?/summary>
    internal sealed class ShopOverlayMotion : MonoBehaviour
    {
        private const float OpenDuration = .26f;
        private const float CloseDuration = .12f;
        private const float OpenOffset = 14f;
        private const float CloseOffset = 8f;

        private RectTransform _rect;
        private CanvasGroup _group;
        private Vector3 _restPosition;
        private Vector3 _restScale;
        private Vector3 _fromPosition;
        private Vector3 _toPosition;
        private Vector3 _fromScale;
        private Vector3 _toScale;
        private float _fromAlpha;
        private float _toAlpha;
        private float _startedAt;
        private float _duration;
        private bool _animating;
        private bool _closing;
        private Action _onClosed;

        public void Configure(RectTransform rect)
        {
            _rect = rect;
            _group = GetComponent<CanvasGroup>();
            _restPosition = rect.localPosition;
            _restScale = rect.localScale;
            HideImmediate();
        }

        public void PlayOpen()
        {
            if (_rect == null || _group == null)
            {
                return;
            }

            var continueFromCurrent = _animating;
            _animating = true;
            _closing = false;
            _onClosed = null;
            _startedAt = Time.unscaledTime;
            _duration = OpenDuration;
            _fromPosition = continueFromCurrent
                ? _rect.localPosition
                : _restPosition + Vector3.down * OpenOffset;
            _fromScale = continueFromCurrent
                ? _rect.localScale
                : _restScale * .965f;
            _fromAlpha = continueFromCurrent ? _group.alpha : 0f;
            _toPosition = _restPosition;
            _toScale = _restScale;
            _toAlpha = 1f;
            _group.interactable = true;
            _group.blocksRaycasts = true;
        }

        public void PlayClose(Action onClosed)
        {
            if (_rect == null || _group == null)
            {
                onClosed?.Invoke();
                return;
            }

            if (_animating && _closing)
            {
                _onClosed = onClosed;
                return;
            }

            _animating = true;
            _closing = true;
            _onClosed = onClosed;
            _startedAt = Time.unscaledTime;
            _duration = CloseDuration;
            _fromPosition = _rect.localPosition;
            _fromScale = _rect.localScale;
            _fromAlpha = _group.alpha;
            _toPosition = _restPosition + Vector3.down * CloseOffset;
            _toScale = _restScale * .985f;
            _toAlpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            if (_fromAlpha <= .001f)
            {
                CompleteClose();
            }
        }

        public void HideImmediate()
        {
            _animating = false;
            _closing = false;
            _onClosed = null;
            if (_rect != null)
            {
                _rect.localPosition = _restPosition;
                _rect.localScale = _restScale == Vector3.zero ? Vector3.one : _restScale;
            }

            if (_group != null)
            {
                _group.alpha = 0f;
                _group.interactable = false;
                _group.blocksRaycasts = false;
            }
        }

        private void Update()
        {
            if (!_animating || _rect == null || _group == null)
            {
                return;
            }

            var progress = Mathf.Clamp01((Time.unscaledTime - _startedAt) / _duration);
            var eased = 1f - Mathf.Pow(1f - progress, 3f);
            _rect.localPosition = Vector3.LerpUnclamped(_fromPosition, _toPosition, eased);
            _rect.localScale = Vector3.LerpUnclamped(_fromScale, _toScale, eased);
            _group.alpha = Mathf.LerpUnclamped(_fromAlpha, _toAlpha, eased);

            if (progress < 1f)
            {
                return;
            }

            _animating = false;
            if (_closing)
            {
                CompleteClose();
            }
        }

        private void CompleteClose()
        {
            _animating = false;
            _closing = false;
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;
            var onClosed = _onClosed;
            _onClosed = null;
            onClosed?.Invoke();
        }
    }

    /// <summary>动态按钮的轻量 hover、按下和键盘选中反馈，不改变 Button 的业务事件。</summary>
    internal sealed class UiButtonMotion : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        ISelectHandler,
        IDeselectHandler
    {
        private RectTransform _rect;
        private Button _button;
        private Vector3 _restScale;
        private float _hoverScale;
        private float _pressScale;
        private bool _hovered;
        private bool _pressed;
        private bool _selected;

        public void Configure(float hoverScale = 1.016f, float pressScale = .978f)
        {
            _rect = transform as RectTransform;
            _button = GetComponent<Button>();
            _restScale = _rect == null ? Vector3.one : _rect.localScale;
            _hoverScale = hoverScale;
            _pressScale = pressScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = IsInteractable;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed = IsInteractable;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pressed = false;
            _hovered = IsInteractable;
        }

        public void OnSelect(BaseEventData eventData)
        {
            _selected = IsInteractable;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _selected = false;
        }

        private bool IsInteractable => _button != null && _button.IsInteractable();

        private void Update()
        {
            if (_rect == null)
            {
                return;
            }

            if (!IsInteractable)
            {
                _hovered = false;
                _pressed = false;
                _selected = false;
            }

            var targetScale = _pressed
                ? _pressScale
                : (_hovered || _selected ? _hoverScale : 1f);
            var smoothing = 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime);
            _rect.localScale = Vector3.Lerp(
                _rect.localScale,
                _restScale * targetScale,
                smoothing);
        }

        private void OnDisable()
        {
            _hovered = false;
            _pressed = false;
            _selected = false;
            if (_rect != null)
            {
                _rect.localScale = _restScale;
            }
        }
    }

    /// <summary>商品切换时只播放一次的短促图标刷新，不承担持续呼吸效果。</summary>
    internal sealed class ShopDetailRefreshMotion : MonoBehaviour
    {
        private const float Duration = .18f;
        private const float MaxScaleBump = .04f;

        private RectTransform _rect;
        private Vector3 _restScale;
        private float _startedAt;
        private bool _playing;

        public void Configure(RectTransform rect)
        {
            _rect = rect;
            _restScale = rect.localScale;
            _startedAt = 0f;
            _playing = false;
        }

        public void Play()
        {
            if (_rect == null)
            {
                return;
            }

            _startedAt = Time.unscaledTime;
            _playing = true;
        }

        private void Update()
        {
            if (!_playing || _rect == null)
            {
                return;
            }

            var progress = Mathf.Clamp01((Time.unscaledTime - _startedAt) / Duration);
            var bump = Mathf.Sin(progress * Mathf.PI) * MaxScaleBump;
            _rect.localScale = _restScale * (1f + bump);
            if (progress >= 1f)
            {
                _rect.localScale = _restScale;
                _playing = false;
            }
        }

        private void OnDisable()
        {
            _playing = false;
            if (_rect != null)
            {
                _rect.localScale = _restScale;
            }
        }
    }

    /// <summary>鍟嗗搧璇︽儏鍥炬爣鐨勫井寮卞懠鍚告晥鏋滐紝淇濇寔淇℃伅灞傜骇鑰屼笉骞叉壈鎿嶄綔銆?/summary>
    internal sealed class GraphicPulse : MonoBehaviour
    {
        private Image _image;
        private float _min;
        private float _max;
        private float _speed;

        public void Configure(float min, float max, float speed)
        {
            _image = GetComponent<Image>();
            _min = min;
            _max = max;
            _speed = speed;
        }

        private void Update()
        {
            if (_image == null) return;
            var alpha = Mathf.Lerp(_min, _max, (Mathf.Sin(Time.unscaledTime * _speed * Mathf.PI) + 1f) * .5f);
            var color = _image.color;
            color.a = alpha;
            _image.color = color;
        }
    }
}
