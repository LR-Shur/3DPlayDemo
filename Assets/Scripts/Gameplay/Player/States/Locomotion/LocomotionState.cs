using Train.Gameplay.Player.Core;
using Train.Gameplay.Player.States;
using Train.Gameplay.Player.Animation.Data;
using UnityEngine;

namespace Train.Gameplay.Player.States.Locomotion
{
    /// <summary>
    /// 表示顶层自由移动阶段，并持有待机、步行和奔跑子状态。
    /// 此阶段也是唯一接受新翻滚和攻击请求的阶段。
    /// </summary>
    public sealed class LocomotionState : PlayerState
    {
        private const float TurnBackDotThreshold = -0.7f;

        /// <summary>
        /// 初始化自由移动父状态。
        /// </summary>
        public LocomotionState(PlayerStateMachine machine, PlayerContext context)
            : base(machine, context)
        {
        }

        /// <summary>
        /// 选择合适的初始移动子状态。
        /// 具体子状态会依据其动画定义配置对应的位移策略。
        /// </summary>
        public override void Enter()
        {
            RefreshLocomotionChild();
        }

        /// <summary>
        /// 在更新当前移动子状态前检查顶层行为请求。
        /// </summary>
        public override void Tick()
        {
            if (Context.Input.ConsumeJumpPressed())
            {
                Context.Motor.TryJump(Context.Config.JumpHeight);
            }

            if (Context.Input.ConsumeDodgePressed())
            {
                PlayerMachine.ChangeState(new DodgeState(PlayerMachine, Context));
                return;
            }

            if (Context.Input.ConsumeCrouchPressed())
            {
                PlayerMachine.ChangeState(new PlayerCombatSequenceState(
                    PlayerMachine,
                    Context,
                    PlayerCombatSequenceId.ParryCounter));
                return;
            }

            if (Context.Input.ConsumePreviousPressed())
            {
                PlayerMachine.ChangeState(new PlayerCombatSequenceState(
                    PlayerMachine,
                    Context,
                    PlayerCombatSequenceId.Branch));
                return;
            }

            if (Context.Input.ConsumeNextPressed())
            {
                PlayerMachine.ChangeState(new PlayerCombatSequenceState(
                    PlayerMachine,
                    Context,
                    PlayerCombatSequenceId.Assault));
                return;
            }

            if (Context.Input.ConsumeRushPressed())
            {
                PlayerMachine.ChangeState(new PlayerCombatSequenceState(
                    PlayerMachine,
                    Context,
                    PlayerCombatSequenceId.Rush));
                return;
            }

            if (Context.Input.ConsumeInteractPressed())
            {
                PlayerMachine.ChangeState(new PlayerActionState(
                    PlayerMachine,
                    Context,
                    PlayerAnimationId.QuestStart,
                    true));
                return;
            }

            if (Context.Input.ConsumeAttackPressed())
            {
                PlayerMachine.ChangeState(Context.Input.IsSprintHeld
                    ? new PlayerCombatSequenceState(
                        PlayerMachine,
                        Context,
                        PlayerCombatSequenceId.Dash)
                    : new AttackState(PlayerMachine, Context));
                return;
            }

            base.Tick();
        }

        /// <summary>
        /// 重新评估输入强度和冲刺状态，以选择待机、步行或奔跑。
        /// </summary>
        public void RefreshLocomotionChild()
        {
            if (ChildState is LocomotionTransitionState)
            {
                return;
            }

            if (!HasMovementInput())
            {
                if (ChildState is WalkState)
                {
                    BeginTransition(PlayerAnimationId.Walk_End, LocomotionTransitionType.Stop);
                    return;
                }

                if (ChildState is RunState)
                {
                    BeginTransition(PlayerAnimationId.Run_End, LocomotionTransitionType.Stop);
                    return;
                }

                EnterIdle();
                return;
            }

            if (ChildState is IdleState)
            {
                BeginTransition(PlayerAnimationId.Walk_Start, LocomotionTransitionType.Start);
                return;
            }

            if (Context.Input.IsSprintHeld)
            {
                if (!(ChildState is RunState))
                {
                    SetChildState(new RunState(PlayerMachine, Context, this));
                }

                return;
            }

            if (!(ChildState is WalkState))
            {
                if (ChildState is RunState)
                {
                    BeginTransition(PlayerAnimationId.Run_End, LocomotionTransitionType.Stop);
                }
                else
                {
                    SetChildState(new WalkState(PlayerMachine, Context, this));
                }
            }
        }

        /// <summary>
        /// 在步行或奔跑子状态开始实际移动前检查本次输入是否需要播放转身动画。
        /// </summary>
        /// <returns>已开始转身过渡时返回 true，调用方应停止本帧普通移动。</returns>
        public bool TryStartTurnBack()
        {
            if (!HasMovementInput())
            {
                return false;
            }

            var inputDirection = Context.Motor.GetCameraRelativeDirection(Context.Input.Move);
            if (inputDirection.sqrMagnitude <= 0.0001f ||
                Vector3.Dot(Context.Motor.Forward, inputDirection) > TurnBackDotThreshold)
            {
                return false;
            }

            BeginTransition(PlayerAnimationId.TurnBack, LocomotionTransitionType.Turn);
            return true;
        }

        /// <summary>
        /// 在移动过渡动画结束时依据最新输入选择正确的后续移动子状态。
        /// </summary>
        /// <param name="transitionState">触发回调的过渡子状态。</param>
        /// <param name="transitionType">已完成的过渡类别。</param>
        public void CompleteTransition(
            LocomotionTransitionState transitionState,
            LocomotionTransitionType transitionType)
        {
            if (!ReferenceEquals(ChildState, transitionState))
            {
                return;
            }

            if (transitionType == LocomotionTransitionType.Turn)
            {
                Context.Motor.FaceDirectionImmediately(
                    Context.Motor.GetCameraRelativeDirection(Context.Input.Move));
            }

            if (!HasMovementInput())
            {
                if (transitionType == LocomotionTransitionType.Start)
                {
                    BeginTransition(PlayerAnimationId.Walk_Start_End, LocomotionTransitionType.Stop);
                    return;
                }

                EnterIdle();
                return;
            }

            if (Context.Input.IsSprintHeld)
            {
                SetChildState(new RunState(PlayerMachine, Context, this));
                return;
            }

            SetChildState(new WalkState(PlayerMachine, Context, this));
        }

        /// <summary>
        /// 根据最新移动输入抢占正在播放的起步或停步动画。
        /// 这样短按移动键后再次按下时不会继续等待旧的过渡动画自然结束。
        /// </summary>
        /// <param name="transitionState">当前正在播放的移动过渡子状态。</param>
        /// <param name="transitionType">当前过渡动画的逻辑用途。</param>
        /// <returns>已替换当前子状态时返回 true，调用方应停止本帧旧状态逻辑。</returns>
        public bool TryHandleTransitionInput(
            LocomotionTransitionState transitionState,
            LocomotionTransitionType transitionType)
        {
            if (!ReferenceEquals(ChildState, transitionState))
            {
                return true;
            }

            var hasMovementInput = HasMovementInput();
            if (transitionType == LocomotionTransitionType.Start && !hasMovementInput)
            {
                BeginTransition(PlayerAnimationId.Walk_Start_End, LocomotionTransitionType.Stop);
                return true;
            }

            if (transitionType == LocomotionTransitionType.Stop && hasMovementInput)
            {
                EnterMovingLoop();
                return true;
            }

            if (transitionType == LocomotionTransitionType.Turn && !hasMovementInput)
            {
                EnterIdle();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 切换到指定的一次性移动过渡动画子状态。
        /// </summary>
        /// <param name="animationId">需要播放的移动过渡动画。</param>
        /// <param name="transitionType">该过渡的逻辑用途。</param>
        private void BeginTransition(
            PlayerAnimationId animationId,
            LocomotionTransitionType transitionType)
        {
            SetChildState(new LocomotionTransitionState(
                PlayerMachine,
                Context,
                this,
                animationId,
                transitionType));
        }

        /// <summary>
        /// 切换到待机子状态，避免重复创建同类型状态。
        /// </summary>
        private void EnterIdle()
        {
            if (!(ChildState is IdleState))
            {
                SetChildState(new IdleState(PlayerMachine, Context, this));
            }
        }

        /// <summary>
        /// 根据当前冲刺输入直接进入步行或奔跑循环，用于抢占已经失效的停步过渡。
        /// </summary>
        private void EnterMovingLoop()
        {
            if (Context.Input.IsSprintHeld)
            {
                SetChildState(new RunState(PlayerMachine, Context, this));
                return;
            }

            SetChildState(new WalkState(PlayerMachine, Context, this));
        }

        /// <summary>
        /// 判断当前输入是否超过配置的移动死区。
        /// </summary>
        /// <returns>存在有效移动输入时返回 true。</returns>
        private bool HasMovementInput()
        {
            return Context.Input.Move.sqrMagnitude >
                   Context.Config.InputDeadZone * Context.Config.InputDeadZone;
        }
    }
}
