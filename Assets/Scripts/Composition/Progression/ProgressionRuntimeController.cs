using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using Train.Architecture.Assets;
using Train.Architecture.Bootstrap;
using Train.Architecture.Events;
using Train.Equipment.Application;
using Train.Equipment.Core;
using Train.Equipment.Data;
using Train.GameFlow.Application.Events;
using Train.GameFlow.Runtime;
using Train.Gameplay.Combat;
using Train.Gameplay.Combat.Application.Events;
using Train.Inventory.Application;
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
            "Assets/Prefabs/World/WorldItemPickup.prefab";

        private readonly List<IInstanceLease> _lootLeases = new();
        private readonly HashSet<string> _rewardedLevels = new(
            StringComparer.Ordinal);

        private IProgressionService _progression;
        private IInventoryService _inventory;
        private IEquipmentService _equipment;
        private IAssetService _assets;
        private IEventBus _events;
        private IDisposable _enemySubscription;
        private IDisposable _levelSubscription;
        private IDisposable _sceneSubscription;
        private CancellationTokenSource _lifetime;
        private ProgressionOverlay _overlay;

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
            _enemySubscription = _events.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
            _levelSubscription = _events.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
            _sceneSubscription = _events.Subscribe<LevelSceneLoadedEvent>(OnSceneLoaded);
            _overlay = ProgressionOverlay.Create(_events, _progression, _inventory, _equipment, LoadNextNode);
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

            _progression.CompleteLevel(message.LevelId, out var nextNode);
            var rewardItem = nextNode.IsBoss ? "upgrade_module" : "training_chip";
            var rewardCount = nextNode.IsBoss ? 8 : 6;
            _inventory?.TryAdd(rewardItem, rewardCount);
            _overlay?.ShowCompletion(
                message.LevelId,
                message.ElapsedSeconds,
                rewardItem,
                rewardCount,
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
            SceneManager.LoadSceneAsync(node.ScenePath, LoadSceneMode.Single);
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
        private readonly Action<RunNode> _loadNext;
        private readonly GameObject _root;
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
        private Health _bossHealth;
        private float _toastHideAt;

        private ProgressionOverlay(
            IEventBus events,
            IProgressionService progression,
            IInventoryService inventory,
            IEquipmentService equipment,
            Action<RunNode> loadNext)
        {
            _events = events;
            _progression = progression;
            _inventory = inventory;
            _equipment = equipment;
            _loadNext = loadNext;

            _root = new GameObject("[ProgressionOverlay]");
            UnityEngine.Object.DontDestroyOnLoad(_root);
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 250;
            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            _root.AddComponent<GraphicRaycaster>();

            _wallet = CreateLabel(_root.transform, "COINS 0150", 26, TextAlignmentOptions.TopRight);
            SetRect(_wallet.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(56f, -116f), new Vector2(320f, 48f));
            _wallet.color = new Color32(255, 214, 120, 255);

            _toast = CreateLabel(_root.transform, string.Empty, 22, TextAlignmentOptions.Center);
            SetRect(_toast.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -250f), new Vector2(620f, 52f));
            _toast.color = new Color32(120, 235, 235, 255);

            _completion = CreatePanel(_root.transform, "[MissionResult]");
            SetRect(_completion.GetComponent<RectTransform>(), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(760f, 520f));
            var completionTitle = CreateLabel(_completion.transform, "MISSION COMPLETE", 34, TextAlignmentOptions.Top);
            SetRect(completionTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -58f), new Vector2(0f, 54f));
            _completionSummary = CreateLabel(_completion.transform, string.Empty, 22, TextAlignmentOptions.Center);
            SetRect(_completionSummary.rectTransform, new Vector2(.08f, .42f), new Vector2(.92f, .78f), new Vector2(.5f, .6f), Vector2.zero, Vector2.zero);
            _shopButton = CreateButton(_completion.transform, "OPEN SHOP / 进入商店", 22);
            SetRect(_shopButton.GetComponent<RectTransform>(), new Vector2(.08f, .10f), new Vector2(.46f, .28f), new Vector2(.27f, .19f), Vector2.zero, Vector2.zero);
            _nextButton = CreateButton(_completion.transform, "NEXT MISSION / 前往下一关", 22);
            SetRect(_nextButton.GetComponent<RectTransform>(), new Vector2(.54f, .10f), new Vector2(.92f, .28f), new Vector2(.73f, .19f), Vector2.zero, Vector2.zero);
            _shopButton.onClick.AddListener(ShowShop);
            _nextButton.onClick.AddListener(LoadNext);

            _shop = CreatePanel(_root.transform, "[Shop]");
            SetRect(_shop.GetComponent<RectTransform>(), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(860f, 650f));
            var shopTitle = CreateLabel(_shop.transform, "FIELD SHOP // 战地商店", 32, TextAlignmentOptions.Top);
            SetRect(shopTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -50f), new Vector2(0f, 50f));
            _shopStatus = CreateLabel(_shop.transform, string.Empty, 18, TextAlignmentOptions.Center);
            SetRect(_shopStatus.rectTransform, new Vector2(.08f, .08f), new Vector2(.92f, .18f), new Vector2(.5f, .13f), Vector2.zero, Vector2.zero);
            var close = CreateButton(_shop.transform, "RETURN / 返回", 20);
            SetRect(close.GetComponent<RectTransform>(), new Vector2(.32f, .18f), new Vector2(.68f, .28f), new Vector2(.5f, .23f), Vector2.zero, Vector2.zero);
            close.onClick.AddListener(HideShop);

            _bossBar = CreatePanel(_root.transform, "[BossHealth]");
            SetRect(_bossBar.GetComponent<RectTransform>(), new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -116f), new Vector2(650f, 72f));
            var bossBackground = _bossBar.GetComponent<Image>();
            bossBackground.color = new Color32(24, 14, 34, 244);
            _bossLabel = CreateLabel(_bossBar.transform, "BOSS // CORE FORGE", 18, TextAlignmentOptions.Top);
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

        public static ProgressionOverlay Create(IEventBus events, IProgressionService progression, IInventoryService inventory, IEquipmentService equipment, Action<RunNode> loadNext)
        {
            return new ProgressionOverlay(events, progression, inventory, equipment, loadNext);
        }

        public void RenderWallet(RunProgressSnapshot snapshot)
        {
            _wallet.text = $"COINS {snapshot.Coins:0000}";
        }

        public void Tick()
        {
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
            ShowCombatToast(node.DisplayName, node.IsBoss ? "BOSS NODE ONLINE" : "战区已载入");
        }

        public void ShowCompletion(string levelId, float elapsed, string rewardItem, int rewardCount, RunNode nextNode)
        {
            _completion.SetActive(true);
            _shop.SetActive(false);
            _completionSummary.text = $"{levelId}\nCLEAR TIME  {elapsed:0.0}s\n\n战利品已写入背包：{rewardItem} ×{rewardCount}\n\n下一节点：{(string.IsNullOrWhiteSpace(nextNode.LevelId) ? "RUN COMPLETE" : nextNode.DisplayName)}";
            _nextButton.interactable = !string.IsNullOrWhiteSpace(nextNode.ScenePath);
            RenderWallet(_progression.Snapshot);
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
                    _bossLabel.text = $"BOSS // {identity.ArchetypeId.ToUpperInvariant()}";
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
            if (!_bossHealth.IsAlive)
            {
                _bossBar.SetActive(false);
            }
        }

        public void HideAllPanels()
        {
            _completion.SetActive(false);
            _shop.SetActive(false);
        }

        private void ShowShop()
        {
            _completion.SetActive(false);
            _shop.SetActive(true);
            _shopStatus.text = "选择装备，购买后自动放入背包并穿戴。";
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
                var button = CreateButton(rows.transform, $"{definition.DisplayName}   C {cost}    [{definition.Rarity}]", 20);
                button.onClick.AddListener(() => Buy(definition, cost));
            }
        }

        private void Buy(EquipmentItemDefinition definition, int cost)
        {
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
            _shopStatus.text = $"已购买并装备：{definition.DisplayName}";
            RenderWallet(_progression.Snapshot);
        }

        private void HideShop()
        {
            _shop.SetActive(false);
            _completion.SetActive(true);
        }

        private void LoadNext()
        {
            if (_progression.Snapshot.CurrentNodeIndex + 1 < _progression.Nodes.Count)
            {
                _loadNext(_progression.Nodes[_progression.Snapshot.CurrentNodeIndex + 1]);
            }
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
