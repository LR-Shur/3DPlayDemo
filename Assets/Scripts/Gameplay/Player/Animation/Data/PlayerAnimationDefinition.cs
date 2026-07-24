using UnityEngine;

namespace Train.Gameplay.Player.Animation.Data
{
    /// <summary>
    /// 保存一个角色动画的资源引用和播放规则。
    /// 它是数据对象，不包含任何 Animancer 或状态机逻辑。
    /// </summary>
    [System.Serializable]
    public sealed class PlayerAnimationDefinition
    {
        [SerializeField] private PlayerAnimationId _id;
        [SerializeField] private PlayerAnimationCategory _category;
        [SerializeField] private AnimationClip _clip;
        [SerializeField] private bool _loop;
        [SerializeField, Min(0f)] private float _fadeDuration = 0.15f;
        [SerializeField] private PlayerAnimationStateKind _stateKind;
        [SerializeField] private PlayerAnimationMovementPolicy _movementPolicy;
        [SerializeField, Min(0f)] private float _rootMotionPositionScale = 1f;
        [SerializeField, Min(0f)] private float _authoredMotionDistance;
        [SerializeField] private AnimationCurve _authoredMotionCurve;
        [SerializeField, Range(0f, 1f)] private float _movementCancelStartNormalizedTime = 1f;
        [SerializeField, HideInInspector] private bool _hasActionTimingConfigured;
        [SerializeField, HideInInspector] private bool _useRootMotion;
        [SerializeField, HideInInspector] private bool _hasMovementPolicyConfigured;
        [SerializeField] private bool _canBeInterrupted;

        /// <summary>
        /// 获取玩法代码用于请求该动画的稳定标识。
        /// </summary>
        public PlayerAnimationId Id => _id;

        /// <summary>
        /// 获取动画所属的大类。
        /// </summary>
        public PlayerAnimationCategory Category => _category;

        /// <summary>
        /// 获取实际由 Animancer 播放的动画剪辑。
        /// </summary>
        public AnimationClip Clip => _clip;

        /// <summary>
        /// 获取该动画是否应持续循环。
        /// </summary>
        public bool Loop => _loop;

        /// <summary>
        /// 获取切换到该动画时使用的淡入时间。
        /// </summary>
        public float FadeDuration => _fadeDuration;

        /// <summary>
        /// 获取负责处理该动画的玩家状态类别。
        /// </summary>
        public PlayerAnimationStateKind StateKind => _stateKind;

        /// <summary>
        /// 获取该动画播放期间使用的水平位移策略。
        /// </summary>
        public PlayerAnimationMovementPolicy MovementPolicy =>
            _movementPolicy == PlayerAnimationMovementPolicy.KeepInPlace && _useRootMotion
                ? PlayerAnimationMovementPolicy.RootMotion
                : _movementPolicy;

        /// <summary>
        /// 获取 Root Motion 位移应用到 CharacterController 前使用的缩放系数。
        /// 仅当位移策略为 RootMotion 时生效。
        /// </summary>
        public float RootMotionPositionScale => _rootMotionPositionScale;

        /// <summary>
        /// 获取数据驱动位移动画在完整播放期间计划前进的总距离。
        /// 仅当位移策略为 AuthoredMotion 时生效。
        /// </summary>
        public float AuthoredMotionDistance => _authoredMotionDistance;

        /// <summary>
        /// 获取数据驱动位移使用的归一化累计距离曲线。
        /// 目录生成器会保留手动调整后的曲线。
        /// </summary>
        public AnimationCurve AuthoredMotionCurve => _authoredMotionCurve;

        /// <summary>
        /// 获取允许移动输入提前结束当前动作的最早归一化动画进度。
        /// 数值为一表示只能等待动画自然结束。
        /// </summary>
        public float MovementCancelStartNormalizedTime => _movementCancelStartNormalizedTime;

        /// <summary>
        /// 获取该定义是否已写入动作取消时机数据。
        /// 用于目录生成器在升级旧动画目录时填入默认值，同时保留后续手动调参。
        /// </summary>
        public bool HasActionTimingConfigured => _hasActionTimingConfigured;

        /// <summary>
        /// 获取该定义是否已写入新版位移策略数据。
        /// 用于让目录生成工具迁移旧数据时应用默认值，而不会覆盖后续手动调参。
        /// </summary>
        public bool HasMovementPolicyConfigured => _hasMovementPolicyConfigured;

        /// <summary>
        /// 获取该动画在当前版本中是否允许被其他行为打断。
        /// </summary>
        public bool CanBeInterrupted => _canBeInterrupted;

        /// <summary>
        /// 初始化由编辑器自动收录的动画定义。
        /// </summary>
        public PlayerAnimationDefinition(
            PlayerAnimationId id,
            PlayerAnimationCategory category,
            AnimationClip clip,
            bool loop,
            float fadeDuration,
            PlayerAnimationStateKind stateKind,
            PlayerAnimationMovementPolicy movementPolicy,
            float rootMotionPositionScale,
            float authoredMotionDistance,
            AnimationCurve authoredMotionCurve,
            float movementCancelStartNormalizedTime,
            bool canBeInterrupted)
        {
            _id = id;
            _category = category;
            _clip = clip;
            _loop = loop;
            _fadeDuration = fadeDuration;
            _stateKind = stateKind;
            _movementPolicy = movementPolicy;
            _rootMotionPositionScale = rootMotionPositionScale;
            _authoredMotionDistance = authoredMotionDistance;
            _authoredMotionCurve = authoredMotionCurve;
            _movementCancelStartNormalizedTime = Mathf.Clamp01(movementCancelStartNormalizedTime);
            _hasActionTimingConfigured = true;
            _useRootMotion = movementPolicy == PlayerAnimationMovementPolicy.RootMotion;
            _hasMovementPolicyConfigured = true;
            _canBeInterrupted = canBeInterrupted;
        }

        /// <summary>
        /// 根据动画归一化进度计算数据驱动位移已经累计执行的距离。
        /// 曲线的横轴和纵轴都使用零到一，最终结果会乘以该动画配置的总距离。
        /// </summary>
        /// <param name="normalizedTime">当前动画从零到一的归一化播放进度。</param>
        /// <returns>从动画开始到当前进度应累计移动的米数。</returns>
        public float EvaluateAuthoredMotionDistance(float normalizedTime)
        {
            var clampedTime = Mathf.Clamp01(normalizedTime);
            var normalizedDistance = _authoredMotionCurve != null &&
                                     _authoredMotionCurve.length > 0
                ? _authoredMotionCurve.Evaluate(clampedTime)
                : clampedTime;
            return Mathf.Clamp01(normalizedDistance) * Mathf.Max(0f, _authoredMotionDistance);
        }
    }
}
