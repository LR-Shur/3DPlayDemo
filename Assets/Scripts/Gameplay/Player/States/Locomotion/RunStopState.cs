using Train.Gameplay.Player.Animation.Data;
using Train.Gameplay.Player.Core;

namespace Train.Gameplay.Player.States.Locomotion
{
    /// <summary>
    /// 播放奔跑停止时的收步动画。
    /// 收步期间保持角色原地，新移动输入能够立即恢复步行起步或奔跑。
    /// </summary>
    public sealed class RunStopState : PlayerState
    {
        private readonly LocomotionState _parent;

        /// <summary>
        /// 初始化奔跑收步子状态。
        /// </summary>
        /// <param name="machine">所属玩家状态机。</param>
        /// <param name="context">玩家共享上下文。</param>
        /// <param name="parent">拥有该子状态的自由移动父状态。</param>
        public RunStopState(
            PlayerStateMachine machine,
            PlayerContext context,
            LocomotionState parent)
            : base(machine, context)
        {
            _parent = parent;
        }

        /// <summary>
        /// 配置动画位移策略并播放一次奔跑收步动画。
        /// </summary>
        public override void Enter()
        {
            ConfigureAnimationMovement(PlayerAnimationId.Run_End);
            Context.Animation.PlayOneShot(
                PlayerAnimationId.Run_End,
                OnAnimationEnded);
        }

        /// <summary>
        /// 没有输入时只保留重力；重新按下移动键时立即恢复移动。
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
