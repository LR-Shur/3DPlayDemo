using Train.Gameplay.Player.Core;
using Train.Gameplay.Player.Animation.Data;

namespace Train.Gameplay.Player.States.Locomotion
{
    /// <summary>
    /// 表示冲刺被按住时使用配置奔跑速度的移动子状态。
    /// </summary>
    public sealed class RunState : PlayerState
    {
        private readonly LocomotionState _parent;

        /// <summary>
        /// 使用所属移动父状态初始化奔跑子状态。
        /// </summary>
        public RunState(PlayerStateMachine machine, PlayerContext context, LocomotionState parent)
            : base(machine, context)
        {
            _parent = parent;
        }

        /// <summary>
        /// 开始播放奔跑动画。
        /// </summary>
        public override void Enter()
        {
            ConfigureAnimationMovement(PlayerAnimationId.Run);
            Context.Animation.PlayLoop(PlayerAnimationId.Run);
        }

        /// <summary>
        /// 以奔跑速度移动，并重新评估所需的移动子状态。
        /// </summary>
        public override void Tick()
        {
            Context.Motor.MoveCameraRelative(Context.Input.Move, Context.Config.RunSpeed);
            _parent.RefreshLocomotionChild();
        }
    }
}
