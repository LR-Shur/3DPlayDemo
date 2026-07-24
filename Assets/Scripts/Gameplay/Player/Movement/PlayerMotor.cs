using Train.Gameplay.Player.Animation.Data;
using UnityEngine;

namespace Train.Gameplay.Player.Movement
{
    /// <summary>
    /// 通过 CharacterController 执行所有带碰撞检测的玩家移动。
    /// 同时支持相机相对移动、Animator Root Motion 和动画时间驱动的可调位移曲线。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private float _gravity = -25f;
        [SerializeField] private float _cameraFacingTurnSpeed = 720f;

        private Transform _cameraTransform;
        private PlayerMovementMode _movementMode;
        private float _rootMotionPositionScale = 1f;
        private float _verticalVelocity;
        private bool _hasReportedMissingCharacterController;

        /// <summary>
        /// 获取当前选中的水平移动来源。
        /// </summary>
        public PlayerMovementMode MovementMode => _movementMode;

        /// <summary>
        /// 获取玩家当前的水平前方方向。
        /// </summary>
        public Vector3 Forward => transform.forward;

        /// <summary>
        /// 获取角色当前是否由 CharacterController 判定为站在地面上。
        /// </summary>
        public bool IsGrounded => EnsureCharacterController() && _characterController.isGrounded;

        /// <summary>
        /// 缓存必需的 CharacterController 引用。
        /// </summary>
        private void Awake()
        {
            EnsureCharacterController();
        }

        /// <summary>
        /// 在编辑器内修改组件时自动回填同一对象上的 CharacterController 引用。
        /// </summary>
        private void OnValidate()
        {
            EnsureCharacterController();
        }

        /// <summary>
        /// 获取移动组件是否已找到可用的 CharacterController。
        /// </summary>
        public bool IsReady => EnsureCharacterController();

        /// <summary>
        /// 设置用于计算移动坐标轴和角色朝向的相机。
        /// </summary>
        /// <param name="cameraTransform">当前游戏相机的 Transform。</param>
        public void SetCameraTransform(Transform cameraTransform)
        {
            _cameraTransform = cameraTransform;
        }

        /// <summary>
        /// 切换水平位移由代码提供还是由当前动画提供。
        /// </summary>
        /// <param name="movementMode">当前状态所需的移动来源。</param>
        public void SetMovementMode(PlayerMovementMode movementMode)
        {
            _movementMode = movementMode;
        }

        /// <summary>
        /// 根据动画定义配置当前动画期间的位移来源与 Root Motion 位移缩放。
        /// 原地和代码移动动画均不会接收动画位移；只有 RootMotion 会接收 Animator 位移。
        /// </summary>
        /// <param name="movementPolicy">动画定义指定的水平位移策略。</param>
        /// <param name="rootMotionPositionScale">Root Motion 水平位移缩放系数。</param>
        public void ConfigureAnimationMovement(
            PlayerAnimationMovementPolicy movementPolicy,
            float rootMotionPositionScale)
        {
            _movementMode = movementPolicy == PlayerAnimationMovementPolicy.RootMotion
                ? PlayerMovementMode.AnimationRootMotion
                : movementPolicy == PlayerAnimationMovementPolicy.AuthoredMotion
                    ? PlayerMovementMode.AuthoredMotion
                    : PlayerMovementMode.Scripted;
            _rootMotionPositionScale = movementPolicy == PlayerAnimationMovementPolicy.RootMotion
                ? Mathf.Max(0f, rootMotionPositionScale)
                : 1f;
        }

        /// <summary>
        /// 使用相机相对的输入轴移动，并只在存在移动输入时朝实际移动方向转身。
        /// </summary>
        /// <param name="input">当前输入向量，Y 表示前后，X 表示左右。</param>
        /// <param name="speed">以米每秒为单位的水平移动速度。</param>
        public void MoveCameraRelative(Vector2 input, float speed)
        {
            if (_movementMode != PlayerMovementMode.Scripted)
            {
                return;
            }

            var movementDirection = GetCameraRelativeDirection(input);
            if (movementDirection.sqrMagnitude <= 0.0001f)
            {
                Move(Vector3.zero);
                return;
            }

            FaceMovementDirection(movementDirection);
            Move(movementDirection * speed * Time.deltaTime);
        }

        /// <summary>
        /// 将输入轴转换为相对相机水平轴的归一化世界坐标方向。
        /// </summary>
        /// <param name="input">需要转换的输入轴。</param>
        /// <returns>归一化的世界坐标方向；没有移动输入时返回零向量。</returns>
        public Vector3 GetCameraRelativeDirection(Vector2 input)
        {
            if (_cameraTransform == null || input.sqrMagnitude <= 0.0001f)
            {
                return Vector3.zero;
            }

            var forward = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(_cameraTransform.right, Vector3.up).normalized;
            return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
        }

        /// <summary>
        /// 立即将玩家逻辑根节点转向指定的水平世界方向。
        /// 用于翻滚等进入状态时必须锁定朝向、不能继续平滑转身的动作。
        /// </summary>
        /// <param name="worldDirection">需要面对的世界空间水平向量。</param>
        public void FaceDirectionImmediately(Vector3 worldDirection)
        {
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(worldDirection.normalized, Vector3.up);
        }

        /// <summary>
        /// 在动画 Root Motion 状态激活时，通过 CharacterController 应用动画位移。
        /// </summary>
        /// <param name="deltaPosition">Animator 在当前帧报告的位移。</param>
        public void ApplyRootMotion(Vector3 deltaPosition)
        {
            if (_movementMode != PlayerMovementMode.AnimationRootMotion)
            {
                return;
            }

            deltaPosition.y = 0f;
            Move(deltaPosition * _rootMotionPositionScale);
        }

        /// <summary>
        /// 在数据驱动位移状态激活时，沿锁定方向执行本帧动画曲线产生的位移。
        /// 最终仍交给 CharacterController，因此不会绕过墙体、台阶和地面碰撞。
        /// </summary>
        /// <param name="worldDirection">状态进入时锁定的水平世界方向。</param>
        /// <param name="distanceDelta">本帧相对上一帧新增的位移距离。</param>
        public void ApplyAuthoredMotion(Vector3 worldDirection, float distanceDelta)
        {
            if (_movementMode != PlayerMovementMode.AuthoredMotion)
            {
                return;
            }

            worldDirection.y = 0f;
            var horizontalDisplacement = worldDirection.sqrMagnitude > 0.0001f
                ? worldDirection.normalized * distanceDelta
                : Vector3.zero;
            Move(horizontalDisplacement);
        }

        /// <summary>
        /// 在角色位于地面时施加一次向上的初速度，使角色以配置高度起跳。
        /// </summary>
        /// <param name="jumpHeight">期望达到的最高跳跃高度。</param>
        /// <returns>成功起跳时返回 true；空中再次请求时返回 false。</returns>
        public bool TryJump(float jumpHeight)
        {
            if (!EnsureCharacterController() || !_characterController.isGrounded || jumpHeight <= 0f)
            {
                return false;
            }

            _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * _gravity);
            return true;
        }

        /// <summary>
        /// 当状态刻意不提供脚本水平移动时，保持重力仍然生效。
        /// </summary>
        public void MoveVerticalOnly()
        {
            if (_movementMode != PlayerMovementMode.Scripted)
            {
                return;
            }

            Move(Vector3.zero);
        }

        /// <summary>
        /// 将角色旋转至本次输入换算得到的实际水平移动方向。
        /// </summary>
        /// <param name="movementDirection">已转换到世界坐标系的水平移动方向。</param>
        private void FaceMovementDirection(Vector3 movementDirection)
        {
            if (movementDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var targetRotation = Quaternion.LookRotation(movementDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                _cameraFacingTurnSpeed * Time.deltaTime);
        }

        /// <summary>
        /// 应用重力，并将最终位移交给 CharacterController。
        /// </summary>
        /// <param name="horizontalDisplacement">当前帧请求的水平位移。</param>
        private void Move(Vector3 horizontalDisplacement)
        {
            if (!EnsureCharacterController())
            {
                return;
            }

            if (_characterController.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            _verticalVelocity += _gravity * Time.deltaTime;
            var verticalDisplacement = Vector3.up * (_verticalVelocity * Time.deltaTime);
            _characterController.Move(horizontalDisplacement + verticalDisplacement);
        }

        /// <summary>
        /// 确保移动组件拥有同一对象上的 CharacterController。
        /// 缺失时仅记录一次错误并禁用自身，防止每帧重复抛出空引用异常。
        /// </summary>
        /// <returns>成功取得 CharacterController 时返回 true。</returns>
        private bool EnsureCharacterController()
        {
            if (_characterController != null)
            {
                return true;
            }

            _characterController = GetComponent<CharacterController>();
            if (_characterController != null)
            {
                _hasReportedMissingCharacterController = false;
                return true;
            }

            if (!_hasReportedMissingCharacterController)
            {
                Debug.LogError("PlayerMotor 必须与 CharacterController 挂在同一个对象上。", this);
                _hasReportedMissingCharacterController = true;
            }

            enabled = false;
            return false;
        }
    }

    /// <summary>
    /// 标识当前为玩家提供水平位移的系统。
    /// </summary>
    public enum PlayerMovementMode
    {
        Scripted,
        AnimationRootMotion,
        AuthoredMotion,
    }
}
