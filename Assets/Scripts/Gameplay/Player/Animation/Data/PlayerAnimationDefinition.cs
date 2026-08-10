using Animancer;
using UnityEngine;
using UnityEngine.Serialization;

namespace Train.Gameplay.Player.Animation.Data
{
    /// <summary>
    /// 保存一个角色动画的玩法元数据，以及真正负责播放配置的 Animancer TransitionAsset。
    /// 淡入、速度、起始时间、事件和具体动画类型均由 TransitionAsset 管理，本类只保留状态机需要的规则。
    /// </summary>
    [System.Serializable]
    public sealed class PlayerAnimationDefinition
    {
        [SerializeField] private PlayerAnimationId _id;
        [SerializeField] private PlayerAnimationCategory _category;
        [SerializeField] private TransitionAssetBase _transition;
        [SerializeField] private PlayerAnimationStateKind _stateKind;
        [SerializeField] private PlayerAnimationMovementPolicy _movementPolicy;
        [SerializeField, Min(0f)] private float _rootMotionPositionScale = 1f;
        [SerializeField, Min(0f)] private float _authoredMotionDistance;
        [SerializeField] private AnimationCurve _authoredMotionCurve;
        [SerializeField] private AnimationCurve _authoredTurnCurve;
        [FormerlySerializedAs("_movementCancelStartNormalizedTime")]
        [SerializeField, Range(0f, 1f)] private float _cancelStartNormalizedTime = 1f;
        [SerializeField, Range(0f, 1f)] private float _cancelEndNormalizedTime = 1f;
        [SerializeField] private PlayerAnimationCancelTarget _cancelTargets;
        [SerializeField, HideInInspector] private bool _hasActionTimingConfigured;
        [SerializeField, HideInInspector] private bool _hasCancelRuleConfigured;
        [SerializeField, HideInInspector] private bool _useRootMotion;
        [SerializeField, HideInInspector] private bool _hasMovementPolicyConfigured;

        /// <summary>
        /// 获取玩法代码用于请求该动画的稳定标识。
        /// </summary>
        public PlayerAnimationId Id => _id;

        /// <summary>
        /// 获取动画所属的大类。
        /// </summary>
        public PlayerAnimationCategory Category => _category;

        /// <summary>
        /// 获取实际交给 Animancer 播放的 TransitionAsset。
        /// 字段使用基类类型，因此将来可以直接替换为 Clip、Mixer 或自定义 TransitionAsset。
        /// </summary>
        public TransitionAssetBase Transition => _transition;

        /// <summary>
        /// 获取 TransitionAsset 中用于调试和编辑器验证的主动画剪辑。
        /// Mixer 等复合 Transition 没有唯一剪辑时可能返回空值。
        /// </summary>
        public AnimationClip PrimaryClip =>
            _transition != null &&
            _transition.GetTransition() is ClipTransition clipTransition
                ? clipTransition.Clip
                : null;

        /// <summary>
        /// 获取 TransitionAsset 当前是否会循环播放。
        /// </summary>
        public bool Loop => _transition != null && _transition.IsLooping;

        /// <summary>
        /// 获取 TransitionAsset 中配置的淡入时间。
        /// </summary>
        public float FadeDuration => _transition != null ? _transition.FadeDuration : 0f;

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
        /// 获取 Root Motion 位移应用到 Rigidbody 前使用的缩放系数。
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
        /// 获取由 TurnBack 原始骨架根旋转提取出的累计转向曲线。
        /// 横轴和纵轴均为零到一，只有需要脚本驱动朝向的一百八十度转身动画会配置。
        /// </summary>
        public AnimationCurve AuthoredTurnCurve => _authoredTurnCurve;

        /// <summary>
        /// 获取取消窗口开始的归一化动画进度。
        /// </summary>
        public float CancelStartNormalizedTime => _cancelStartNormalizedTime;

        /// <summary>
        /// 获取取消窗口结束的归一化动画进度。
        /// </summary>
        public float CancelEndNormalizedTime => _cancelEndNormalizedTime;

        /// <summary>
        /// 获取取消窗口内允许接管当前动画的行为集合。
        /// </summary>
        public PlayerAnimationCancelTarget CancelTargets => _cancelTargets;

        /// <summary>
        /// 获取该定义是否已经写入动作取消时机数据。
        /// </summary>
        public bool HasActionTimingConfigured => _hasActionTimingConfigured;

        /// <summary>
        /// 获取该定义是否已经写入统一取消规则。
        /// 旧目录迁移时会使用默认规则，新目录则保留 Inspector 中的手动调参。
        /// </summary>
        public bool HasCancelRuleConfigured => _hasCancelRuleConfigured;

        /// <summary>
        /// 获取该定义是否已经写入新版位移策略数据。
        /// </summary>
        public bool HasMovementPolicyConfigured => _hasMovementPolicyConfigured;

        /// <summary>
        /// 初始化由编辑器自动收录的动画定义。
        /// </summary>
        public PlayerAnimationDefinition(
            PlayerAnimationId id,
            PlayerAnimationCategory category,
            TransitionAssetBase transition,
            PlayerAnimationStateKind stateKind,
            PlayerAnimationMovementPolicy movementPolicy,
            float rootMotionPositionScale,
            float authoredMotionDistance,
            AnimationCurve authoredMotionCurve,
            AnimationCurve authoredTurnCurve,
            float cancelStartNormalizedTime,
            float cancelEndNormalizedTime,
            PlayerAnimationCancelTarget cancelTargets)
        {
            _id = id;
            _category = category;
            _transition = transition;
            _stateKind = stateKind;
            _movementPolicy = movementPolicy;
            _rootMotionPositionScale = rootMotionPositionScale;
            _authoredMotionDistance = authoredMotionDistance;
            _authoredMotionCurve = authoredMotionCurve;
            _authoredTurnCurve = authoredTurnCurve;
            _cancelStartNormalizedTime = Mathf.Clamp01(cancelStartNormalizedTime);
            _cancelEndNormalizedTime = Mathf.Max(
                _cancelStartNormalizedTime,
                Mathf.Clamp01(cancelEndNormalizedTime));
            _cancelTargets = cancelTargets;
            _hasActionTimingConfigured = true;
            _hasCancelRuleConfigured = true;
            _useRootMotion = movementPolicy == PlayerAnimationMovementPolicy.RootMotion;
            _hasMovementPolicyConfigured = true;
        }

        /// <summary>
        /// 判断指定行为能否在当前动画进度接管播放。
        /// </summary>
        /// <param name="target">希望切换到的行为类型。</param>
        /// <param name="normalizedTime">当前动画归一化播放进度。</param>
        /// <returns>目标被允许且当前进度位于取消窗口内时返回 true。</returns>
        public bool CanCancelTo(
            PlayerAnimationCancelTarget target,
            float normalizedTime)
        {
            return (_cancelTargets & target) != 0 &&
                   normalizedTime >= _cancelStartNormalizedTime &&
                   normalizedTime <= _cancelEndNormalizedTime;
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

        /// <summary>
        /// 根据 TurnBack 当前归一化进度计算 Player 逻辑根应完成的转向比例。
        /// 曲线缺失时使用平滑插值作为安全回退，避免角色停留在未完成朝向。
        /// </summary>
        /// <param name="normalizedTime">TurnBack 当前零到一的动画进度。</param>
        /// <returns>绝对值表示完成比例，正负号表示源动画选择的转身方向。</returns>
        public float EvaluateAuthoredTurnProgress(float normalizedTime)
        {
            var clampedTime = Mathf.Clamp01(normalizedTime);
            var progress = _authoredTurnCurve != null &&
                           _authoredTurnCurve.length > 0
                ? _authoredTurnCurve.Evaluate(clampedTime)
                : Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(clampedTime / 0.60f));
            return Mathf.Clamp(progress, -1f, 1f);
        }
    }
}
