using Train.Gameplay.Player.Core;
using Train.Gameplay.Player.Animation.Data;

namespace Train.Gameplay.Player.States.Locomotion
{
    /// <summary>
    /// 表示使用配置步行速度的移动子状态。
    /// 移动组件会处理相对相机的前进、后退和侧移输入。
    /// </summary>
    public sealed class WalkState : PlayerState
    {
        private readonly LocomotionState _parent;

        /// <summary>
        /// 使用所属移动父状态初始化步行子状态。
        /// </summary>
        public WalkState(PlayerStateMachine machine, PlayerContext context, LocomotionState parent)
            : base(machine, context)
        {
            _parent = parent;
        }

        /// <summary>
        /// 开始播放步行动画。
        /// </summary>
        public override void Enter()
        {
            ConfigureAnimationMovement(PlayerAnimationId.Walk);
            Context.Animation.PlayLoop(PlayerAnimationId.Walk);
        }

        /// <summary>
        /// 以步行速度移动，并重新评估所需的移动子状态。
        /// </summary>
        public override void Tick()
        {
            Context.Motor.MoveCameraRelative(Context.Input.Move, Context.Config.WalkSpeed);
            _parent.RefreshMovingLoop();
        }
    }
}
