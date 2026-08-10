using UnityEngine;
using UnityEngine.InputSystem;

namespace Train.Gameplay.Player.Input
{
    /// <summary>
    /// 读取 Player 动作映射，并向玩家状态提供便于游戏逻辑使用的输入值。
    /// 当前项目为单本地玩家，直接启用输入资源中的 Player 动作映射。
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
        private InputAction _rushAction;
        private InputAction _jumpAction;
        private InputAction _crouchAction;
        private InputAction _interactAction;
        private InputAction _startAction;
        private InputAction _previousAction;
        private InputAction _nextAction;
        private bool _attackPressed;
        private bool _dodgePressed;
        private bool _rushPressed;
        private bool _jumpPressed;
        private bool _crouchPressed;
        private bool _interactPressed;
        private bool _startPressed;
        private bool _previousPressed;
        private bool _nextPressed;
        private bool _externalInputBlocked;

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
        /// 获取攻击动作当前是否仍被按住。
        /// 与一次性按下事件不同，该值用于让普通攻击状态判断玩家是否希望持续完成一轮连段。
        /// </summary>
        public bool IsAttackHeld => _attackAction != null && _attackAction.IsPressed();

        /// <summary>
        /// 获取是否存在尚未被消费的交互按下事件。
        /// 世界交互控制器可先判断附近是否有目标；没有目标时把该输入留给玩家状态机。
        /// </summary>
        public bool HasInteractPressed => _interactPressed;

        /// <summary>获取是否存在尚未消费的关卡开始按键。</summary>
        public bool HasStartPressed => _startPressed;

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
            _rushAction = _playerMap.FindAction("Rush", true);
            _jumpAction = _playerMap.FindAction("Jump", true);
            _crouchAction = _playerMap.FindAction("Crouch", true);
            _interactAction = _playerMap.FindAction("Interact", true);
            // 允许旧输入资产暂时没有 Start 动作，仍可由 UI 直接调用 StartLevel。
            _startAction = _playerMap.FindAction("Start", false);
            _previousAction = _playerMap.FindAction("Previous", true);
            _nextAction = _playerMap.FindAction("Next", true);
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
            _rushAction.performed += OnRushPerformed;
            _jumpAction.performed += OnJumpPerformed;
            _crouchAction.performed += OnCrouchPerformed;
            _interactAction.performed += OnInteractPerformed;
            if (_startAction != null)
            {
                _startAction.performed += OnStartPerformed;
            }
            _previousAction.performed += OnPreviousPerformed;
            _nextAction.performed += OnNextPerformed;
            if (_externalInputBlocked)
            {
                _startAction?.Enable();
            }
            else
            {
                _playerMap.Enable();
            }
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
            _rushAction.performed -= OnRushPerformed;
            _jumpAction.performed -= OnJumpPerformed;
            _crouchAction.performed -= OnCrouchPerformed;
            _interactAction.performed -= OnInteractPerformed;
            if (_startAction != null)
            {
                _startAction.performed -= OnStartPerformed;
            }
            _previousAction.performed -= OnPreviousPerformed;
            _nextAction.performed -= OnNextPerformed;
            _playerMap.Disable();
            _startAction?.Disable();
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
        /// 返回并清除已缓存的突进攻击按下事件。
        /// </summary>
        public bool ConsumeRushPressed()
        {
            var wasPressed = _rushPressed;
            _rushPressed = false;
            return wasPressed;
        }

        /// <summary>
        /// 返回并清除已缓存的跳跃按下事件。
        /// </summary>
        public bool ConsumeJumpPressed()
        {
            var wasPressed = _jumpPressed;
            _jumpPressed = false;
            return wasPressed;
        }

        /// <summary>
        /// 返回并清除已缓存的格挡/反击按下事件。
        /// </summary>
        public bool ConsumeCrouchPressed()
        {
            var wasPressed = _crouchPressed;
            _crouchPressed = false;
            return wasPressed;
        }

        /// <summary>
        /// 返回并清除已缓存的交互按下事件。
        /// </summary>
        public bool ConsumeInteractPressed()
        {
            var wasPressed = _interactPressed;
            _interactPressed = false;
            return wasPressed;
        }

        /// <summary>返回并清除一次关卡开始按键。</summary>
        public bool ConsumeStartPressed()
        {
            var wasPressed = _startPressed;
            _startPressed = false;
            return wasPressed;
        }

        /// <summary>
        /// 返回并清除已缓存的分支攻击按下事件。
        /// </summary>
        public bool ConsumePreviousPressed()
        {
            var wasPressed = _previousPressed;
            _previousPressed = false;
            return wasPressed;
        }

        /// <summary>
        /// 返回并清除已缓存的突击攻击按下事件。
        /// </summary>
        public bool ConsumeNextPressed()
        {
            var wasPressed = _nextPressed;
            _nextPressed = false;
            return wasPressed;
        }

        /// <summary>
        /// 当状态需要忽略玩家指令时清除已缓存的按钮事件。
        /// </summary>
        public void ClearBufferedButtons()
        {
            _attackPressed = false;
            _dodgePressed = false;
            _rushPressed = false;
            _jumpPressed = false;
            _crouchPressed = false;
            _interactPressed = false;
            _startPressed = false;
            _previousPressed = false;
            _nextPressed = false;
        }

        /// <summary>
        /// Blocks gameplay actions without changing this component's enabled
        /// state. Level flow and modal UI can therefore gate input independently.
        /// </summary>
        public void SetExternalInputBlocked(bool blocked)
        {
            if (_externalInputBlocked == blocked)
            {
                return;
            }

            _externalInputBlocked = blocked;
            if (_playerMap == null)
            {
                return;
            }

            if (blocked)
            {
                _playerMap.Disable();
                _startAction?.Enable();
                ClearBufferedButtons();
            }
            else if (isActiveAndEnabled)
            {
                _startAction?.Disable();
                _playerMap.Enable();
            }
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

        /// <summary>
        /// 缓存突进攻击按下事件，当前映射为 F 键或手柄 Right Trigger。
        /// </summary>
        private void OnRushPerformed(InputAction.CallbackContext context)
        {
            _rushPressed = true;
        }

        /// <summary>
        /// 缓存跳跃按下事件，确保状态机不会遗漏短按空格。
        /// </summary>
        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            _jumpPressed = true;
        }

        /// <summary>
        /// 缓存格挡/反击按下事件，当前映射为 C 键或手柄 East 键。
        /// </summary>
        private void OnCrouchPerformed(InputAction.CallbackContext context)
        {
            _crouchPressed = true;
        }

        /// <summary>
        /// 缓存交互按下事件，当前映射为 E 键或手柄 North 键。
        /// </summary>
        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            _interactPressed = true;
        }

        /// <summary>缓存关卡开始按键，供关卡流程在输入锁定阶段读取。</summary>
        private void OnStartPerformed(InputAction.CallbackContext context)
        {
            _startPressed = true;
        }

        /// <summary>
        /// 缓存分支攻击按下事件，当前映射为数字 1 或手柄方向键左。
        /// </summary>
        private void OnPreviousPerformed(InputAction.CallbackContext context)
        {
            _previousPressed = true;
        }

        /// <summary>
        /// 缓存突击攻击按下事件，当前映射为数字 2 或手柄方向键右。
        /// </summary>
        private void OnNextPerformed(InputAction.CallbackContext context)
        {
            _nextPressed = true;
        }
    }
}
