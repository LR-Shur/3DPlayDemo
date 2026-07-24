using Train.Gameplay.Player.Animation.Data;
using Train.Gameplay.Player.Core;
using Train.Gameplay.Player.States.Locomotion;
using UnityEngine;

namespace Train.Gameplay.Player.States
{
    /// <summary>
    /// 管理基础普通攻击连段。
    /// 在有效窗口内缓存下一次攻击输入，未续按时尽快释放回移动状态，避免长收势造成后摇。
    /// </summary>
    public sealed class AttackState : PlayerState
    {
        /// <summary>
        /// 普通连击在未缓存下一击时允许回到移动状态的归一化时间。
        /// </summary>
        private const float BasicAttackReleaseNormalizedTime = 0.78f;

        /// <summary>
        /// 终结技在未缓存下一击时允许回到移动状态的归一化时间。
        /// </summary>
        private const float FinisherReleaseNormalizedTime = 0.88f;

        /// <summary>
        /// 基础连击依次播放的主攻击段。
        /// </summary>
        private static readonly PlayerAnimationId[] ComboAnimationIds =
        {
            PlayerAnimationId.Attack_Normal_01_01,
            // PlayerAnimationId.Attack_Normal_01_02,
            PlayerAnimationId.Attack_Normal_02_01,
            // PlayerAnimationId.Attack_Normal_02_02,
        };

        private int _comboIndex;
        private bool _hasQueuedNextAttack;
        private bool _isPlayingFinisher;

        /// <summary>
        /// 初始化基础攻击状态。
        /// </summary>
        /// <param name="machine">所属的玩家状态机。</param>
        /// <param name="context">玩家共享上下文。</param>
        public AttackState(PlayerStateMachine machine, PlayerContext context)
            : base(machine, context)
        {
        }

        /// <summary>
        /// 清空旧输入并播放第一段普通攻击。
        /// </summary>
        public override void Enter()
        {
            Context.Input.ClearBufferedButtons();
            _comboIndex = 0;
            _hasQueuedNextAttack = false;
            _isPlayingFinisher = false;
            PlayCurrentComboAnimation();
        }

        /// <summary>
        /// 缓存续段输入、处理重力，并在未续按时于释放窗口交还移动控制权。
        /// </summary>
        public override void Tick()
        {
            if (Context.Input.ConsumeAttackPressed())
            {
                _hasQueuedNextAttack = true;
            }

            if (!HasContinuationRequest() &&
                Context.Animation.CurrentAnimationNormalizedTime >= GetReleaseNormalizedTime())
            {
                ReturnToLocomotion();
                return;
            }

            Context.Motor.MoveVerticalOnly();
        }

        /// <summary>
        /// 在当前攻击完整结束时衔接已缓存的下一段，或安全返回移动状态。
        /// </summary>
        private void OnAttackAnimationEnded()
        {
            if (HasContinuationRequest() && _comboIndex < ComboAnimationIds.Length - 1)
            {
                _comboIndex++;
                _hasQueuedNextAttack = false;
                PlayCurrentComboAnimation();
                return;
            }

            if (HasContinuationRequest() && _comboIndex == ComboAnimationIds.Length - 1)
            {
                _comboIndex++;
                _hasQueuedNextAttack = false;
                _isPlayingFinisher = true;
                PlayAnimation(SelectFinisherAnimation());
                return;
            }

            ReturnToLocomotion();
        }

        /// <summary>
        /// 播放当前连击索引对应的主攻击动画。
        /// </summary>
        private void PlayCurrentComboAnimation()
        {
            PlayAnimation(ComboAnimationIds[_comboIndex]);
        }

        /// <summary>
        /// 根据当前输入方向和冲刺修饰键选择终结技变体。
        /// </summary>
        /// <returns>本次连段应播放的终结技动画标识。</returns>
        private PlayerAnimationId SelectFinisherAnimation()
        {
            var input = Context.Input.Move;
            var deadZone = Context.Config.InputDeadZone;
            var isBackward = input.y < -deadZone;
            var isNearVariant = Mathf.Abs(input.x) > deadZone;
            var useSecondSet = Context.Input.IsSprintHeld;

            if (isBackward && isNearVariant)
            {
                return useSecondSet
                    ? PlayerAnimationId.Attack_Normal_03_02_Near_Back
                    : PlayerAnimationId.Attack_Normal_03_01_Back_Near;
            }

            if (isBackward)
            {
                return useSecondSet
                    ? PlayerAnimationId.Attack_Normal_03_02_Back
                    : PlayerAnimationId.Attack_Normal_03_01_Back;
            }

            if (isNearVariant)
            {
                return useSecondSet
                    ? PlayerAnimationId.Attack_Normal_03_02_Near
                    : PlayerAnimationId.Attack_Normal_03_01_Near;
            }

            return useSecondSet
                ? PlayerAnimationId.Attack_Normal_03_02
                : PlayerAnimationId.Attack_Normal_03_01;
        }

        /// <summary>
        /// 播放指定攻击动画，并同步采用目录中配置的位移策略。
        /// </summary>
        /// <param name="animationId">要播放的攻击动画标识。</param>
        private void PlayAnimation(PlayerAnimationId animationId)
        {
            ConfigureAnimationMovement(animationId);
            Context.Animation.PlayOneShot(animationId, OnAttackAnimationEnded);
        }

        /// <summary>
        /// 判断当前攻击结束后是否应自动衔接下一段。
        /// 点击一次后再次点击会写入输入缓冲；持续按住攻击键则只在按住期间持续请求连段。
        /// </summary>
        /// <returns>存在一次性缓冲输入或攻击键仍被按住时返回 true。</returns>
        private bool HasContinuationRequest()
        {
            return _hasQueuedNextAttack || Context.Input.IsAttackHeld;
        }

        /// <summary>
        /// 获取当前攻击段在未续按时的提前释放时间。
        /// </summary>
        /// <returns>当前攻击段允许切回移动状态的归一化时间。</returns>
        private float GetReleaseNormalizedTime()
        {
            return _isPlayingFinisher
                ? FinisherReleaseNormalizedTime
                : BasicAttackReleaseNormalizedTime;
        }

        /// <summary>
        /// 仅当本状态仍处于激活状态时返回自由移动，防止过期动画回调覆盖新状态。
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
