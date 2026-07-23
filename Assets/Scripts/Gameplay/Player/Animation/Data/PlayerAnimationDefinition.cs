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
        [SerializeField] private PlayerAnimationMovementPolicy _movementPolicy;
        [SerializeField, Min(0f)] private float _rootMotionPositionScale = 1f;
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
            PlayerAnimationMovementPolicy movementPolicy,
            float rootMotionPositionScale,
            bool canBeInterrupted)
        {
            _id = id;
            _category = category;
            _clip = clip;
            _loop = loop;
            _fadeDuration = fadeDuration;
            _movementPolicy = movementPolicy;
            _rootMotionPositionScale = rootMotionPositionScale;
            _useRootMotion = movementPolicy == PlayerAnimationMovementPolicy.RootMotion;
            _hasMovementPolicyConfigured = true;
            _canBeInterrupted = canBeInterrupted;
        }
    }
}
