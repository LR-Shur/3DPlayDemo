using System;
using System.Collections.Generic;
using Train.Architecture.Bootstrap;
using Train.Architecture.Events;
using Train.Gameplay.Player.Input;
using Train.WorldInteraction.Core;
using Train.WorldInteraction.Events;
using UnityEngine;

namespace Train.WorldInteraction.Runtime
{
    /// <summary>
    /// 扫描玩家附近的世界交互目标、维护稳定焦点并消费 E 键交互。
    /// 组件先于玩家状态机更新；只有存在焦点时才消费交互输入。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class PlayerInteractionController : MonoBehaviour
    {
        private const int OverlapCapacity = 64;

        [SerializeField] private PlayerInputReader _input;
        [SerializeField, Min(0.5f)] private float _interactionRange = 3f;
        [SerializeField, Min(0.02f)] private float _scanInterval = 0.08f;
        [SerializeField] private LayerMask _interactionLayers = ~0;
        [SerializeField] private Vector3 _scanCenterOffset = Vector3.up;

        private readonly Collider[] _overlaps = new Collider[OverlapCapacity];
        private readonly Dictionary<string, IInteractable> _tracked =
            new(StringComparer.Ordinal);
        private readonly HashSet<string> _seen = new(StringComparer.Ordinal);
        private readonly List<string> _exitBuffer = new();
        private readonly InteractionFocusModel _focus = new();

        private IEventBus _events;
        private float _nextScanTime;

        /// <summary>
        /// 获取当前是否存在可交互焦点，供测试与调试面板读取。
        /// </summary>
        public bool HasFocus => _focus.HasFocus;

        /// <summary>
        /// 查找玩家输入组件和全局事件总线。
        /// </summary>
        private void Awake()
        {
            _input ??= GetComponent<PlayerInputReader>();
            // 交互提示属于当前场景，优先使用场景事件中心；
            // 场景尚未完成初始化时再回退到进程级事件中心。
            // UI 服务订阅的是全局事件总线，交互提示也必须发布到同一条总线。
            _events = GameBootstrap.EnsureExists().Context.Events;
        }

        /// <summary>
        /// 开始监听纯领域模型的焦点变化。
        /// </summary>
        private void OnEnable()
        {
            _focus.FocusChanged += OnFocusChanged;
        }

        /// <summary>
        /// 定时刷新候选，并在存在焦点时优先消费交互输入。
        /// </summary>
        private void Update()
        {
            if (Time.unscaledTime >= _nextScanTime)
            {
                ScanNearbyPickups();
                _nextScanTime = Time.unscaledTime + _scanInterval;
            }

            _focus.Refresh();

            // E 键只在这里消费，避免和玩家 LocomotionState 竞争同一个输入缓冲。
            var interactPressed = _input != null &&
                                  _input.HasInteractPressed &&
                                  _input.ConsumeInteractPressed();
            if (interactPressed && _focus.HasFocus)
            {
                _focus.InteractFocused();
            }
        }

        /// <summary>
        /// 停止监听并隐藏交互提示。
        /// </summary>
        private void OnDisable()
        {
            _focus.FocusChanged -= OnFocusChanged;
            _focus.Clear();
            _tracked.Clear();
            // 场景卸载时 UI 可能已经销毁，避免向旧 HudView 发布事件造成 MissingReferenceException。
            if (!GameBootstrap.IsApplicationQuitting &&
                gameObject != null &&
                gameObject.scene.IsValid() &&
                gameObject.scene.isLoaded)
            {
                _events?.Publish(InteractionPromptChangedEvent.Hidden());
            }
        }

        /// <summary>
        /// 使用无分配球形查询查找附近拾取物，并同步纯领域候选集合。
        /// </summary>
        private void ScanNearbyPickups()
        {
            _seen.Clear();
            var center = transform.TransformPoint(_scanCenterOffset);
            var count = Physics.OverlapSphereNonAlloc(
                center,
                _interactionRange,
                _overlaps,
                _interactionLayers,
                QueryTriggerInteraction.Collide);

            if (count >= _overlaps.Length)
            {
                // NonAlloc 查询在容量耗尽时会静默截断结果；
                // 这在城市场景的碰撞体较多时会漏掉终端或拾取物。
                // 仅在饱和帧回退到完整查询，正常帧仍保持无分配路径。
                var completeOverlaps = Physics.OverlapSphere(
                    center,
                    _interactionRange,
                    _interactionLayers,
                    QueryTriggerInteraction.Collide);
                for (var index = 0; index < completeOverlaps.Length; index++)
                {
                    ProcessOverlap(center, completeOverlaps[index]);
                }
            }
            else
            {
                for (var index = 0; index < count; index++)
                {
                    ProcessOverlap(center, _overlaps[index]);
                }
            }

            for (var index = 0; index < _overlaps.Length; index++)
            {
                _overlaps[index] = null;
            }

            _exitBuffer.Clear();
            foreach (var pair in _tracked)
            {
                if (!_seen.Contains(pair.Key) || pair.Value == null)
                {
                    _exitBuffer.Add(pair.Key);
                }
            }

            foreach (var interactionId in _exitBuffer)
            {
                _focus.Exit(interactionId);
                _tracked.Remove(interactionId);
            }
        }

        /// <summary>
        /// 将一个物理碰撞体转换为交互候选并提交给焦点模型。
        /// </summary>
        private void ProcessOverlap(Vector3 center, Collider overlap)
        {
            if (overlap == null)
            {
                return;
            }

            var interactable = FindInteractable(overlap);
            if (interactable == null ||
                string.IsNullOrWhiteSpace(interactable.InteractionId) ||
                !_seen.Add(interactable.InteractionId))
            {
                return;
            }

            var distance = Vector3.Distance(
                center,
                GetInteractionPosition(interactable, overlap.transform));
            var candidate = new InteractionCandidate(
                interactable,
                GetInteractionPriority(interactable),
                distance);

            if (_tracked.ContainsKey(interactable.InteractionId))
            {
                _focus.Update(candidate);
            }
            else
            {
                _tracked.Add(interactable.InteractionId, interactable);
                _focus.Enter(candidate);
            }
        }

        /// <summary>
        /// 把领域焦点变化转换为只含展示数据的 UI 事件。
        /// </summary>
        private void OnFocusChanged(
            object sender,
            InteractionFocusChangedEventArgs eventArgs)
        {
            if (!eventArgs.CurrentCandidate.HasValue)
            {
                _events?.Publish(InteractionPromptChangedEvent.Hidden());
                return;
            }

            var interactable =
                eventArgs.CurrentCandidate.Value.Interactable;
            if (interactable is WorldItemPickup pickup)
            {
                _events?.Publish(
                    new InteractionPromptChangedEvent(
                        true,
                        pickup.InteractionId,
                        pickup.DisplayName,
                        pickup.Quantity,
                        pickup.Rarity,
                        "拾取"));
                return;
            }

            _events?.Publish(
                new InteractionPromptChangedEvent(
                    true,
                    interactable.InteractionId,
                    interactable is IInteractionTargetInfo targetInfo
                        ? targetInfo.DisplayName
                        : "可交互目标",
                    0,
                    Train.Inventory.Data.ItemRarity.Common,
                    interactable is IInteractionTargetInfo actionInfo
                        ? actionInfo.ActionLabel
                        : "交互"));
        }

        /// <summary>
        /// 从物理查询结果中找到一个实现了 IInteractable 的 MonoBehaviour。
        /// </summary>
        private static IInteractable FindInteractable(
            Collider overlap)
        {
            var behaviours =
                overlap.GetComponentsInParent<MonoBehaviour>(true);
            for (var index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IInteractable interactable)
                {
                    return interactable;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取通用交互目标的业务优先级。
        /// </summary>
        private static int GetInteractionPriority(
            IInteractable interactable)
        {
            return interactable switch
            {
                WorldItemPickup pickup => pickup.Priority,
                IInteractionTargetInfo info => info.Priority,
                _ => 0
            };
        }

        /// <summary>
        /// 获取通用交互目标的世界坐标。
        /// </summary>
        private static Vector3 GetInteractionPosition(
            IInteractable interactable,
            Transform fallback)
        {
            return interactable switch
            {
                WorldItemPickup pickup => pickup.InteractionPosition,
                IInteractionTargetInfo info => info.InteractionPosition,
                Component component => component.transform.position,
                _ => fallback != null ? fallback.position : Vector3.zero
            };
        }

        /// <summary>
        /// 在场景视图显示交互扫描范围，便于关卡设计时调整。
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.35f, 0.88f, 0.91f, 0.35f);
            Gizmos.DrawWireSphere(
                transform.TransformPoint(_scanCenterOffset),
                _interactionRange);
        }
    }
}
