using Train.Gameplay.Enemy.Abstractions;
using UnityEngine;
using UnityEngine.AI;

namespace Train.Gameplay.Enemy.Movement
{
    /// <summary>
    /// 在指定半径内为敌人提供随机巡逻点。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyPatrolArea : MonoBehaviour, IEnemyPatrol
    {
        [SerializeField, Min(0.5f)] private float _radius = 4f;
        [SerializeField, Min(1)] private int _sampleAttempts = 8;

        private Vector3 _homePosition;

        private void Awake()
        {
            _homePosition = transform.position;
        }

        public bool TryGetNextDestination(Vector3 currentPosition, out Vector3 destination)
        {
            for (var i = 0; i < _sampleAttempts; i++)
            {
                var random = Random.insideUnitCircle * _radius;
                var candidate = _homePosition + new Vector3(random.x, 0f, random.y);
                if (NavMesh.SamplePosition(candidate, out var hit, 1.5f, NavMesh.AllAreas))
                {
                    destination = hit.position;
                    return true;
                }
            }

            var fallback = Random.insideUnitCircle * _radius;
            destination = _homePosition + new Vector3(fallback.x, 0f, fallback.y);
            return true;
        }

        public void ResetPatrol()
        {
            _homePosition = transform.position;
        }
    }
}
