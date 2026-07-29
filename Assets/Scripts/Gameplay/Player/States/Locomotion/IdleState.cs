using Train.Gameplay.Player.Core;
using Train.Gameplay.Player.Animation.Data;

namespace Train.Gameplay.Player.States.Locomotion
{
    /// <summary>
    /// 表示未按住移动输入时的待机移动子状态。
    /// </summary>
    public sealed class IdleState : PlayerState
    {
        private readonly LocomotionState _parent;

        /// <summary>
        /// 使用所属移动父状态初始化待机子状态。
        /// </summary>
        public IdleState(PlayerStateMachine machine, PlayerContext context, LocomotionState parent)
            : base(machine, context)
        {
            _parent = parent;
        }

        /// <summary>
        /// 开始播放待机动画。
        /// </summary>
        public override void Enter()
        {
            ConfigureAnimationMovement(PlayerAnimationId.Idle);
            Context.Animation.PlayLoop(PlayerAnimationId.Idle);
        }

        /// <summary>
        /// 保持重力生效，并在出现输入时切换到移动子状态。
        /// </summary>
        public override void Tick()
        {
            Context.Motor.MoveCameraRelative(Context.Input.Move, 0f);
            if (_parent.HasMovementInput())
            {
                _parent.EnterMovementFromRest();
            }
        }
    }
}
