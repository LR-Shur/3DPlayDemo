using Train.Gameplay.Player.Animation.Data;
using Train.Gameplay.Player.Core;

namespace Train.Gameplay.Player.States.Locomotion
{
    /// <summary>
    /// 播放从静止进入步行时的起步动画。
    /// 起步期间保持代码移动，并允许松键、冲刺或方向变化立即接管。
    /// </summary>
    public sealed class WalkStartState : PlayerState
    {
        private readonly LocomotionState _parent;

        /// <summary>
        /// 初始化步行起步子状态。
        /// </summary>
        /// <param name="machine">所属玩家状态机。</param>
        /// <param name="context">玩家共享上下文。</param>
        /// <param name="parent">拥有该子状态的自由移动父状态。</param>
        public WalkStartState(
            PlayerStateMachine machine,
            PlayerContext context,
            LocomotionState parent)
            : base(machine, context)
        {
            _parent = parent;
        }

        /// <summary>
        /// 配置代码位移并播放一次步行起步动画。
        /// </summary>
        public override void Enter()
        {
            ConfigureAnimationMovement(PlayerAnimationId.Walk_Start);
            Context.Animation.PlayOneShot(
                PlayerAnimationId.Walk_Start,
                OnAnimationEnded);
        }

        /// <summary>
        /// 起步期间按步行速度响应输入；松键立即切换起步收势，按住冲刺则直接进入奔跑。
        /// </summary>
        public override void Tick()
        {
            if (!_parent.HasMovementInput())
            {
                _parent.EnterWalkStop(PlayerAnimationId.Walk_Start_End);
                return;
            }

            if (Context.Input.IsSprintHeld)
            {
                _parent.EnterRun();
                return;
            }

            Context.Motor.MoveCameraRelative(
                Context.Input.Move,
                Context.Config.WalkSpeed);
        }

        /// <summary>
        /// 起步动画自然结束后依据最新输入进入待机、步行或奔跑。
        /// </summary>
        private void OnAnimationEnded()
        {
            if (_parent.IsCurrentChild(this))
            {
                _parent.ResolveLatestLocomotion();
            }
        }
    }
}
