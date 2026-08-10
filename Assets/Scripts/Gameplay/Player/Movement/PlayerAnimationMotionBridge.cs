using UnityEngine;

namespace Train.Gameplay.Player.Movement
{
    /// <summary>
    /// 在 Animator 完成动画求值后读取其根运动增量，并交给逻辑根节点上的 PlayerMotor。
    /// 该组件只传递 Animator.deltaPosition，不读取或重置模型 Transform，因此不会制造闪现或父子节点分离。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerAnimationMotionBridge : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private PlayerMotor _motor;

        [Header("运行时根运动调试（播放时查看）")]
        [SerializeField, Tooltip("Animator 在上一帧产生的世界空间根运动位移。")]
        private Vector3 _lastDeltaPosition;
        private bool _hasReportedMissingReference;

        /// <summary>
        /// 在动画开始求值前缓存 Animator 与父级玩家移动组件。
        /// </summary>
        private void Awake()
        {
            EnsureReferences();
        }

        /// <summary>
        /// 在检查器引用改变时自动回填 Animator 与父级玩家移动组件。
        /// </summary>
        private void OnValidate()
        {
            EnsureReferences();
        }

        /// <summary>
        /// 接管 Animator 的根运动回调，并把本帧位移增量交给 Rigidbody 移动链。
        /// PlayerMotor 会依据当前动画策略决定忽略、缩放或应用这段位移。
        /// </summary>
        private void OnAnimatorMove()
        {
            if (!EnsureReferences())
            {
                return;
            }

            _lastDeltaPosition = _animator.deltaPosition;
            _motor.ApplyRootMotion(_lastDeltaPosition);
        }

        /// <summary>
        /// 确保动画桥接器拥有同对象上的 Animator 和父级逻辑根上的 PlayerMotor。
        /// </summary>
        /// <returns>两个必需引用均已取得时返回 true。</returns>
        private bool EnsureReferences()
        {
            _animator ??= GetComponent<Animator>();
            _motor ??= GetComponentInParent<PlayerMotor>();
            if (_animator != null && _motor != null)
            {
                _hasReportedMissingReference = false;
                return true;
            }

            if (!_hasReportedMissingReference)
            {
                Debug.LogError("PlayerAnimationMotionBridge 缺少 Animator 或父级 PlayerMotor 引用。", this);
                _hasReportedMissingReference = true;
            }

            return false;
        }
    }
}
