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

        private Vector3 _destination;
        private Vector3 _externalPull;
        private bool _movementEnabled = true;

        public Vector3 Position => transform.position;
        public bool HasReachedDestination
        {
            get
            {
                if (TryEnsureAgentOnNavMesh())
                {
                    return !_agent.pathPending &&
                           _agent.remainingDistance <= Mathf.Max(_agent.stoppingDistance, 0.05f);
                }

                return true;
            }
        }

        private void Awake()
        {
            ResolveAgent();
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
            if (TryEnsureAgentOnNavMesh())
            {
                _agent.isStopped = false;
                _agent.speed = Mathf.Max(0f, speed);
                _agent.SetDestination(destination);
            }
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
            var agent = ResolveAgent();
            if (agent != null && agent.enabled)
            {
                agent.isStopped = true;
                if (agent.isOnNavMesh)
                {
                    agent.ResetPath();
                }
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

        /// <summary>累积本帧由主动道具施加的位移，在状态机移动之后统一应用。</summary>
        public void ApplyExternalPull(Vector3 displacement)
        {
            if (_movementEnabled && TryEnsureAgentOnNavMesh())
            {
                _externalPull += displacement;
            }
        }

        private void LateUpdate()
        {
            if (!_movementEnabled || _externalPull.sqrMagnitude <= 0.000001f)
            {
                _externalPull = Vector3.zero;
                return;
            }

            if (!TryEnsureAgentOnNavMesh())
            {
                _externalPull = Vector3.zero;
                return;
            }

            var start = _agent.nextPosition;
            var desired = start + _externalPull;
            if (NavMesh.Raycast(start, desired, out var hit, NavMesh.AllAreas))
            {
                desired = hit.position;
            }

            _agent.Move(desired - start);

            _externalPull = Vector3.zero;
        }

        private bool TryEnsureAgentOnNavMesh()
        {
            var agent = ResolveAgent();
            if (agent == null || !agent.enabled)
            {
                return false;
            }

            if (agent.isOnNavMesh)
            {
                return true;
            }

            if (NavMesh.SamplePosition(transform.position, out var hit, .75f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }

            if (!agent.isOnNavMesh)
            {
                agent.isStopped = true;
                return false;
            }

            return true;
        }

        private NavMeshAgent ResolveAgent()
        {
            if (_agent != null && _agent.gameObject != null)
            {
                return _agent;
            }

            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null)
            {
                _agent = GetComponentInChildren<NavMeshAgent>(true);
            }

            return _agent;
        }
    }
}
