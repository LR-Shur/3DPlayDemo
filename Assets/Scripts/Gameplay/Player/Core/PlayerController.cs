using Train.Gameplay.Player.Animation;
using Train.Gameplay.Player.Data;
using Train.Gameplay.Player.Input;
using Train.Gameplay.Player.Movement;
using Train.Gameplay.Player.Animation.Data;
using Train.Gameplay.Player.States;
using Train.Gameplay.Player.States.Locomotion;
using Train.Gameplay.Combat;
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
        [SerializeField] private PlayerCombat _combat;

        private PlayerStateMachine _stateMachine;

        [Header("运行时状态调试（播放时查看）")]
        [SerializeField, Tooltip("当前顶层玩家状态名称。")]
        private string _currentStateName;

        // 动态玩家由 YooAsset 实例化时，Awake 可能早于相机绑定；初始化必须允许重试。
        private bool _initialized;
        private bool _reportedMissingReferences;

        /// <summary>
        /// 在运行时相机或 YooAsset 玩家实例晚于 Awake 完成时，主动重新绑定玩家状态机。
        /// </summary>
        public bool InitializeRuntime()
        {
            return TryInitialize();
        }

        /// <summary>
        /// 使用场景已经准备好的主相机初始化玩家状态机。
        /// 商店等独立测试场景可能在玩家 Awake 之后才创建相机，显式传入可避免状态机一直等待相机依赖。
        /// </summary>
        public bool InitializeRuntime(Transform cameraTransform)
        {
            _cameraTransform = cameraTransform;
            return TryInitialize();
        }

        /// <summary>
        /// 校验引用、配置相机相对移动，并进入移动状态。
        /// </summary>
        private void Awake()
        {
            TryInitialize();
        }

        /// <summary>
        /// 在所有动态依赖就绪后创建玩家状态机；相机或物理组件暂缺时留待后续帧重试。
        /// </summary>
        private bool TryInitialize()
        {
            if (_initialized)
            {
                return true;
            }

            _input ??= GetComponent<PlayerInputReader>();
            _motor ??= GetComponent<PlayerMotor>();
            _animation ??= GetComponentInChildren<PlayerAnimation>(true);
            _combat ??= GetComponent<PlayerCombat>();
            _cameraTransform ??= UnityEngine.Camera.main != null
                ? UnityEngine.Camera.main.transform
                : null;

            var missing = _input == null ||
                          _motor == null ||
                          !_motor.IsReady ||
                          _animation == null ||
                          _config == null ||
                          _cameraTransform == null;
            if (missing)
            {
                if (!_reportedMissingReferences)
                {
                    _reportedMissingReferences = true;
                    Debug.LogWarning(
                        $"PlayerController 等待依赖就绪：input={_input != null}, " +
                        $"motor={_motor != null && _motor.IsReady}, " +
                        $"animation={_animation != null}, config={_config != null}, " +
                        $"camera={_cameraTransform != null}, combat={_combat != null}。" +
                        "动态玩家会在后续帧自动重试。",
                        this);
                }

                return false;
            }

            _motor.SetCameraTransform(_cameraTransform);
            var context = new PlayerContext(_input, _motor, _animation, _config, _combat);
            _stateMachine = new PlayerStateMachine(context);
            _stateMachine.ChangeState(new LocomotionState(_stateMachine, context));
            _initialized = true;
            return true;
        }

        /// <summary>Start 阶段再尝试一次，覆盖相机在玩家 Awake 后才创建的场景。</summary>
        private void Start()
        {
            TryInitialize();
        }

        /// <summary>
        /// 在 Unity 处理完当前帧输入后更新当前玩家状态。
        /// </summary>
        private void Update()
        {
            if (!TryInitialize())
            {
                return;
            }

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
