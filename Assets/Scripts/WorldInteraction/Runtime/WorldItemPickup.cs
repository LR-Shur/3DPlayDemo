using Train.Architecture.Bootstrap;
using Train.Architecture.Events;
using Train.Inventory.Application;
using Train.Inventory.Data;
using Train.WorldInteraction.Core;
using Train.WorldInteraction.Events;
using UnityEngine;

namespace Train.WorldInteraction.Runtime
{
    /// <summary>
    /// 地图中的一次性物品拾取组件。
    /// 它只通过 IInventoryService 写入背包，并通过事件总线通知 UI 与任务系统。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldItemPickup :
        MonoBehaviour,
        IInteractable
    {
        [SerializeField] private string _interactionId;
        [SerializeField] private string _itemId;
        [SerializeField, Min(1)] private int _quantity = 1;
        [SerializeField] private int _priority = 10;
        [SerializeField] private Transform _interactionPoint;
        [SerializeField] private GameObject _markerRoot;

        private IInventoryService _inventory;
        private IEventBus _events;
        private ItemDefinition _definition;
        private bool _collected;

        /// <summary>
        /// 场景内稳定且唯一的交互标识。
        /// </summary>
        public string InteractionId => _interactionId;

        /// <summary>
        /// 背包服务可用、物品定义存在且尚未拾取时允许交互。
        /// </summary>
        public bool IsAvailable =>
            !_collected && _inventory != null && _definition != null;

        /// <summary>
        /// 物品稳定标识。
        /// </summary>
        public string ItemId => _itemId;

        /// <summary>
        /// 物品显示名称。
        /// </summary>
        public string DisplayName =>
            _definition != null ? _definition.DisplayName : _itemId;

        /// <summary>
        /// 本次拾取数量。
        /// </summary>
        public int Quantity => Mathf.Max(1, _quantity);

        /// <summary>
        /// 物品品质。
        /// </summary>
        public ItemRarity Rarity =>
            _definition != null ? _definition.Rarity : ItemRarity.Common;

        /// <summary>
        /// 候选选择优先级。
        /// </summary>
        public int Priority => _priority;

        /// <summary>
        /// 玩家计算距离时使用的世界坐标。
        /// </summary>
        public Vector3 InteractionPosition =>
            _interactionPoint != null
                ? _interactionPoint.position
                : transform.position;

        /// <summary>
        /// 自动从启动上下文取得背包服务与事件总线。
        /// </summary>
        private void Start()
        {
            TryResolveServices();
        }

        /// <summary>
        /// 启动流程仍在加载 YooAsset 数据时持续等待背包服务。
        /// </summary>
        private void Update()
        {
            // 服务可能先于 Luban 物品目录安装完成；定义为空时继续重试，
            // 避免拾取物在启动竞态中永久变成不可交互状态。
            if (_inventory == null || _definition == null)
            {
                TryResolveServices();
            }
        }

        /// <summary>
        /// 注入背包与事件服务，供启动器、场景测试和自动化测试复用。
        /// </summary>
        public void Initialize(
            IInventoryService inventory,
            IEventBus events)
        {
            _inventory = inventory;
            _events = events;
            _definition = null;
            if (_inventory != null)
            {
                _inventory.TryGetDefinition(_itemId, out _definition);
            }
        }

        /// <summary>
        /// 配置场景掉落数据。
        /// 编辑器内容构建器使用该方法批量生成不同品质和数量的拾取物。
        /// </summary>
        public void Configure(
            string interactionId,
            string itemId,
            int quantity,
            int priority = 10)
        {
            _interactionId = interactionId;
            _itemId = itemId;
            _quantity = Mathf.Max(1, quantity);
            _priority = priority;
        }

        /// <summary>
        /// 配置拾取距离点与地图光标根节点。
        /// </summary>
        public void ConfigurePresentation(
            Transform interactionPoint,
            GameObject markerRoot)
        {
            _interactionPoint = interactionPoint;
            _markerRoot = markerRoot;
        }

        /// <summary>
        /// 尝试把物品原子写入背包；背包满时保留场景物体。
        /// </summary>
        public InteractResult Interact()
        {
            if (!IsAvailable)
            {
                return PublishResult(
                    InteractResult.Unavailable("该物品当前无法拾取。"));
            }

            if (!_inventory.TryAdd(_itemId, Quantity))
            {
                return PublishResult(
                    InteractResult.InventoryFull("背包空间不足，物品仍留在原地。"));
            }

            _collected = true;
            if (_markerRoot != null)
            {
                _markerRoot.SetActive(false);
            }

            var result = InteractResult.Succeeded(
                $"获得 {DisplayName} ×{Quantity}");
            PublishResult(result);

            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }

            return result;
        }

        /// <summary>
        /// 发布统一拾取反馈并返回原结果。
        /// </summary>
        private InteractResult PublishResult(InteractResult result)
        {
            _events?.Publish(
                new WorldItemPickupFeedbackEvent(
                    _itemId,
                    DisplayName,
                    Quantity,
                    Rarity,
                    result.Code,
                    result.Message));
            return result;
        }

        /// <summary>
        /// 尝试从全局服务注册表取得背包；尚未准备好时保持不可交互。
        /// </summary>
        private void TryResolveServices()
        {
            var bootstrap = GameBootstrap.EnsureExists();
            if (bootstrap.Context.Services.TryResolve<IInventoryService>(
                    out var inventory))
            {
                Initialize(inventory, bootstrap.Context.Events);
            }
        }
    }
}
