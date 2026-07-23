using Train.Gameplay.Player.Core;
using Train.Gameplay.Player.Animation.Data;
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
            var animationId = Vector3.Dot(Context.Motor.Forward, _dodgeDirection) < -0.1f
                ? PlayerAnimationId.Evade_Back
                : PlayerAnimationId.Evade_Front;
            ConfigureAnimationMovement(animationId);
            Context.Animation.PlayOneShot(animationId, OnDodgeAnimationEnded);
        }

        /// <summary>
        /// 在翻滚动画结束前刻意忽略移动和行为请求。
        /// </summary>
        public override void Tick()
        {
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
