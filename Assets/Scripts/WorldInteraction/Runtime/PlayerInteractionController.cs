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
            if (_input != null &&
                _focus.HasFocus &&
                _input.HasInteractPressed &&
                _input.ConsumeInteractPressed())
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
            _events?.Publish(InteractionPromptChangedEvent.Hidden());
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

            for (var index = 0; index < count; index++)
            {
                var overlap = _overlaps[index];
                _overlaps[index] = null;
                if (overlap == null)
                {
                    continue;
                }

                var interactable = FindInteractable(overlap);
                if (interactable == null ||
                    string.IsNullOrWhiteSpace(interactable.InteractionId) ||
                    !_seen.Add(interactable.InteractionId))
                {
                    continue;
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
