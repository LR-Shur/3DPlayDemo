using Train.Gameplay.Player.Animation.Data;
using Train.Gameplay.Player.Core;
using Train.Gameplay.Player.States.Locomotion;
using UnityEngine;

namespace Train.Gameplay.Player.States
{
    /// <summary>
    /// 负责播放由多个动画组成的战斗动作链，并在每段结束后自动衔接下一段。
    /// 分支、突击、冲刺、突进以及格挡反击都通过本状态复用同一套位移和回收流程。
    /// </summary>
    public sealed class PlayerCombatSequenceState : PlayerState
    {
        /// <summary>
        /// 动作链末尾收势动画的最大播放比例。
        /// 原始资源的 End 段较长，只保留用于视觉回收的前段，避免长时间锁住玩家。
        /// </summary>
        private const float RecoveryExitNormalizedTime = 0.18f;

        private readonly PlayerCombatSequenceId _sequenceId;
        private PlayerAnimationId[] _animationIds;
        private PlayerAnimationDefinition _currentDefinition;
        private Vector3 _lockedDirection;
        private float _appliedMotionDistance;
        private int _currentIndex;
        private int _playbackVersion;

        /// <summary>
        /// 使用指定的战斗动作链初始化状态。
        /// </summary>
        /// <param name="machine">所属玩家状态机。</param>
        /// <param name="context">玩家共享上下文。</param>
        /// <param name="sequenceId">需要播放的战斗动作链标识。</param>
        public PlayerCombatSequenceState(
            PlayerStateMachine machine,
            PlayerContext context,
            PlayerCombatSequenceId sequenceId)
            : base(machine, context)
        {
            _sequenceId = sequenceId;
        }

        /// <summary>
        /// 锁定动作链方向、按输入选择方向变体，并开始播放第一段动画。
        /// </summary>
        public override void Enter()
        {
            Context.Input.ClearBufferedButtons();
            _lockedDirection = Context.Motor.GetCameraRelativeDirection(Context.Input.Move);
            if (_lockedDirection.sqrMagnitude <= 0.0001f)
            {
                _lockedDirection = Context.Motor.Forward;
            }

            _animationIds = PlayerCombatSequenceLibrary.Create(_sequenceId, Context.Input.Move, Context.Config.InputDeadZone);
            _currentIndex = 0;
            PlayCurrentAnimation();
        }

        /// <summary>
        /// 状态完成或被打断时关闭武器 Hitbox。
        /// </summary>
        public override void Exit()
        {
            Context.Combat?.EndAttack();
            base.Exit();
        }

        /// <summary>
        /// 推进当前动作段的数据位移；没有数据位移的动作仍会持续应用重力。
        /// </summary>
        public override void Tick()
        {
            if (_currentDefinition == null)
            {
                return;
            }

            var normalizedTime = Context.Animation.CurrentAnimationNormalizedTime;
            if (_currentDefinition.MovementPolicy == PlayerAnimationMovementPolicy.AuthoredMotion)
            {
                ApplyAuthoredMotionUntil(normalizedTime);
            }
            else
            {
                Context.Motor.MoveVerticalOnly();
            }

            if (TryCancelCurrentAnimation(normalizedTime))
            {
                return;
            }

            if (IsRecoveryAnimation() && normalizedTime >= RecoveryExitNormalizedTime)
            {
                CompleteCurrentAnimation(normalizedTime, false);
            }
        }

        /// <summary>
        /// 播放当前动作段；缺少条目时安全地返回自由移动状态。
        /// </summary>
        private void PlayCurrentAnimation()
        {
            if (_animationIds == null || _currentIndex >= _animationIds.Length ||
                !Context.Animation.TryGetDefinition(_animationIds[_currentIndex], out _currentDefinition))
            {
                ReturnToLocomotion();
                return;
            }

            _appliedMotionDistance = 0f;
            if (_animationIds[_currentIndex].ToString().Contains("_End"))
            {
                Context.Combat?.EndAttack();
            }
            else
            {
                Context.Combat?.BeginAttack();
            }

            ConfigureAnimationMovement(_animationIds[_currentIndex]);
            var playbackVersion = ++_playbackVersion;
            Context.Animation.PlayOneShot(
                _animationIds[_currentIndex],
                () => OnCurrentAnimationEnded(playbackVersion));
        }

        /// <summary>
        /// 完成当前动作段的剩余数据位移后，衔接下一段或回到自由移动状态。
        /// </summary>
        private void OnCurrentAnimationEnded(int playbackVersion)
        {
            if (playbackVersion != _playbackVersion ||
                !ReferenceEquals(PlayerMachine.CurrentState, this))
            {
                return;
            }

            CompleteCurrentAnimation(1f, true);
        }

        /// <summary>
        /// 结束当前动作段，并按需要补齐完整位移或保留当前已播放的位移。
        /// </summary>
        /// <param name="normalizedTime">结束时动画已播放到的归一化时间。</param>
        /// <param name="applyRemainingMotion">是否补齐动画剩余的数值位移。</param>
        private void CompleteCurrentAnimation(float normalizedTime, bool applyRemainingMotion)
        {
            ApplyAuthoredMotionUntil(applyRemainingMotion ? 1f : normalizedTime);
            if (!ReferenceEquals(PlayerMachine.CurrentState, this))
            {
                return;
            }

            _currentIndex++;
            PlayCurrentAnimation();
        }

        /// <summary>
        /// 判断当前动画是否为动作链的收势段。
        /// End 段的原始时长较长，因此只播放到指定窗口便交还控制权。
        /// </summary>
        /// <returns>当前动画名称包含 End 标记时返回 true。</returns>
        private bool IsRecoveryAnimation()
        {
            return _animationIds != null &&
                _currentIndex >= 0 &&
                _currentIndex < _animationIds.Length &&
                _animationIds[_currentIndex].ToString().Contains("_End");
        }

        /// <summary>
        /// 根据当前动作的统一取消窗口处理翻滚或移动接管。
        /// 翻滚优先于持续移动，避免玩家明确按下翻滚时被移动取消抢先处理。
        /// </summary>
        /// <param name="normalizedTime">当前动作的归一化播放进度。</param>
        /// <returns>已经切换到新的顶层状态时返回 true。</returns>
        private bool TryCancelCurrentAnimation(float normalizedTime)
        {
            if (_currentDefinition == null)
            {
                return false;
            }

            if (_currentDefinition.CanCancelTo(
                    PlayerAnimationCancelTarget.Dodge,
                    normalizedTime) &&
                Context.Input.ConsumeDodgePressed())
            {
                PlayerMachine.ChangeState(new DodgeState(PlayerMachine, Context));
                return true;
            }

            if (_currentDefinition.CanCancelTo(
                    PlayerAnimationCancelTarget.Movement,
                    normalizedTime) &&
                Context.Input.Move.sqrMagnitude >
                Context.Config.InputDeadZone * Context.Config.InputDeadZone)
            {
                ReturnToLocomotion();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 根据当前动画的归一化时间计算本帧应补充的数据位移。
        /// </summary>
        /// <param name="normalizedTime">当前动画从零到一的归一化播放进度。</param>
        private void ApplyAuthoredMotionUntil(float normalizedTime)
        {
            if (_currentDefinition == null ||
                _currentDefinition.MovementPolicy != PlayerAnimationMovementPolicy.AuthoredMotion)
            {
                return;
            }

            var targetDistance = _currentDefinition.EvaluateAuthoredMotionDistance(normalizedTime);
            Context.Motor.ApplyAuthoredMotion(_lockedDirection, targetDistance - _appliedMotionDistance);
            _appliedMotionDistance = targetDistance;
        }

        /// <summary>
        /// 仅当本状态仍处于激活状态时回到自由移动，避免过期动画回调覆盖后续行为。
        /// </summary>
        private void ReturnToLocomotion()
        {
            if (ReferenceEquals(PlayerMachine.CurrentState, this))
            {
                PlayerMachine.ChangeState(new LocomotionState(PlayerMachine, Context));
            }
        }
    }

    /// <summary>
    /// 标识可由玩家直接触发的完整战斗动作链。
    /// 每种动作链都对应一组已验证存在于 Ellen 动画目录中的动画标识。
    /// </summary>
    public enum PlayerCombatSequenceId
    {
        Branch,
        Assault,
        Dash,
        Rush,
        ParryCounter,
    }

    /// <summary>
    /// 集中维护战斗动作链的动画顺序和方向变体选择规则。
    /// 这里不依赖具体 FBX 路径，新增动画时只需扩展 PlayerAnimationId 和本映射。
    /// </summary>
    public static class PlayerCombatSequenceLibrary
    {
        /// <summary>
        /// 根据动作链类型和进入时的输入方向创建实际要播放的动画序列。
        /// </summary>
        /// <param name="sequenceId">请求的战斗动作链类型。</param>
        /// <param name="input">进入状态时的移动输入。</param>
        /// <param name="deadZone">忽略微小输入的死区阈值。</param>
        /// <returns>按播放顺序排列的动画标识数组。</returns>
        public static PlayerAnimationId[] Create(
            PlayerCombatSequenceId sequenceId,
            Vector2 input,
            float deadZone)
        {
            return sequenceId switch
            {
                PlayerCombatSequenceId.Branch => CreateBranchSequence(input, deadZone),
                PlayerCombatSequenceId.Assault => CreateAssaultSequence(input, deadZone),
                PlayerCombatSequenceId.Dash => CreateDashSequence(input, deadZone),
                PlayerCombatSequenceId.Rush => CreateRushSequence(),
                PlayerCombatSequenceId.ParryCounter => CreateParryCounterSequence(input, deadZone),
                _ => System.Array.Empty<PlayerAnimationId>(),
            };
        }

        /// <summary>
        /// 创建四段分支攻击的主链；短版本和前几段的 End 动画属于条件分支或提前收招，
        /// 不再错误地与主链强行连续播放。
        /// </summary>
        private static PlayerAnimationId[] CreateBranchSequence(Vector2 input, float deadZone)
        {
            var useShortOpening = input.y < -deadZone;
            return new[]
            {
                useShortOpening
                    ? PlayerAnimationId.Attack_Branch_01_Short
                    : PlayerAnimationId.Attack_Branch_01,
                useShortOpening
                    ? PlayerAnimationId.Attack_Branch_02_Short
                    : PlayerAnimationId.Attack_Branch_02,
                PlayerAnimationId.Attack_Branch_03,
                PlayerAnimationId.Attack_Branch_04,
                PlayerAnimationId.Attack_Branch_04_End,
            };
        }

        /// <summary>
        /// 根据输入方向选择突击辅助的方向变体，并在攻击后播放收势动画。
        /// </summary>
        private static PlayerAnimationId[] CreateAssaultSequence(Vector2 input, float deadZone)
        {
            var isHorizontal = Mathf.Abs(input.x) > deadZone;
            var isBackward = input.y < -deadZone;
            var assaultAnimation = isBackward
                ? isHorizontal
                    ? PlayerAnimationId.Attack_AssaultAid_Near_Back
                    : PlayerAnimationId.Attack_AssaultAid_Back
                : isHorizontal
                    ? PlayerAnimationId.Attack_AssaultAid_Near
                    : PlayerAnimationId.Attack_AssaultAid;
            return new[] { assaultAnimation, PlayerAnimationId.Attack_AssaultAid_End };
        }

        /// <summary>
        /// 根据左右输入选择冲刺起手变体，并自动衔接冲刺循环、斩击、切击和收招。
        /// </summary>
        private static PlayerAnimationId[] CreateDashSequence(Vector2 input, float deadZone)
        {
            var startAnimation = input.x > deadZone
                ? PlayerAnimationId.Attack_Dash_Start_02
                : PlayerAnimationId.Attack_Dash_Start_01;
            return new[]
            {
                startAnimation,
                PlayerAnimationId.Attack_Dash_Slash_01,
                PlayerAnimationId.Attack_Dash_End_01,
            };
        }

        /// <summary>
        /// 创建突进攻击和收势动画组成的动作链。
        /// </summary>
        private static PlayerAnimationId[] CreateRushSequence()
        {
            return new[]
            {
                PlayerAnimationId.Attack_Rush,
                PlayerAnimationId.Attack_Rush_End,
            };
        }

        /// <summary>
        /// 根据左右输入选择轻或重格挡辅助变体；向后输入时改为直接播放反击。
        /// 真正的“成功格挡后反击”将在敌人命中判定系统接入后由外部调用 Counter 动画，
        /// 而不是在没有命中事件时无条件自动播放。
        /// </summary>
        private static PlayerAnimationId[] CreateParryCounterSequence(Vector2 input, float deadZone)
        {
            if (input.y < -deadZone)
            {
                return new[]
                {
                    PlayerAnimationId.Attack_Counter,
                    PlayerAnimationId.Attack_Counter_End,
                };
            }

            var useLightParry = input.x < -deadZone;
            return useLightParry
                ? new[]
                {
                    PlayerAnimationId.Attack_ParryAid_L,
                    PlayerAnimationId.Attack_ParryAid_L_End,
                }
                : new[]
                {
                    PlayerAnimationId.Attack_ParryAid_H,
                    PlayerAnimationId.Attack_ParryAid_H_End,
                };
        }
    }
}
