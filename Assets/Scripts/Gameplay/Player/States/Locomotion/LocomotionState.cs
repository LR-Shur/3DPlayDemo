using Train.Gameplay.Player.Core;
using Train.Gameplay.Player.States;

namespace Train.Gameplay.Player.States.Locomotion
{
    /// <summary>
    /// 表示顶层自由移动阶段，并持有待机、步行和奔跑子状态。
    /// 此阶段也是唯一接受新翻滚和攻击请求的阶段。
    /// </summary>
    public sealed class LocomotionState : PlayerState
    {
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
            if (Context.Input.ConsumeDodgePressed())
            {
                PlayerMachine.ChangeState(new DodgeState(PlayerMachine, Context));
                return;
            }

            if (Context.Input.ConsumeAttackPressed())
            {
                PlayerMachine.ChangeState(new AttackState(PlayerMachine, Context));
                return;
            }

            base.Tick();
        }

        /// <summary>
        /// 重新评估输入强度和冲刺状态，以选择待机、步行或奔跑。
        /// </summary>
        public void RefreshLocomotionChild()
        {
            if (Context.Input.Move.sqrMagnitude <= Context.Config.InputDeadZone * Context.Config.InputDeadZone)
            {
                if (!(ChildState is IdleState))
                {
                    SetChildState(new IdleState(PlayerMachine, Context, this));
                }

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
                SetChildState(new WalkState(PlayerMachine, Context, this));
            }
        }
    }
}
