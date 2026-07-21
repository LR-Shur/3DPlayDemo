using Train.Gameplay.Player.Core;
using Train.Gameplay.Player.Movement;
using Train.Gameplay.Player.States.Locomotion;
using UnityEngine;

namespace Train.Gameplay.Player.States
{
    /// <summary>
    /// 表示不可被打断的翻滚状态；该状态锁定方向并使用动画 Root Motion 产生位移。
    /// 第一版会刻意忽略翻滚期间按下的输入。
    /// </summary>
    public sealed class DodgeState : PlayerState
    {
        private Vector3 _dodgeDirection;

        /// <summary>
        /// 初始化翻滚状态。
        /// </summary>
        public DodgeState(PlayerStateMachine machine, PlayerContext context)
            : base(machine, context)
        {
        }

        /// <summary>
        /// 锁定翻滚方向、将移动组件切换至 Root Motion，并开始播放翻滚动画。
        /// </summary>
        public override void Enter()
        {
            _dodgeDirection = Context.Motor.GetCameraRelativeDirection(Context.Input.Move);
            if (_dodgeDirection.sqrMagnitude <= 0.0001f)
            {
                _dodgeDirection = Context.Motor.Forward;
            }

            Context.Input.ClearBufferedButtons();
            Context.Motor.SetMovementMode(PlayerMovementMode.AnimationRootMotion);
            Context.Animation.PlayDodge(_dodgeDirection, OnDodgeAnimationEnded);
        }

        /// <summary>
        /// 在翻滚动画结束前刻意忽略移动和行为请求。
        /// </summary>
        public override void Tick()
        {
        }

        /// <summary>
        /// 在激活另一个顶层状态前恢复脚本移动。
        /// </summary>
        public override void Exit()
        {
            Context.Motor.SetMovementMode(PlayerMovementMode.Scripted);
            base.Exit();
        }

        /// <summary>
        /// 当前翻滚动画结束后返回自由移动状态。
        /// </summary>
        private void OnDodgeAnimationEnded()
        {
            if (ReferenceEquals(PlayerMachine.CurrentState, this))
            {
                PlayerMachine.ChangeState(new LocomotionState(PlayerMachine, Context));
            }
        }
    }
}
