using Train.Gameplay.Player.Animation.Data;
using Train.Gameplay.Player.Core;
using Train.Gameplay.Player.States.Locomotion;
using UnityEngine;

namespace Train.Gameplay.Player.States
{
    /// <summary>
    /// 播放任意非移动类动作的通用状态。
    /// 攻击派生、受击、交互、切换和剧情动画均可复用此状态，避免为每个动画创建重复脚本。
    /// </summary>
    public sealed class PlayerActionState : PlayerState
    {
        private readonly PlayerAnimationId _animationId;
        private readonly bool _returnToLocomotionOnEnd;
        private PlayerAnimationDefinition _animationDefinition;

        /// <summary>
        /// 使用待播放的动画和结束后的返回规则初始化通用动作状态。
        /// </summary>
        /// <param name="machine">所属玩家状态机。</param>
        /// <param name="context">玩家共享上下文。</param>
        /// <param name="animationId">需要播放的动作动画标识。</param>
        /// <param name="returnToLocomotionOnEnd">动画结束后是否返回自由移动状态。</param>
        public PlayerActionState(
            PlayerStateMachine machine,
            PlayerContext context,
            PlayerAnimationId animationId,
            bool returnToLocomotionOnEnd)
            : base(machine, context)
        {
            _animationId = animationId;
            _returnToLocomotionOnEnd = returnToLocomotionOnEnd;
        }

        /// <summary>
        /// 读取动作定义并开始播放；循环动作会保持到外部请求停止。
        /// </summary>
        public override void Enter()
        {
            Context.Input.ClearBufferedButtons();
            if (!Context.Animation.TryGetDefinition(_animationId, out _animationDefinition))
            {
                ReturnToLocomotion();
                return;
            }

            ConfigureAnimationMovement(_animationId);
            if (_animationDefinition.Loop)
            {
                Context.Animation.PlayLoop(_animationId);
                return;
            }

            Context.Animation.PlayOneShot(_animationId, OnAnimationEnded);
        }

        /// <summary>
        /// 保持重力生效，并在定义允许时让移动输入提前结束动作。
        /// </summary>
        public override void Tick()
        {
            Context.Motor.MoveVerticalOnly();
            if (_animationDefinition != null &&
                _animationDefinition.CanCancelTo(
                    PlayerAnimationCancelTarget.Movement,
                    Context.Animation.CurrentAnimationNormalizedTime) &&
                Context.Input.Move.sqrMagnitude > Context.Config.InputDeadZone * Context.Config.InputDeadZone)
            {
                ReturnToLocomotion();
            }
        }

        /// <summary>
        /// 处理一次性动作自然播放结束后的状态去向。
        /// </summary>
        private void OnAnimationEnded()
        {
            if (_returnToLocomotionOnEnd)
            {
                ReturnToLocomotion();
            }
        }

        /// <summary>
        /// 在仍处于本状态时返回自由移动，避免过期动画回调覆盖后续状态。
        /// </summary>
        private void ReturnToLocomotion()
        {
            if (ReferenceEquals(PlayerMachine.CurrentState, this))
            {
                PlayerMachine.ChangeState(new LocomotionState(PlayerMachine, Context));
            }
        }
    }
}
