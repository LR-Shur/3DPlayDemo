using Train.Gameplay.Player.Animation;
using Train.Gameplay.Player.Data;
using Train.Gameplay.Player.Input;
using Train.Gameplay.Player.Movement;
using Train.Gameplay.Player.Animation.Data;
using Train.Gameplay.Player.States;
using Train.Gameplay.Player.States.Locomotion;
using UnityEngine;

namespace Train.Gameplay.Player.Core
{
    /// <summary>
    /// 组装玩家服务、创建初始状态机，并在每帧驱动状态机。
    /// 此组件是唯一了解完整玩家状态配置的 MonoBehaviour。
    /// </summary>
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private PlayerAnimation _animation;
        [SerializeField] private PlayerConfig _config;
        [SerializeField] private Transform _cameraTransform;

        private PlayerStateMachine _stateMachine;

        [Header("运行时状态调试（播放时查看）")]
        [SerializeField, Tooltip("当前顶层玩家状态名称。")]
        private string _currentStateName;

        /// <summary>
        /// 校验引用、配置相机相对移动，并进入移动状态。
        /// </summary>
        private void Awake()
        {
            _input ??= GetComponent<PlayerInputReader>();
            _motor ??= GetComponent<PlayerMotor>();
            _animation ??= GetComponentInChildren<PlayerAnimation>();
            _cameraTransform ??= UnityEngine.Camera.main != null ? UnityEngine.Camera.main.transform : null;

            if (_input == null || _motor == null || !_motor.IsReady || _animation == null || _config == null || _cameraTransform == null)
            {
                Debug.LogError("PlayerController 缺少输入、移动、动画、配置、相机或 CharacterController 引用。", this);
                enabled = false;
                return;
            }

            _motor.SetCameraTransform(_cameraTransform);
            var context = new PlayerContext(_input, _motor, _animation, _config);
            _stateMachine = new PlayerStateMachine(context);
            _stateMachine.ChangeState(new LocomotionState(_stateMachine, context));
        }

        /// <summary>
        /// 在 Unity 处理完当前帧输入后更新当前玩家状态。
        /// </summary>
        private void Update()
        {
            _stateMachine?.Tick();
            _currentStateName = _stateMachine?.CurrentState?.GetType().Name ?? string.Empty;
        }

        /// <summary>
        /// 请求播放任意已收录的非移动动画。
        /// 受击、切换、交互、剧情和非基础普攻动作可由战斗或关卡系统通过此接口接入。
        /// </summary>
        /// <param name="animationId">需要进入状态机播放的动画标识。</param>
        /// <returns>成功切换到对应动作状态时返回 true。</returns>
        public bool TryPlayAction(PlayerAnimationId animationId)
        {
            if (_stateMachine == null ||
                _animation == null ||
                !_animation.TryGetDefinition(animationId, out var definition))
            {
                return false;
            }

            if (definition.StateKind is PlayerAnimationStateKind.LocomotionIdle or
                PlayerAnimationStateKind.LocomotionStart or
                PlayerAnimationStateKind.LocomotionLoop or
                PlayerAnimationStateKind.LocomotionStop or
                PlayerAnimationStateKind.LocomotionTurn or
                PlayerAnimationStateKind.Dodge or
                PlayerAnimationStateKind.PrimaryAttack)
            {
                Debug.LogWarning($"动画 {animationId} 应由移动、翻滚或基础攻击状态驱动，不能通过通用动作接口直接播放。", this);
                return false;
            }

            var returnToLocomotion = definition.StateKind != PlayerAnimationStateKind.Death;
            _stateMachine.ChangeState(new PlayerActionState(
                _stateMachine,
                _stateMachine.Context,
                animationId,
                returnToLocomotion));
            return true;
        }

        /// <summary>
        /// 请求播放基础普攻连段。
        /// </summary>
        public void StartPrimaryAttack()
        {
            if (_stateMachine != null)
            {
                _stateMachine.ChangeState(new AttackState(_stateMachine, _stateMachine.Context));
            }
        }

        /// <summary>
        /// 请求进入一条完整的战斗动作链，供技能、AI 指令或测试按钮统一使用。
        /// </summary>
        /// <param name="sequenceId">需要进入的分支、突击、冲刺、突进或格挡反击动作链。</param>
        public void StartCombatSequence(PlayerCombatSequenceId sequenceId)
        {
            if (_stateMachine != null)
            {
                _stateMachine.ChangeState(new PlayerCombatSequenceState(
                    _stateMachine,
                    _stateMachine.Context,
                    sequenceId));
            }
        }

        /// <summary>
        /// 请求播放受击、击飞等反应动画。
        /// </summary>
        /// <param name="animationId">需要播放的反应动画标识。</param>
        /// <returns>动画属于反应类别且成功播放时返回 true。</returns>
        public bool TryPlayReaction(PlayerAnimationId animationId)
        {
            return TryPlayActionByKind(animationId, PlayerAnimationStateKind.Reaction);
        }

        /// <summary>
        /// 请求播放角色死亡动画；死亡动画结束后不会自动返回自由移动状态。
        /// </summary>
        /// <returns>成功进入死亡动作状态时返回 true。</returns>
        public bool TryPlayDeath()
        {
            return TryPlayActionByKind(PlayerAnimationId.Death, PlayerAnimationStateKind.Death);
        }

        /// <summary>
        /// 请求播放角色切换类动画。
        /// </summary>
        /// <param name="animationId">需要播放的切换动画标识。</param>
        /// <returns>动画属于切换类别且成功播放时返回 true。</returns>
        public bool TryPlayTransition(PlayerAnimationId animationId)
        {
            return TryPlayActionByKind(animationId, PlayerAnimationStateKind.Transition);
        }

        /// <summary>
        /// 请求播放交互或剧情类动画。
        /// </summary>
        /// <param name="animationId">需要播放的交互或剧情动画标识。</param>
        /// <returns>动画属于交互或剧情类别且成功播放时返回 true。</returns>
        public bool TryPlayInteractionOrCinematic(PlayerAnimationId animationId)
        {
            if (_animation == null || !_animation.TryGetDefinition(animationId, out var definition) ||
                definition.StateKind is not (PlayerAnimationStateKind.Interaction or PlayerAnimationStateKind.Cinematic))
            {
                return false;
            }

            return TryPlayAction(animationId);
        }

        /// <summary>
        /// 强制结束当前通用动作并返回自由移动状态。
        /// 用于外部战斗、交互或剧情系统在明确时机释放角色控制权。
        /// </summary>
        public void ReturnToLocomotion()
        {
            if (_stateMachine != null)
            {
                _stateMachine.ChangeState(new LocomotionState(_stateMachine, _stateMachine.Context));
            }
        }

        /// <summary>
        /// 验证动作类别后通过通用动作状态播放动画。
        /// </summary>
        /// <param name="animationId">需要播放的动画标识。</param>
        /// <param name="expectedStateKind">调用方允许播放的状态类别。</param>
        /// <returns>类别匹配且成功进入动作状态时返回 true。</returns>
        private bool TryPlayActionByKind(
            PlayerAnimationId animationId,
            PlayerAnimationStateKind expectedStateKind)
        {
            if (_animation == null ||
                !_animation.TryGetDefinition(animationId, out var definition) ||
                definition.StateKind != expectedStateKind)
            {
                return false;
            }

            return TryPlayAction(animationId);
        }
    }
}
