using Train.Gameplay.Player.Animation;
using Train.Gameplay.Player.Data;
using Train.Gameplay.Player.Input;
using Train.Gameplay.Player.Movement;
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

        /// <summary>
        /// 校验引用、配置相机相对移动，并进入移动状态。
        /// </summary>
        private void Awake()
        {
            _input ??= GetComponent<PlayerInputReader>();
            _motor ??= GetComponent<PlayerMotor>();
            _animation ??= GetComponentInChildren<PlayerAnimation>();
            _cameraTransform ??= Camera.main != null ? Camera.main.transform : null;

            if (_input == null || _motor == null || _animation == null || _config == null || _cameraTransform == null)
            {
                Debug.LogError("PlayerController requires Input, Motor, Animation, Config, and Camera references.", this);
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
        }
    }
}
