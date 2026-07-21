using UnityEngine;

namespace Train.Gameplay.Player.Movement
{
    /// <summary>
    /// 在 Animator 所在对象上接收 Root Motion，并将其转交给玩家的碰撞感知移动组件。
    /// 这样可以避免动画直接修改 Transform 的位置。
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerRootMotionReceiver : MonoBehaviour
    {
        [SerializeField] private PlayerMotor _motor;

        private Animator _animator;

        /// <summary>
        /// 启用 Animator Root Motion，并在可能时查找玩家移动组件。
        /// </summary>
        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _motor ??= GetComponentInParent<PlayerMotor>();
            _animator.applyRootMotion = true;
        }

        /// <summary>
        /// 将当前帧的动画位移转交给玩家移动组件。
        /// </summary>
        private void OnAnimatorMove()
        {
            if (_motor != null)
            {
                _motor.ApplyRootMotion(_animator.deltaPosition);
            }
        }
    }
}
