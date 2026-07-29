using System;
using Train.Gameplay.Player.Animation;
using Train.Gameplay.Player.Animation.Data;
using Train.Gameplay.Player.Core;
using Train.Gameplay.Player.Data;
using Train.Gameplay.Player.Input;
using Train.Gameplay.Player.Movement;
using Train.Gameplay.Player.States;
using UnityEditor;
using UnityEngine;

namespace Train.EditorTools
{
    /// <summary>
    /// 在真实播放模式下检查 Ellen 玩家预制体的逻辑根、表现根和蒙皮根骨骼是否发生水平分离。
    /// 验证会主动播放 Walk 或 Run，并在一个移动周期内测量层级漂移和腿部实际摆动。
    /// </summary>
    [InitializeOnLoad]
    public static class PlayerPrefabRuntimeValidator
    {
        private const string ValidationRequestedKey = "Train.PlayerPrefabRuntimeValidator.Requested";
        private const string ValidationModeKey = "Train.PlayerPrefabRuntimeValidator.Mode";
        private const string VisualRootName = "PlayerVisualRoot";
        private const string ModelRootName = "TPOSS";
        private const string SkinRootPath = "Avatar_Female_Size02_Ellen/Bip001";
        private const string LeftThighPath =
            "Avatar_Female_Size02_Ellen/Bip001/Bip001 Pelvis/Bip001 L Thigh";
        private const string PlayerConfigPath = "Assets/Settings/PlayerConfig.asset";

        private static Transform _playerRoot;
        private static Transform _visualRoot;
        private static Transform _modelRoot;
        private static Transform _skinRoot;
        private static Transform _leftThigh;
        private static Vector3 _baselinePlayerPosition;
        private static Vector3 _baselineVisualPosition;
        private static Vector3 _baselineModelPosition;
        private static Vector3 _baselineSkinPosition;
        private static Quaternion _baselineLeftThighRotation;
        private static float _maximumLeftThighRotation;
        private static double _baselineTime;
        private static double _finishTime;
        private static bool _hasBaseline;
        private static bool _isSampling;
        private static ValidationMode _validationMode;
        private static PlayerAnimationDefinition _validationAnimationDefinition;
        private static PlayerStateMachine _validationStateMachine;
        private static int _lastStateMachineTickFrame = -1;

        /// <summary>
        /// 定义运行时验证当前检查原地动画还是翻滚数据驱动位移。
        /// </summary>
        private enum ValidationMode
        {
            WalkInPlace,
            RunInPlace,
            DodgeMotion,
        }

        /// <summary>
        /// 在编辑器脚本载入时注册播放模式与采样更新回调。
        /// </summary>
        static PlayerPrefabRuntimeValidator()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= UpdateValidation;
            EditorApplication.update += UpdateValidation;
        }

        /// <summary>
        /// 进入播放模式并自动执行 Walk 原地性和预制体层级同步验证。
        /// </summary>
        [MenuItem("Train/Player/验证 Ellen 玩家预制体运行结构")]
        public static void ValidatePlayerPrefab()
        {
            RequestValidation(ValidationMode.WalkInPlace);
        }

        /// <summary>
        /// 进入播放模式并自动执行 Run 原地性、腿部摆动和预制体层级同步验证。
        /// </summary>
        [MenuItem("Train/Player/验证 Ellen 跑步动画")]
        public static void ValidateRunAnimation()
        {
            RequestValidation(ValidationMode.RunInPlace);
        }

        /// <summary>
        /// 进入播放模式并验证翻滚位移是否只移动玩家逻辑根而不拆散表现层。
        /// </summary>
        [MenuItem("Train/Player/验证 Ellen 翻滚位移")]
        public static void ValidateDodgeMotion()
        {
            RequestValidation(ValidationMode.DodgeMotion);
        }

        /// <summary>
        /// 保存待执行的验证类型，并确保 Unity 进入播放模式。
        /// </summary>
        /// <param name="validationMode">本次需要运行的验证类型。</param>
        private static void RequestValidation(ValidationMode validationMode)
        {
            SessionState.SetBool(ValidationRequestedKey, true);
            SessionState.SetInt(ValidationModeKey, (int)validationMode);
            if (EditorApplication.isPlaying)
            {
                BeginValidation();
                return;
            }

            EditorApplication.delayCall += EnterPlayModeForValidation;
        }

        /// <summary>
        /// 在当前菜单命令结束后的编辑器更新中进入播放模式，避免切换过程被工具调用阻塞。
        /// </summary>
        private static void EnterPlayModeForValidation()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.isPlaying = true;
            }
        }

        /// <summary>
        /// 在 Unity 完成进入播放模式后启动实际动画采样。
        /// </summary>
        /// <param name="state">Unity 当前的播放模式切换阶段。</param>
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode &&
                SessionState.GetBool(ValidationRequestedKey, false))
            {
                EditorApplication.delayCall += BeginValidation;
            }
        }

        /// <summary>
        /// 查找预制体关键节点、播放待验证动画，并安排基准帧与结束帧的采样时间。
        /// </summary>
        private static void BeginValidation()
        {
            _validationMode = (ValidationMode)SessionState.GetInt(
                ValidationModeKey,
                (int)ValidationMode.WalkInPlace);
            var playerObject = GameObject.FindWithTag("Player");
            _playerRoot = playerObject != null ? playerObject.transform : null;
            _visualRoot = _playerRoot != null ? _playerRoot.Find(VisualRootName) : null;
            _modelRoot = _visualRoot != null ? _visualRoot.Find(ModelRootName) : null;
            _skinRoot = _modelRoot != null ? _modelRoot.Find(SkinRootPath) : null;
            _leftThigh = _modelRoot != null
                ? _modelRoot.Find(LeftThighPath)
                : null;

            var animation = _modelRoot != null ? _modelRoot.GetComponent<PlayerAnimation>() : null;
            var motor = _playerRoot != null ? _playerRoot.GetComponent<PlayerMotor>() : null;
            var catalog = Resources.Load<PlayerAnimationCatalog>("Player/EllenAnimationCatalog");
            if (_playerRoot == null ||
                _visualRoot == null ||
                _modelRoot == null ||
                _skinRoot == null ||
                (_validationMode != ValidationMode.DodgeMotion &&
                 _leftThigh == null) ||
                animation == null ||
                motor == null ||
                catalog == null ||
                !catalog.TryGet(GetValidationAnimationId(), out var animationDefinition))
            {
                FinishValidation(false, "验证失败：玩家预制体缺少关键层级、动画组件、移动组件或待验证动画定义。");
                return;
            }

            _validationAnimationDefinition = animationDefinition;
            if (_validationMode != ValidationMode.DodgeMotion)
            {
                motor.ConfigureAnimationMovement(PlayerAnimationMovementPolicy.ScriptedMovement, 1f);
                animation.PlayLoop(GetValidationAnimationId());
            }
            else
            {
                var controller = _playerRoot.GetComponent<PlayerController>();
                var input = _playerRoot.GetComponent<PlayerInputReader>();
                var config = AssetDatabase.LoadAssetAtPath<PlayerConfig>(PlayerConfigPath);
                if (controller == null || input == null || config == null)
                {
                    FinishValidation(false, "验证失败：无法创建真实翻滚状态所需的控制器、输入或玩家配置。");
                    return;
                }

                controller.enabled = false;
                var context = new PlayerContext(input, motor, animation, config);
                _validationStateMachine = new PlayerStateMachine(context);
                CaptureBaseline();
                _validationStateMachine.ChangeState(
                    new DodgeState(_validationStateMachine, context));
                _lastStateMachineTickFrame = -1;
            }

            var currentTime = Time.timeAsDouble;
            if (_validationMode != ValidationMode.DodgeMotion)
            {
                _baselineTime = currentTime + Math.Max(0.15d, animationDefinition.FadeDuration + 0.03d);
                var clipLength = animationDefinition.PrimaryClip != null
                    ? animationDefinition.PrimaryClip.length
                    : animationDefinition.Transition.MaximumDuration;
                _finishTime = _baselineTime + Math.Max(0.2d, clipLength * 0.45d);
                _hasBaseline = false;
            }
            else
            {
                _baselineTime = currentTime;
                var clipLength = animationDefinition.PrimaryClip != null
                    ? animationDefinition.PrimaryClip.length
                    : animationDefinition.Transition.MaximumDuration;
                _finishTime = currentTime + Math.Max(0.3d, clipLength + 0.2d);
            }

            _isSampling = true;
        }

        /// <summary>
        /// 在 Walk 播放期间采集基准值和结束值，并计算各层节点的水平漂移。
        /// </summary>
        private static void UpdateValidation()
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            if (!_isSampling)
            {
                if (SessionState.GetBool(ValidationRequestedKey, false))
                {
                    BeginValidation();
                }

                return;
            }

            TickValidationStateMachine();
            var currentTime = Time.timeAsDouble;
            if (!_hasBaseline && currentTime >= _baselineTime)
            {
                CaptureBaseline();
            }

            if (_hasBaseline && _leftThigh != null)
            {
                _maximumLeftThighRotation = Mathf.Max(
                    _maximumLeftThighRotation,
                    Quaternion.Angle(
                        _baselineLeftThighRotation,
                        _leftThigh.localRotation));
            }

            if (!_hasBaseline || currentTime < _finishTime)
            {
                return;
            }

            var playerDrift = GetHorizontalDistance(_baselinePlayerPosition, _playerRoot.position);
            var visualDrift = GetHorizontalDistance(_baselineVisualPosition, _visualRoot.position);
            var modelDrift = GetHorizontalDistance(_baselineModelPosition, _modelRoot.position);
            var skinDrift = GetHorizontalDistance(_baselineSkinPosition, _skinRoot.position);
            if (_validationMode != ValidationMode.DodgeMotion)
            {
                var animationName =
                    _validationMode == ValidationMode.RunInPlace
                        ? "Run"
                        : "Walk";
                var locomotionPassed = playerDrift <= 0.05f &&
                                        visualDrift <= 0.05f &&
                                        modelDrift <= 0.05f &&
                                        skinDrift <= 0.35f &&
                                        _maximumLeftThighRotation >= 10f;
                FinishValidation(
                    locomotionPassed,
                    $"{animationName} 水平漂移：逻辑根={playerDrift:F3}m，表现根={visualDrift:F3}m，" +
                    $"TPOSS={modelDrift:F3}m，Bip001={skinDrift:F3}m，" +
                    $"左大腿最大摆动={_maximumLeftThighRotation:F1}°。" +
                    (locomotionPassed
                        ? $"验证通过，模型未分离且 {animationName} 步态正在播放。"
                        : "验证未通过，请继续检查原地动画或腿部曲线。"));
                return;
            }

            var visualSeparation = GetHorizontalDistance(
                _baselineVisualPosition - _baselinePlayerPosition,
                _visualRoot.position - _playerRoot.position);
            var modelSeparation = GetHorizontalDistance(
                _baselineModelPosition - _baselinePlayerPosition,
                _modelRoot.position - _playerRoot.position);
            var skinSeparation = GetHorizontalDistance(
                _baselineSkinPosition - _baselinePlayerPosition,
                _skinRoot.position - _playerRoot.position);
            var expectedDistance = _validationAnimationDefinition != null
                ? _validationAnimationDefinition.AuthoredMotionDistance
                : 0f;
            var minimumAcceptedDistance = Mathf.Max(0.1f, expectedDistance * 0.8f);
            var dodgePassed = playerDrift >= minimumAcceptedDistance &&
                              visualSeparation <= 0.05f &&
                              modelSeparation <= 0.05f &&
                              skinSeparation <= 0.35f;
            FinishValidation(
                dodgePassed,
                $"翻滚位移：逻辑根移动={playerDrift:F3}m，配置总距离={expectedDistance:F3}m，" +
                $"表现根分离={visualSeparation:F3}m，" +
                $"TPOSS 分离={modelSeparation:F3}m，Bip001 分离={skinSeparation:F3}m。" +
                (dodgePassed ? "验证通过，数据驱动位移已移动完整玩家。" : "验证未通过，请继续检查翻滚位移来源。"));
        }

        /// <summary>
        /// 在翻滚验证期间每个游戏帧只更新一次真实玩家状态机，避免编辑器回调频率影响位移结果。
        /// </summary>
        private static void TickValidationStateMachine()
        {
            if (_validationMode != ValidationMode.DodgeMotion ||
                _validationStateMachine == null ||
                _lastStateMachineTickFrame == Time.frameCount)
            {
                return;
            }

            _lastStateMachineTickFrame = Time.frameCount;
            _validationStateMachine.Tick();
        }

        /// <summary>
        /// 根据当前请求返回需要播放和检查的动画标识。
        /// </summary>
        /// <returns>原地验证使用 Walk 或 Run，翻滚位移验证使用前翻滚。</returns>
        private static PlayerAnimationId GetValidationAnimationId()
        {
            var validationMode = (ValidationMode)SessionState.GetInt(
                ValidationModeKey,
                (int)ValidationMode.WalkInPlace);
            return validationMode switch
            {
                ValidationMode.WalkInPlace => PlayerAnimationId.Walk,
                ValidationMode.RunInPlace => PlayerAnimationId.Run,
                _ => PlayerAnimationId.Evade_Front,
            };
        }

        /// <summary>
        /// 记录开始采样时各关键节点的世界空间位置。
        /// </summary>
        private static void CaptureBaseline()
        {
            _baselinePlayerPosition = _playerRoot.position;
            _baselineVisualPosition = _visualRoot.position;
            _baselineModelPosition = _modelRoot.position;
            _baselineSkinPosition = _skinRoot.position;
            if (_leftThigh != null)
            {
                _baselineLeftThighRotation = _leftThigh.localRotation;
            }

            _maximumLeftThighRotation = 0f;
            _hasBaseline = true;
        }

        /// <summary>
        /// 计算两个世界位置在水平面上的距离。
        /// </summary>
        /// <param name="from">起始世界位置。</param>
        /// <param name="to">结束世界位置。</param>
        /// <returns>忽略高度差后的水平距离。</returns>
        private static float GetHorizontalDistance(Vector3 from, Vector3 to)
        {
            var delta = to - from;
            delta.y = 0f;
            return delta.magnitude;
        }

        /// <summary>
        /// 输出验证结论、清理采样状态，并在下一次编辑器更新时退出播放模式。
        /// </summary>
        /// <param name="passed">本次运行结构验证是否通过。</param>
        /// <param name="message">需要输出到 Unity Console 的验证详情。</param>
        private static void FinishValidation(bool passed, string message)
        {
            _isSampling = false;
            _hasBaseline = false;
            _validationAnimationDefinition = null;
            _validationStateMachine = null;
            _leftThigh = null;
            _lastStateMachineTickFrame = -1;
            SessionState.SetBool(ValidationRequestedKey, false);

            if (passed)
            {
                Debug.Log($"Ellen 玩家预制体验证通过：{message}");
            }
            else
            {
                Debug.LogError($"Ellen 玩家预制体验证失败：{message}");
            }

            if (EditorApplication.isPlaying)
            {
                EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
            }
        }
    }
}
