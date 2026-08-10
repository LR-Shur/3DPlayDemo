using System.Collections.Generic;
using Train.Gameplay.Player.Animation.Data;
using UnityEngine;

namespace Train.Gameplay.Player.Movement
{
    /// <summary>
    /// 通过动态 Rigidbody 和 CapsuleCollider 执行玩家移动与碰撞响应。
    /// 同时支持相机相对移动、Animator Root Motion 和动画时间驱动的可调位移曲线。
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private CapsuleCollider _capsuleCollider;
        [SerializeField, Range(0f, 89f)] private float _maximumGroundAngle = 55f;
        [SerializeField] private float _cameraFacingTurnSpeed = 720f;

        private readonly HashSet<Collider> _groundColliders = new();
        private Transform _cameraTransform;
        private PlayerMovementMode _movementMode;
        private float _rootMotionPositionScale = 1f;
        private Vector3 _requestedHorizontalVelocity;
        private Vector3 _pendingHorizontalDisplacement;
        private Quaternion _targetRotation;
        private float _pendingJumpSpeed = -1f;
        private bool _hasRotationTarget;
        private bool _snapRotation;
        private bool _hasReportedMissingPhysicsComponents;

        /// <summary>
        /// 获取当前选中的水平移动来源。
        /// </summary>
        public PlayerMovementMode MovementMode => _movementMode;

        /// <summary>
        /// 获取玩家当前的水平前方方向。
        /// </summary>
        public Vector3 Forward =>
            _hasRotationTarget ? _targetRotation * Vector3.forward : transform.forward;

        /// <summary>
        /// 获取玩家逻辑根节点当前或待应用的世界旋转。
        /// </summary>
        public Quaternion FacingRotation =>
            _hasRotationTarget ? _targetRotation : transform.rotation;

        /// <summary>
        /// 获取角色是否与坡度允许的地面保持物理接触。
        /// </summary>
        public bool IsGrounded => _groundColliders.Count > 0;

        /// <summary>
        /// 缓存必需的刚体与胶囊碰撞体，并初始化物理旋转目标。
        /// </summary>
        private void Awake()
        {
            if (!EnsurePhysicsComponents())
            {
                return;
            }

            _targetRotation = _rigidbody.rotation;
        }

        /// <summary>
        /// 在编辑器内修改组件时自动回填同一对象上的刚体与胶囊碰撞体引用。
        /// </summary>
        private void OnValidate()
        {
            EnsurePhysicsComponents();
        }

        /// <summary>
        /// 在固定物理步中统一应用水平速度、跳跃速度和朝向。
        /// </summary>
        private void FixedUpdate()
        {
            if (!EnsurePhysicsComponents())
            {
                return;
            }

            // 死亡/过场会临时把刚体设为 Kinematic。此时 Unity 禁止写速度，
            // 并且这些帧的位移请求也不应该在复活后一次性补发。
            if (_rigidbody.isKinematic)
            {
                _pendingHorizontalDisplacement = Vector3.zero;
                _requestedHorizontalVelocity = Vector3.zero;
                _pendingJumpSpeed = -1f;
                return;
            }

            ApplyRotation();

            var velocity = _rigidbody.linearVelocity;
            var horizontalVelocity = _movementMode == PlayerMovementMode.Scripted
                ? _requestedHorizontalVelocity
                : _pendingHorizontalDisplacement / Time.fixedDeltaTime;
            velocity.x = horizontalVelocity.x;
            velocity.z = horizontalVelocity.z;

            if (_pendingJumpSpeed >= 0f)
            {
                velocity.y = _pendingJumpSpeed;
                _pendingJumpSpeed = -1f;
                _groundColliders.Clear();
            }

            _rigidbody.linearVelocity = velocity;
            _pendingHorizontalDisplacement = Vector3.zero;
        }

        /// <summary>
        /// 获取移动组件是否已找到可用的刚体与胶囊碰撞体。
        /// </summary>
        public bool IsReady => EnsurePhysicsComponents();

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
            ResetHorizontalRequest();
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
            var movementMode = movementPolicy == PlayerAnimationMovementPolicy.RootMotion
                ? PlayerMovementMode.AnimationRootMotion
                : movementPolicy == PlayerAnimationMovementPolicy.AuthoredMotion
                    ? PlayerMovementMode.AuthoredMotion
                    : PlayerMovementMode.Scripted;
            if (_movementMode != movementMode)
            {
                _movementMode = movementMode;
                ResetHorizontalRequest();
            }

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
                _requestedHorizontalVelocity = Vector3.zero;
                return;
            }

            FaceMovementDirection(movementDirection);
            _requestedHorizontalVelocity = movementDirection * speed;
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
        /// 在下一个物理步立即将玩家转向指定的水平世界方向。
        /// </summary>
        /// <param name="worldDirection">需要面对的世界空间水平向量。</param>
        public void FaceDirectionImmediately(Vector3 worldDirection)
        {
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            _targetRotation = Quaternion.LookRotation(worldDirection.normalized, Vector3.up);
            _hasRotationTarget = true;
            _snapRotation = true;
        }

        /// <summary>
        /// 在动画 Root Motion 状态激活时，把动画位移交给刚体移动链。
        /// </summary>
        /// <param name="deltaPosition">Animator 在当前帧报告的位移。</param>
        public void ApplyRootMotion(Vector3 deltaPosition)
        {
            if (_movementMode != PlayerMovementMode.AnimationRootMotion)
            {
                return;
            }

            deltaPosition.y = 0f;
            _pendingHorizontalDisplacement += deltaPosition * _rootMotionPositionScale;
        }

        /// <summary>
        /// 在数据驱动位移状态激活时，沿锁定方向累计本帧动画曲线产生的位移。
        /// 位移最终由动态刚体在物理步中执行，因此仍会响应墙体和地面碰撞。
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
            if (worldDirection.sqrMagnitude > 0.0001f)
            {
                _pendingHorizontalDisplacement += worldDirection.normalized * distanceDelta;
            }
        }

        /// <summary>
        /// 在角色位于地面时安排一次向上的刚体初速度。
        /// </summary>
        /// <param name="jumpHeight">期望达到的最高跳跃高度。</param>
        /// <returns>成功安排起跳时返回 true；空中再次请求时返回 false。</returns>
        public bool TryJump(float jumpHeight)
        {
            if (!EnsurePhysicsComponents() || !IsGrounded || jumpHeight <= 0f)
            {
                return false;
            }

            var gravity = Physics.gravity.y;
            if (gravity >= 0f)
            {
                return false;
            }

            _pendingJumpSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);
            return true;
        }

        /// <summary>
        /// 当状态刻意不提供脚本水平移动时，仅保留刚体的垂直物理运动。
        /// </summary>
        public void MoveVerticalOnly()
        {
            if (_movementMode == PlayerMovementMode.Scripted)
            {
                _requestedHorizontalVelocity = Vector3.zero;
            }
        }

        /// <summary>
        /// 将角色旋转目标更新为本次输入换算得到的实际水平移动方向。
        /// </summary>
        private void FaceMovementDirection(Vector3 movementDirection)
        {
            if (movementDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            _targetRotation = Quaternion.LookRotation(
                movementDirection.normalized,
                Vector3.up);
            _hasRotationTarget = true;
            _snapRotation = false;
        }

        /// <summary>
        /// 在固定物理步中平滑或立即应用待处理朝向。
        /// </summary>
        private void ApplyRotation()
        {
            if (!_hasRotationTarget)
            {
                return;
            }

            var rotation = _snapRotation
                ? _targetRotation
                : Quaternion.RotateTowards(
                    _rigidbody.rotation,
                    _targetRotation,
                    _cameraFacingTurnSpeed * Time.fixedDeltaTime);
            _rigidbody.MoveRotation(rotation);
            _snapRotation = false;
        }

        /// <summary>
        /// 清除切换位移模式时不再适用的水平移动请求。
        /// </summary>
        private void ResetHorizontalRequest()
        {
            _requestedHorizontalVelocity = Vector3.zero;
            _pendingHorizontalDisplacement = Vector3.zero;
        }

        /// <summary>
        /// 收集满足最大坡度限制的刚体接触面，供落地与跳跃判断使用。
        /// </summary>
        private void OnCollisionStay(Collision collision)
        {
            var minimumGroundNormalY = Mathf.Cos(_maximumGroundAngle * Mathf.Deg2Rad);
            var hasGroundContact = false;
            for (var i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y >= minimumGroundNormalY)
                {
                    hasGroundContact = true;
                    break;
                }
            }

            if (hasGroundContact)
            {
                _groundColliders.Add(collision.collider);
            }
            else
            {
                _groundColliders.Remove(collision.collider);
            }
        }

        /// <summary>
        /// 移除已经离开的地面碰撞体。
        /// </summary>
        private void OnCollisionExit(Collision collision)
        {
            _groundColliders.Remove(collision.collider);
        }

        /// <summary>
        /// 确保移动组件拥有同一对象上的 Rigidbody 与 CapsuleCollider。
        /// </summary>
        private bool EnsurePhysicsComponents()
        {
            _rigidbody ??= GetComponent<Rigidbody>();
            _capsuleCollider ??= GetComponent<CapsuleCollider>();
            if (_rigidbody != null && _capsuleCollider != null)
            {
                _hasReportedMissingPhysicsComponents = false;
                return true;
            }

            if (!_hasReportedMissingPhysicsComponents)
            {
                Debug.LogError(
                    "PlayerMotor 必须与 Rigidbody 和 CapsuleCollider 挂在同一个对象上。",
                    this);
                _hasReportedMissingPhysicsComponents = true;
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
