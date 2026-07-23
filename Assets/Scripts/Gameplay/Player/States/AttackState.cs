using Train.Gameplay.Player.Core;
using Train.Gameplay.Player.Animation.Data;
using Train.Gameplay.Player.States.Locomotion;

namespace Train.Gameplay.Player.States
{
    /// <summary>
    /// 表示第一版基础攻击实现。
    /// 角色会保持原地状态，直到配置的攻击动画结束。
    /// </summary>
    public sealed class AttackState : PlayerState
    {
        /// <summary>
        /// 初始化攻击状态。
        /// </summary>
        public AttackState(PlayerStateMachine machine, PlayerContext context)
            : base(machine, context)
        {
        }

        /// <summary>
        /// 停止脚本水平移动，并开始播放配置的攻击动画。
        /// </summary>
        public override void Enter()
        {
            Context.Input.ClearBufferedButtons();
            var animationId = PlayerAnimationId.Attack_Normal_01_01;
            ConfigureAnimationMovement(animationId);
            Context.Animation.PlayOneShot(animationId, OnAttackAnimationEnded);
        }

        /// <summary>
        /// 第一版基础攻击期间刻意保持状态不可被打断。
        /// </summary>
        public override void Tick()
        {
            Context.Motor.MoveVerticalOnly();
        }

        /// <summary>
        /// 当前攻击动画结束后返回自由移动状态。
        /// </summary>
        private void OnAttackAnimationEnded()
        {
            if (ReferenceEquals(PlayerMachine.CurrentState, this))
            {
                PlayerMachine.ChangeState(new LocomotionState(PlayerMachine, Context));
            }
        }

    }
}
