using System;
using Animancer;
using Train.Gameplay.Player.Animation.Data;
using UnityEngine;

namespace Train.Gameplay.Player.Animation
{
    /// <summary>
    /// 集中管理所有直接调用 Animancer 的逻辑，并从动画目录按语义标识查找定义。
    /// 具体状态不需要了解动画片段、淡入时间或 Animancer 事件配置。
    /// </summary>
    public sealed class PlayerAnimation : MonoBehaviour
    {
        [SerializeField] private AnimancerComponent _animancer;
        [SerializeField] private PlayerAnimationCatalog _catalog;

        [Header("运行时动画调试（播放时查看）")]
        [SerializeField, Tooltip("状态机最后请求播放的动画标识。")]
        private PlayerAnimationId _currentAnimationId;
        [SerializeField, Tooltip("当前实际交给 Animancer 播放的动画片段。")]
        private AnimationClip _currentAnimationClip;
        [SerializeField, Tooltip("当前实际交给 Animancer 播放的 TransitionAsset。")]
        private TransitionAssetBase _currentTransitionAsset;
        [SerializeField, Tooltip("当前动画已播放到的归一化进度；循环动画会持续递增。")]
        private float _currentAnimationNormalizedTime;
        [SerializeField, Tooltip("当前动画采用的水平位移策略。")]
        private PlayerAnimationMovementPolicy _currentMovementPolicy;
        [SerializeField, Tooltip("当前 Root Motion 位移缩放系数；仅 RootMotion 策略生效。")]
        private float _currentRootMotionPositionScale = 1f;

        private AnimancerState _currentAnimationState;

        /// <summary>
        /// 获取当前实际播放状态的归一化时间，供与动画同步的玩法位移采样。
        /// </summary>
        public float CurrentAnimationNormalizedTime =>
            _currentAnimationState != null
                ? _currentAnimationState.NormalizedTime
                : _currentAnimationNormalizedTime;

        /// <summary>
        /// 当 Inspector 中未指定时查找 Animancer 组件和默认动画目录。
        /// </summary>
        private void Awake()
        {
            _animancer ??= GetComponent<AnimancerComponent>();
            _catalog ??= Resources.Load<PlayerAnimationCatalog>("Player/EllenAnimationCatalog");
        }

        /// <summary>
        /// 每帧刷新 Inspector 中显示的当前动画进度，便于在运行时定位状态机与动画播放问题。
        /// </summary>
        private void Update()
        {
            if (_currentAnimationState != null)
            {
                _currentAnimationNormalizedTime = _currentAnimationState.NormalizedTime;
            }
        }

        /// <summary>
        /// 播放目录中配置为循环的动画。
        /// </summary>
        /// <param name="id">需要播放的动画标识。</param>
        public void PlayLoop(PlayerAnimationId id)
        {
            if (!TryGetDefinition(id, out var definition))
            {
                return;
            }

            if (!definition.Loop)
            {
                Debug.LogWarning($"动画 {id} 未配置为循环动画。", this);
            }

            var transition = _catalog.TryGetContextualTransition(
                _currentAnimationId,
                id,
                out var contextualTransition)
                ? contextualTransition
                : definition.Transition;
            PlayDefinition(definition, null, null, transition);
        }

        /// <summary>
        /// 播放目录中配置的一次性动画，并设置其结束回调。
        /// </summary>
        /// <param name="id">需要播放的动画标识。</param>
        /// <param name="onEnded">动画片段播放结束后调用的回调。</param>
        public void PlayOneShot(PlayerAnimationId id, Action onEnded)
        {
            PlayOneShot(id, onEnded, null, null);
        }

        /// <summary>
        /// 播放目录中配置的一次性动画，并为 TransitionAsset 中的命名事件绑定本次播放专属回调。
        /// 命名事件不存在时仍会正常播放并执行结束回调，同时输出警告帮助定位漏配资源。
        /// </summary>
        /// <param name="id">需要播放的动画标识。</param>
        /// <param name="onEnded">动画片段播放结束后调用的回调。</param>
        /// <param name="eventName">TransitionAsset 中需要监听的命名事件。</param>
        /// <param name="onNamedEvent">动画经过命名事件时调用的回调。</param>
        public void PlayOneShot(
            PlayerAnimationId id,
            Action onEnded,
            string eventName,
            Action onNamedEvent)
        {
            PlayOneShot(
                id,
                onEnded,
                new PlayerAnimationEventBinding(eventName, onNamedEvent));
        }

        /// <summary>
        /// 播放目录中配置的一次性动画，并绑定任意数量的 TransitionAsset 命名事件。
        /// 事件时间完全保存在动画资源中，同一个状态不需要写死动画进度。
        /// </summary>
        /// <param name="id">需要播放的动画标识。</param>
        /// <param name="onEnded">动画片段播放结束后调用的回调。</param>
        /// <param name="eventBindings">本次播放需要监听的命名事件集合。</param>
        public void PlayOneShot(
            PlayerAnimationId id,
            Action onEnded,
            params PlayerAnimationEventBinding[] eventBindings)
        {
            if (!TryGetDefinition(id, out var definition))
            {
                onEnded?.Invoke();
                return;
            }

            if (definition.Loop)
            {
                Debug.LogWarning($"动画 {id} 被配置为循环动画，不能作为一次性动作播放。", this);
            }

            PlayDefinition(definition, onEnded, eventBindings);
        }

        /// <summary>
        /// 查询指定动画的位移策略与 Root Motion 位移缩放系数。
        /// 状态机读取该数据并配置移动组件，不在状态类中写死动画距离。
        /// </summary>
        /// <param name="id">需要查询的动画标识。</param>
        /// <param name="movementPolicy">动画播放期间采用的位移策略。</param>
        /// <param name="rootMotionPositionScale">Root Motion 水平位移的缩放系数。</param>
        /// <returns>动画目录存在该动画定义时返回 true。</returns>
        public bool TryGetMovementSettings(
            PlayerAnimationId id,
            out PlayerAnimationMovementPolicy movementPolicy,
            out float rootMotionPositionScale)
        {
            if (TryGetDefinition(id, out var definition))
            {
                movementPolicy = definition.MovementPolicy;
                rootMotionPositionScale = definition.RootMotionPositionScale;
                return true;
            }

            movementPolicy = PlayerAnimationMovementPolicy.KeepInPlace;
            rootMotionPositionScale = 1f;
            return false;
        }

        /// <summary>
        /// 根据目录定义播放动画，并统一配置循环与动画结束事件。
        /// </summary>
        /// <param name="definition">已通过目录查询的动画定义。</param>
        /// <param name="onEnded">一次性动画结束时调用的回调。</param>
        /// <param name="eventBindings">需要绑定到 TransitionAsset 的本次播放专属事件集合。</param>
        /// <param name="transitionOverride">当前来源到目标组合需要使用的专属过渡资源。</param>
        private void PlayDefinition(
            PlayerAnimationDefinition definition,
            Action onEnded,
            PlayerAnimationEventBinding[] eventBindings = null,
            TransitionAssetBase transitionOverride = null)
        {
            var transition = transitionOverride != null
                ? transitionOverride
                : definition.Transition;
            var state = _animancer.Play(transition);
            _currentAnimationId = definition.Id;
            _currentAnimationClip = state.Clip;
            _currentTransitionAsset = transition;
            _currentAnimationNormalizedTime = 0f;
            _currentMovementPolicy = definition.MovementPolicy;
            _currentRootMotionPositionScale = definition.RootMotionPositionScale;
            _currentAnimationState = state;
            if (definition.Loop && !state.IsLooping)
            {
                Debug.LogWarning($"动画 {definition.Id} 被标记为循环，但 FBX 导入设置未启用循环。", this);
            }

            var events = state.Events(this);
            events.OnEnd = definition.Loop ? null : onEnded;
            if (eventBindings == null)
            {
                return;
            }

            foreach (var eventBinding in eventBindings)
            {
                if (string.IsNullOrWhiteSpace(eventBinding.EventName) ||
                    eventBinding.Callback == null)
                {
                    continue;
                }

                var eventReference = StringReference.Get(eventBinding.EventName);
                if (events.IndexOf(eventReference) >= 0)
                {
                    events.SetCallback(eventReference, eventBinding.Callback);
                }
                else
                {
                    Debug.LogWarning(
                        $"动画 {definition.Id} 的 TransitionAsset 缺少命名事件 " +
                        $"{eventBinding.EventName}，" +
                        "状态将等待动画自然结束作为安全回退。",
                        definition.Transition);
                }
            }
        }

        /// <summary>
        /// 从目录中获取可播放的动画定义，并校验 Animancer 与资源引用。
        /// </summary>
        /// <param name="id">需要查询的动画标识。</param>
        /// <param name="definition">查询成功时返回动画定义。</param>
        /// <returns>定义存在且可播放时返回 true。</returns>
        public bool TryGetDefinition(PlayerAnimationId id, out PlayerAnimationDefinition definition)
        {
            if (_animancer != null && _catalog != null && _catalog.TryGet(id, out definition))
            {
                return true;
            }

            Debug.LogError($"PlayerAnimation 缺少 AnimancerComponent、动画目录或动画 {id} 的有效配置。", this);
            definition = null;
            return false;
        }
    }
}
