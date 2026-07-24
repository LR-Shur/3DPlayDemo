using Train.Gameplay.Player.Animation.Data;
using Train.Gameplay.Player.Core;
using UnityEngine;

namespace Train.Gameplay.Player.States.Locomotion
{
    /// <summary>
    /// 播放走路起步、停步和转身等一次性移动过渡动画。
    /// 该状态是 LocomotionState 的子状态，结束后由父状态重新选择待机、步行或奔跑。
    /// </summary>
    public sealed class LocomotionTransitionState : PlayerState
    {
        private readonly LocomotionState _parent;
        private readonly PlayerAnimationId _animationId;
        private readonly LocomotionTransitionType _transitionType;

        /// <summary>
        /// 初始化一次移动过渡动画状态。
        /// </summary>
        /// <param name="machine">所属玩家状态机。</param>
        /// <param name="context">玩家共享上下文。</param>
        /// <param name="parent">拥有该子状态的自由移动父状态。</param>
        /// <param name="animationId">需要播放的移动过渡动画。</param>
        /// <param name="transitionType">过渡结束后使用的行为规则。</param>
        public LocomotionTransitionState(
            PlayerStateMachine machine,
            PlayerContext context,
            LocomotionState parent,
            PlayerAnimationId animationId,
            LocomotionTransitionType transitionType)
            : base(machine, context)
        {
            _parent = parent;
            _animationId = animationId;
            _transitionType = transitionType;
        }

        /// <summary>
        /// 配置动画对应的位移来源，并播放一次性移动过渡动画。
        /// </summary>
        public override void Enter()
        {
            ConfigureAnimationMovement(_animationId);
            Context.Animation.PlayOneShot(_animationId, OnTransitionAnimationEnded);
        }

        /// <summary>
        /// 起步动画期间允许角色按步行速度向输入方向移动，其余过渡仅保持重力。
        /// </summary>
        public override void Tick()
        {
            if (_parent.TryHandleTransitionInput(this, _transitionType))
            {
                return;
            }

            if (_transitionType == LocomotionTransitionType.Start)
            {
                Context.Motor.MoveCameraRelative(Context.Input.Move, Context.Config.WalkSpeed);
                return;
            }

            Context.Motor.MoveVerticalOnly();
        }

        /// <summary>
        /// 由父移动状态根据过渡类型、当前输入和冲刺状态选择下一个子状态。
        /// </summary>
        private void OnTransitionAnimationEnded()
        {
            _parent.CompleteTransition(this, _transitionType);
        }
    }

    /// <summary>
    /// 标识一次移动过渡动画的用途，以便父状态决定结束后的衔接规则。
    /// </summary>
    public enum LocomotionTransitionType
    {
        Start,
        Stop,
        Turn,
    }
}
