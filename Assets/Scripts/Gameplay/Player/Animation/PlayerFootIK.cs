using Animancer;
using Train.Gameplay.Player.Movement;
using UnityEngine;

namespace Train.Gameplay.Player.Animation
{
    /// <summary>
    /// 使用 Humanoid Animator IK 将双脚贴合到真实物理地面，并通过骨盆补偿减少腿部拉伸。
    /// </summary>
    [RequireComponent(typeof(Animator), typeof(AnimancerComponent))]
    public sealed class PlayerFootIK : MonoBehaviour
    {
        private const int RaycastHitCapacity = 8;

        [SerializeField] private Animator _animator;
        [SerializeField] private AnimancerComponent _animancer;
        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private LayerMask _groundLayers = ~0;
        [SerializeField, Min(0f)] private float _raycastOriginHeight = 0.45f;
        [SerializeField, Min(0.01f)] private float _raycastDistance = 0.8f;
        [SerializeField, Min(0f)] private float _soleOffset = 0.015f;
        [SerializeField, Range(0f, 0.5f)] private float _maximumPelvisDrop = 0.3f;
        [SerializeField, Range(0f, 0.3f)] private float _maximumPelvisRaise = 0.15f;
        [SerializeField, Min(0f)] private float _blendSpeed = 12f;

        private readonly RaycastHit[] _raycastHits = new RaycastHit[RaycastHitCapacity];
        private Rigidbody _playerRigidbody;
        private float _ikWeight;
        private float _pelvisOffset;

        /// <summary>
        /// 查找依赖并通知 Animancer 每帧执行 Animator IK 回调。
        /// </summary>
        private void Awake()
        {
            EnsureReferences();
            if (_animancer != null)
            {
                _animancer.Layers[0].ApplyAnimatorIK = true;
            }
        }

        /// <summary>
        /// 在 Inspector 修改后自动回填同对象 Animator、Animancer 与父级 PlayerMotor。
        /// </summary>
        private void OnValidate()
        {
            EnsureReferences();
        }

        /// <summary>
        /// 在动画姿势求值之后，把脚掌和骨盆调整到碰撞地面。
        /// </summary>
        private void OnAnimatorIK(int layerIndex)
        {
            if (!EnsureReferences() || !_animator.isHuman)
            {
                return;
            }

            var targetWeight = _motor.IsGrounded ? 1f : 0f;
            var blend = 1f - Mathf.Exp(-_blendSpeed * Time.deltaTime);
            _ikWeight = Mathf.Lerp(_ikWeight, targetWeight, blend);

            var hasLeftGround = TryGetFootTarget(
                AvatarIKGoal.LeftFoot,
                _animator.leftFeetBottomHeight,
                out var leftPosition,
                out var leftRotation,
                out var leftOffset);
            var hasRightGround = TryGetFootTarget(
                AvatarIKGoal.RightFoot,
                _animator.rightFeetBottomHeight,
                out var rightPosition,
                out var rightRotation,
                out var rightOffset);

            var desiredPelvisOffset = 0f;
            if (hasLeftGround && hasRightGround)
            {
                desiredPelvisOffset = Mathf.Min(leftOffset, rightOffset);
            }
            else if (hasLeftGround)
            {
                desiredPelvisOffset = leftOffset;
            }
            else if (hasRightGround)
            {
                desiredPelvisOffset = rightOffset;
            }

            desiredPelvisOffset = Mathf.Clamp(
                desiredPelvisOffset,
                -_maximumPelvisDrop,
                _maximumPelvisRaise);
            _pelvisOffset = Mathf.Lerp(
                _pelvisOffset,
                desiredPelvisOffset * _ikWeight,
                blend);
            _animator.bodyPosition += Vector3.up * _pelvisOffset;

            ApplyFootTarget(
                AvatarIKGoal.LeftFoot,
                hasLeftGround,
                leftPosition,
                leftRotation);
            ApplyFootTarget(
                AvatarIKGoal.RightFoot,
                hasRightGround,
                rightPosition,
                rightRotation);
        }

        /// <summary>
        /// 从当前动画脚掌位置向下探测，并计算贴合地面的目标姿势。
        /// </summary>
        private bool TryGetFootTarget(
            AvatarIKGoal goal,
            float footBottomHeight,
            out Vector3 targetPosition,
            out Quaternion targetRotation,
            out float verticalOffset)
        {
            var animatedPosition = _animator.GetIKPosition(goal);
            var animatedRotation = _animator.GetIKRotation(goal);
            var rayOrigin = animatedPosition + Vector3.up * _raycastOriginHeight;
            var hitCount = Physics.RaycastNonAlloc(
                rayOrigin,
                Vector3.down,
                _raycastHits,
                _raycastDistance,
                _groundLayers,
                QueryTriggerInteraction.Ignore);

            var closestDistance = float.PositiveInfinity;
            RaycastHit closestHit = default;
            for (var i = 0; i < hitCount; i++)
            {
                var hit = _raycastHits[i];
                if (hit.collider == null ||
                    hit.collider.attachedRigidbody == _playerRigidbody ||
                    hit.distance >= closestDistance)
                {
                    continue;
                }

                closestDistance = hit.distance;
                closestHit = hit;
            }

            if (float.IsPositiveInfinity(closestDistance))
            {
                targetPosition = animatedPosition;
                targetRotation = animatedRotation;
                verticalOffset = 0f;
                return false;
            }

            targetPosition =
                closestHit.point +
                closestHit.normal * (footBottomHeight + _soleOffset);
            targetRotation =
                Quaternion.FromToRotation(
                    animatedRotation * Vector3.up,
                    closestHit.normal) *
                animatedRotation;
            verticalOffset = targetPosition.y - animatedPosition.y;
            return true;
        }

        /// <summary>
        /// 将单脚目标及权重提交给 Humanoid Animator IK。
        /// </summary>
        private void ApplyFootTarget(
            AvatarIKGoal goal,
            bool hasGround,
            Vector3 targetPosition,
            Quaternion targetRotation)
        {
            var weight = hasGround ? _ikWeight : 0f;
            _animator.SetIKPositionWeight(goal, weight);
            _animator.SetIKRotationWeight(goal, weight);
            if (weight <= 0f)
            {
                return;
            }

            _animator.SetIKPosition(goal, targetPosition);
            _animator.SetIKRotation(goal, targetRotation);
        }

        /// <summary>
        /// 缓存 Animator、Animancer、PlayerMotor 和玩家刚体引用。
        /// </summary>
        private bool EnsureReferences()
        {
            _animator ??= GetComponent<Animator>();
            _animancer ??= GetComponent<AnimancerComponent>();
            _motor ??= GetComponentInParent<PlayerMotor>();
            _playerRigidbody ??= _motor != null ? _motor.GetComponent<Rigidbody>() : null;
            return _animator != null &&
                   _animancer != null &&
                   _motor != null &&
                   _playerRigidbody != null;
        }
    }
}
