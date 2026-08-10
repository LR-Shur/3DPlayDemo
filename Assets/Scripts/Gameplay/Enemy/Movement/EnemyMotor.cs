using Train.Gameplay.Enemy.Abstractions;
using UnityEngine;
using UnityEngine.AI;

namespace Train.Gameplay.Enemy.Movement
{
    /// <summary>
    /// 通过 NavMeshAgent 驱动敌人移动与转向的表现组件。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyMotor : MonoBehaviour, IEnemyMotor
    {
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField, Min(0.01f)] private float _fallbackStoppingDistance = 0.15f;

        private Vector3 _destination;
        private bool _movementEnabled = true;

        public Vector3 Position => transform.position;
        public bool HasReachedDestination
        {
            get
            {
                if (CanUseAgent)
                {
                    return !_agent.pathPending &&
                           _agent.remainingDistance <= Mathf.Max(_agent.stoppingDistance, 0.05f);
                }

                return Vector3.Distance(transform.position, _destination) <= _fallbackStoppingDistance;
            }
        }

        private void Awake()
        {
            _agent ??= GetComponent<NavMeshAgent>();
            if (_agent != null)
            {
                _agent.updateRotation = false;
            }

            _destination = transform.position;
        }

        public void MoveTo(Vector3 destination, float speed)
        {
            if (!_movementEnabled)
            {
                return;
            }

            _destination = destination;
            if (CanUseAgent)
            {
                _agent.isStopped = false;
                _agent.speed = Mathf.Max(0f, speed);
                _agent.SetDestination(destination);
                return;
            }

            var nextPosition = Vector3.MoveTowards(
                transform.position,
                destination,
                Mathf.Max(0f, speed) * Time.deltaTime);
            transform.position = nextPosition;
        }

        public void Face(Vector3 worldPosition, float turnSpeed)
        {
            var direction = worldPosition - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var targetRotation = Quaternion.LookRotation(direction.normalized);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Mathf.Max(0f, turnSpeed) * Time.deltaTime);
        }

        public void Stop()
        {
            _destination = transform.position;
            if (CanUseAgent)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }
        }

        public void SetMovementEnabled(bool enabled)
        {
            _movementEnabled = enabled;
            if (!enabled)
            {
                Stop();
            }
        }

        private bool CanUseAgent =>
            _agent != null && _agent.enabled && _agent.isOnNavMesh;
    }
}
