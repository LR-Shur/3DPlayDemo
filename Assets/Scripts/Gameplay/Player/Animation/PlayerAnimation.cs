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
        [SerializeField, Tooltip("当前动画已播放到的归一化进度；循环动画会持续递增。")]
        private float _currentAnimationNormalizedTime;
        [SerializeField, Tooltip("当前动画采用的水平位移策略。")]
        private PlayerAnimationMovementPolicy _currentMovementPolicy;
        [SerializeField, Tooltip("当前 Root Motion 位移缩放系数；仅 RootMotion 策略生效。")]
        private float _currentRootMotionPositionScale = 1f;

        private AnimancerState _currentAnimationState;

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

            PlayDefinition(definition, null);
        }

        /// <summary>
        /// 播放目录中配置的一次性动画，并设置其结束回调。
        /// </summary>
        /// <param name="id">需要播放的动画标识。</param>
        /// <param name="onEnded">动画片段播放结束后调用的回调。</param>
        public void PlayOneShot(PlayerAnimationId id, Action onEnded)
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

            PlayDefinition(definition, onEnded);
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
        private void PlayDefinition(PlayerAnimationDefinition definition, Action onEnded)
        {
            var state = _animancer.Play(definition.Clip, definition.FadeDuration, FadeMode.FromStart);
            _currentAnimationId = definition.Id;
            _currentAnimationClip = definition.Clip;
            _currentAnimationNormalizedTime = 0f;
            _currentMovementPolicy = definition.MovementPolicy;
            _currentRootMotionPositionScale = definition.RootMotionPositionScale;
            _currentAnimationState = state;
            if (definition.Loop && !state.IsLooping)
            {
                Debug.LogWarning($"动画 {definition.Id} 被标记为循环，但 FBX 导入设置未启用循环。", this);
            }

            state.Events(this).OnEnd = definition.Loop ? null : onEnded;
        }

        /// <summary>
        /// 从目录中获取可播放的动画定义，并校验 Animancer 与资源引用。
        /// </summary>
        /// <param name="id">需要查询的动画标识。</param>
        /// <param name="definition">查询成功时返回动画定义。</param>
        /// <returns>定义存在且可播放时返回 true。</returns>
        private bool TryGetDefinition(PlayerAnimationId id, out PlayerAnimationDefinition definition)
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
