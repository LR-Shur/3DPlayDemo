using Train.Gameplay.Player.Animation.Data;
using Train.Gameplay.Player.Core;

namespace Train.Gameplay.Player.States.Locomotion
{
    /// <summary>
    /// 播放步行结束或起步中途松键时的收步动画。
    /// 收步期间不产生水平位移，新移动输入可以立即切换到新的起步或奔跑状态。
    /// </summary>
    public sealed class WalkStopState : PlayerState
    {
        private readonly LocomotionState _parent;
        private readonly PlayerAnimationId _animationId;

        /// <summary>
        /// 初始化步行收步子状态。
        /// </summary>
        /// <param name="machine">所属玩家状态机。</param>
        /// <param name="context">玩家共享上下文。</param>
        /// <param name="parent">拥有该子状态的自由移动父状态。</param>
        /// <param name="animationId">需要播放的 Walk_End 或 Walk_Start_End。</param>
        public WalkStopState(
            PlayerStateMachine machine,
            PlayerContext context,
            LocomotionState parent,
            PlayerAnimationId animationId)
            : base(machine, context)
        {
            _parent = parent;
            _animationId = animationId;
        }

        /// <summary>
        /// 配置动画位移策略并播放一次收步动画。
        /// </summary>
        public override void Enter()
        {
            ConfigureAnimationMovement(_animationId);
            Context.Animation.PlayOneShot(_animationId, OnAnimationEnded);
        }

        /// <summary>
        /// 没有输入时只保留重力；重新按下移动键时立即由新的移动状态接管。
        /// </summary>
        public override void Tick()
        {
            if (_parent.HasMovementInput())
            {
                _parent.EnterMovementFromRest();
                return;
            }

            Context.Motor.MoveVerticalOnly();
        }

        /// <summary>
        /// 收步动画自然结束后进入待机；已被新状态替换时忽略旧回调。
        /// </summary>
        private void OnAnimationEnded()
        {
            if (_parent.IsCurrentChild(this))
            {
                _parent.EnterIdle();
            }
        }
    }
}
