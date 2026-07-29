using Train.Gameplay.Player.Animation.Data;
using Train.Gameplay.Player.Core;
using Train.Gameplay.Player.States;

namespace Train.Gameplay.Player.States.Locomotion
{
    /// <summary>
    /// 表示顶层自由移动阶段，并管理待机、起步、持续移动和收步子状态。
    /// 本状态不会请求 TurnBack，改变输入方向时始终由代码移动立即更新角色朝向。
    /// </summary>
    public sealed class LocomotionState : PlayerState
    {
        /// <summary>
        /// 初始化自由移动父状态。
        /// </summary>
        /// <param name="machine">所属玩家状态机。</param>
        /// <param name="context">玩家共享上下文。</param>
        public LocomotionState(PlayerStateMachine machine, PlayerContext context)
            : base(machine, context)
        {
        }

        /// <summary>
        /// 根据进入自由移动状态时的最新输入选择待机或持续移动。
        /// 从攻击、翻滚等顶层状态返回时直接恢复移动循环，避免重复播放起步动画。
        /// </summary>
        public override void Enter()
        {
            ResolveLatestLocomotion();
        }

        /// <summary>
        /// 优先处理能够离开自由移动阶段的顶层输入，然后更新当前移动子状态。
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
        /// 由步行或奔跑循环重新评估停止、步行和奔跑切换。
        /// 停止时播放对应收步动画，步行与奔跑之间则直接交接。
        /// </summary>
        public void RefreshMovingLoop()
        {
            if (!HasMovementInput())
            {
                if (ChildState is RunState)
                {
                    EnterRunStop();
                    return;
                }

                if (ChildState is WalkState)
                {
                    EnterWalkStop(PlayerAnimationId.Walk_End);
                    return;
                }

                EnterIdle();
                return;
            }

            EnterMovingLoop();
        }

        /// <summary>
        /// 根据最新输入选择待机、步行或奔跑循环。
        /// </summary>
        public void ResolveLatestLocomotion()
        {
            if (!HasMovementInput())
            {
                EnterIdle();
                return;
            }

            EnterMovingLoop();
        }

        /// <summary>
        /// 进入待机子状态。
        /// </summary>
        public void EnterIdle()
        {
            if (!(ChildState is IdleState))
            {
                SetChildState(new IdleState(PlayerMachine, Context, this));
            }
        }

        /// <summary>
        /// 从待机或一次收步重新按下移动键时进入步行起步状态。
        /// 冲刺键已按住时直接进入奔跑，避免先闪过一帧步行起步。
        /// </summary>
        public void EnterMovementFromRest()
        {
            if (Context.Input.IsSprintHeld)
            {
                EnterRun();
                return;
            }

            EnterWalkStart();
        }

        /// <summary>
        /// 进入步行起步子状态。
        /// </summary>
        public void EnterWalkStart()
        {
            if (!(ChildState is WalkStartState))
            {
                SetChildState(new WalkStartState(PlayerMachine, Context, this));
            }
        }

        /// <summary>
        /// 使用指定动画进入步行收步子状态。
        /// </summary>
        /// <param name="animationId">需要播放的步行收步动画。</param>
        public void EnterWalkStop(PlayerAnimationId animationId)
        {
            SetChildState(new WalkStopState(
                PlayerMachine,
                Context,
                this,
                animationId));
        }

        /// <summary>
        /// 进入奔跑收步子状态。
        /// </summary>
        public void EnterRunStop()
        {
            if (!(ChildState is RunStopState))
            {
                SetChildState(new RunStopState(PlayerMachine, Context, this));
            }
        }

        /// <summary>
        /// 进入步行循环子状态。
        /// </summary>
        public void EnterWalk()
        {
            if (!(ChildState is WalkState))
            {
                SetChildState(new WalkState(PlayerMachine, Context, this));
            }
        }

        /// <summary>
        /// 进入奔跑循环子状态。
        /// </summary>
        public void EnterRun()
        {
            if (!(ChildState is RunState))
            {
                SetChildState(new RunState(PlayerMachine, Context, this));
            }
        }

        /// <summary>
        /// 根据冲刺键选择步行或奔跑循环。
        /// </summary>
        public void EnterMovingLoop()
        {
            if (Context.Input.IsSprintHeld)
            {
                EnterRun();
                return;
            }

            EnterWalk();
        }

        /// <summary>
        /// 判断指定状态是否仍是当前移动子状态，防止旧动画结束回调覆盖新输入。
        /// </summary>
        /// <param name="state">需要检查的子状态实例。</param>
        /// <returns>该实例仍处于激活状态时返回 true。</returns>
        public bool IsCurrentChild(PlayerState state)
        {
            return ReferenceEquals(ChildState, state);
        }

        /// <summary>
        /// 判断当前输入是否超过配置的移动死区。
        /// </summary>
        /// <returns>存在有效移动输入时返回 true。</returns>
        public bool HasMovementInput()
        {
            return Context.Input.Move.sqrMagnitude >
                   Context.Config.InputDeadZone * Context.Config.InputDeadZone;
        }
    }
}
