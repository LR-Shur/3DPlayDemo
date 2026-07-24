using Train.Gameplay.Player.Core;
using Train.Gameplay.Player.Animation.Data;
using Train.Gameplay.Player.States.Locomotion;
using UnityEngine;

namespace Train.Gameplay.Player.States
{
    /// <summary>
    /// 表示锁定方向的翻滚状态；该状态按动画进度执行可调位移曲线。
    /// 翻滚主体阶段不可打断，进入定义的取消窗口后可由移动输入无缝接回自由移动。
    /// </summary>
    public sealed class DodgeState : PlayerState
    {
        private Vector3 _dodgeDirection;
        private PlayerAnimationDefinition _animationDefinition;
        private float _appliedMotionDistance;

        /// <summary>
        /// 初始化翻滚状态。
        /// </summary>
        public DodgeState(PlayerStateMachine machine, PlayerContext context)
            : base(machine, context)
        {
        }

        /// <summary>
        /// 锁定翻滚方向、读取动画位移配置，并开始播放翻滚动画。
        /// </summary>
        public override void Enter()
        {
            _dodgeDirection = Context.Motor.GetCameraRelativeDirection(Context.Input.Move);
            if (_dodgeDirection.sqrMagnitude <= 0.0001f)
            {
                _dodgeDirection = Context.Motor.Forward;
            }

            Context.Input.ClearBufferedButtons();
            var useBackwardDodge = Vector3.Dot(Context.Motor.Forward, _dodgeDirection) < -0.1f;
            var animationId = useBackwardDodge
                ? PlayerAnimationId.Evade_Back
                : PlayerAnimationId.Evade_Front;
            if (!useBackwardDodge)
            {
                Context.Motor.FaceDirectionImmediately(_dodgeDirection);
            }

            Context.Animation.TryGetDefinition(animationId, out _animationDefinition);
            _appliedMotionDistance = 0f;
            ConfigureAnimationMovement(animationId);
            Context.Animation.PlayOneShot(animationId, OnDodgeAnimationEnded);
        }

        /// <summary>
        /// 按实际动画进度执行本帧位移，并在取消窗口开启后检测移动输入。
        /// </summary>
        public override void Tick()
        {
            var normalizedTime = Context.Animation.CurrentAnimationNormalizedTime;
            ApplyAuthoredMotionUntil(normalizedTime);
            if (_animationDefinition != null &&
                normalizedTime >= _animationDefinition.MovementCancelStartNormalizedTime &&
                Context.Input.Move.sqrMagnitude > Context.Config.InputDeadZone * Context.Config.InputDeadZone)
            {
                PlayerMachine.ChangeState(new LocomotionState(PlayerMachine, Context));
            }
        }

        /// <summary>
        /// 当前翻滚动画结束后返回自由移动状态。
        /// </summary>
        private void OnDodgeAnimationEnded()
        {
            ApplyAuthoredMotionUntil(1f);
            if (ReferenceEquals(PlayerMachine.CurrentState, this))
            {
                PlayerMachine.ChangeState(new LocomotionState(PlayerMachine, Context));
            }
        }

        /// <summary>
        /// 将动画位移曲线从上次采样位置推进到指定进度，避免帧率变化改变翻滚总距离。
        /// </summary>
        /// <param name="normalizedTime">当前动画从零到一的归一化播放进度。</param>
        private void ApplyAuthoredMotionUntil(float normalizedTime)
        {
            if (_animationDefinition == null ||
                _animationDefinition.MovementPolicy != PlayerAnimationMovementPolicy.AuthoredMotion)
            {
                return;
            }

            var targetDistance = _animationDefinition.EvaluateAuthoredMotionDistance(normalizedTime);
            var distanceDelta = targetDistance - _appliedMotionDistance;
            Context.Motor.ApplyAuthoredMotion(_dodgeDirection, distanceDelta);
            _appliedMotionDistance = targetDistance;
        }
    }
}
