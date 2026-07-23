using UnityEngine;
using UnityEngine.InputSystem;

namespace Train.Gameplay.Player.Input
{
    /// <summary>
    /// 读取 Player 动作映射，并向玩家状态提供便于游戏逻辑使用的输入值。
    /// 此类不包含移动或动画规则。
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _actionsAsset;
        [SerializeField] private string _actionMapName = "Player";

        private InputActionMap _playerMap;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _sprintAction;
        private InputAction _attackAction;
        private InputAction _dodgeAction;
        private bool _attackPressed;
        private bool _dodgePressed;

        /// <summary>
        /// 获取当前二维移动输入。
        /// </summary>
        public Vector2 Move => _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;

        /// <summary>
        /// 获取当前相机旋转输入，来自鼠标移动或手柄右摇杆。
        /// </summary>
        public Vector2 Look => _lookAction != null ? _lookAction.ReadValue<Vector2>() : Vector2.zero;

        /// <summary>
        /// 获取冲刺动作当前是否被按住。
        /// </summary>
        public bool IsSprintHeld => _sprintAction != null && _sprintAction.IsPressed();

        /// <summary>
        /// 在启用输入前查找配置好的 Player 动作。
        /// </summary>
        private void Awake()
        {
            if (_actionsAsset == null)
            {
                Debug.LogError("PlayerInputReader requires an InputActionAsset.", this);
                enabled = false;
                return;
            }

            _playerMap = _actionsAsset.FindActionMap(_actionMapName, true);
            _moveAction = _playerMap.FindAction("Move", true);
            _lookAction = _playerMap.FindAction("Look", true);
            _sprintAction = _playerMap.FindAction("Sprint", true);
            _attackAction = _playerMap.FindAction("Attack", true);
            _dodgeAction = _playerMap.FindAction("Dodge", true);
        }

        /// <summary>
        /// 启用玩家动作并开始缓存按钮按下事件。
        /// </summary>
        private void OnEnable()
        {
            if (_playerMap == null)
            {
                return;
            }

            _attackAction.performed += OnAttackPerformed;
            _dodgeAction.performed += OnDodgePerformed;
            _playerMap.Enable();
        }

        /// <summary>
        /// 在输入读取器禁用时停止接收输入。
        /// </summary>
        private void OnDisable()
        {
            if (_playerMap == null)
            {
                return;
            }

            _attackAction.performed -= OnAttackPerformed;
            _dodgeAction.performed -= OnDodgePerformed;
            _playerMap.Disable();
            ClearBufferedButtons();
        }

        /// <summary>
        /// 返回并清除已缓存的攻击按下事件。
        /// </summary>
        public bool ConsumeAttackPressed()
        {
            var wasPressed = _attackPressed;
            _attackPressed = false;
            return wasPressed;
        }

        /// <summary>
        /// 返回并清除已缓存的翻滚按下事件。
        /// </summary>
        public bool ConsumeDodgePressed()
        {
            var wasPressed = _dodgePressed;
            _dodgePressed = false;
            return wasPressed;
        }

        /// <summary>
        /// 当状态需要忽略玩家指令时清除已缓存的按钮事件。
        /// </summary>
        public void ClearBufferedButtons()
        {
            _attackPressed = false;
            _dodgePressed = false;
        }

        /// <summary>
        /// 缓存攻击动作，确保状态机能够可靠地读取该输入。
        /// </summary>
        private void OnAttackPerformed(InputAction.CallbackContext context)
        {
            _attackPressed = true;
        }

        /// <summary>
        /// 缓存翻滚动作，确保状态机能够可靠地读取该输入。
        /// </summary>
        private void OnDodgePerformed(InputAction.CallbackContext context)
        {
            _dodgePressed = true;
        }
    }
}
