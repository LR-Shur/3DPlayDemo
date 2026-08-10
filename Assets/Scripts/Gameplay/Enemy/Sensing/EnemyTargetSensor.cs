using Train.Gameplay.Combat;
using Train.Gameplay.Enemy.Abstractions;
using UnityEngine;

namespace Train.Gameplay.Enemy.Sensing
{
    /// <summary>
    /// 按距离与视野条件寻找玩家目标并暴露给敌人状态机。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyTargetSensor : MonoBehaviour, IEnemySensor
    {
        [SerializeField, Min(0.1f)] private float _detectionRadius = 8f;
        [SerializeField, Min(0.1f)] private float _loseTargetRadius = 12f;
        [SerializeField, Min(0.02f)] private float _scanInterval = 0.2f;
        [SerializeField] private LayerMask _targetLayers = ~0;
        [SerializeField] private string _requiredTag = "Player";

        private readonly Collider[] _overlaps = new Collider[24];
        private float _nextScanTime;

        public Transform Target { get; private set; }
        public bool HasTarget => Target != null;
        public float DistanceToTarget =>
            Target == null ? float.PositiveInfinity : Vector3.Distance(transform.position, Target.position);

        public void Tick()
        {
            if (Target != null)
            {
                if (!IsValidTarget(Target) || DistanceToTarget > _loseTargetRadius)
                {
                    ClearTarget();
                }

                return;
            }

            if (Time.time < _nextScanTime)
            {
                return;
            }

            _nextScanTime = Time.time + _scanInterval;
            ScanForTarget();
        }

        public void ClearTarget()
        {
            Target = null;
        }

        private void ScanForTarget()
        {
            var hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                _detectionRadius,
                _overlaps,
                _targetLayers,
                QueryTriggerInteraction.Ignore);

            var nearestSqrDistance = float.PositiveInfinity;
            Transform nearest = null;

            for (var i = 0; i < hitCount; i++)
            {
                var candidate = _overlaps[i];
                if (candidate == null ||
                    candidate.transform.IsChildOf(transform) ||
                    (!string.IsNullOrWhiteSpace(_requiredTag) &&
                     !candidate.transform.root.CompareTag(_requiredTag)))
                {
                    continue;
                }

                var candidateRoot = candidate.transform.root;
                if (!IsValidTarget(candidateRoot))
                {
                    continue;
                }

                var sqrDistance = (candidateRoot.position - transform.position).sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance)
                {
                    continue;
                }

                nearestSqrDistance = sqrDistance;
                nearest = candidateRoot;
            }

            Target = nearest != null ? nearest : FindTaggedTarget();
        }

        private Transform FindTaggedTarget()
        {
            if (string.IsNullOrWhiteSpace(_requiredTag))
            {
                return null;
            }

            try
            {
                var taggedObject = GameObject.FindGameObjectWithTag(_requiredTag);
                var target = taggedObject != null ? taggedObject.transform.root : null;
                return IsValidTarget(target) ? target : null;
            }
            catch (UnityException)
            {
                Debug.LogWarning($"Enemy target tag '{_requiredTag}' is not defined.", this);
                return null;
            }
        }

        private static bool IsValidTarget(Transform target)
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                return false;
            }

            var health = target.GetComponent<Health>();
            return health == null || health.IsAlive;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.65f, 0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, _detectionRadius);
        }
    }
}
