using System.Collections;
using UnityEngine;

namespace Train.Gameplay.Enemy.Animation
{
    /// <summary>敌人受击时的短促回弹，让僵直和命中反馈在没有专用受击骨骼动画时仍然可见。</summary>
    [DisallowMultipleComponent]
    public sealed class EnemyHitReaction : MonoBehaviour
    {
        [SerializeField] private Transform _visualRoot;
        [SerializeField, Min(0.01f)] private float _duration = 0.16f;
        [SerializeField, Min(0f)] private float _kickDistance = 0.1f;
        [SerializeField, Min(0f)] private float _liftDistance = 0.035f;

        private Coroutine _routine;
        private Vector3 _restPosition;
        private Quaternion _restRotation;

        private void Awake()
        {
            ResolveVisualRoot();
            CacheRestPose();
        }

        public void PlayHit(Vector3 hitDirection)
        {
            ResolveVisualRoot();
            if (_visualRoot == null) return;

            if (_routine != null) StopCoroutine(_routine);
            RestorePose();
            var direction = hitDirection;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.001f) direction = -transform.forward;
            _routine = StartCoroutine(HitRoutine(direction.normalized));
        }

        private IEnumerator HitRoutine(Vector3 direction)
        {
            var elapsed = 0f;
            while (elapsed < _duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / _duration);
                var impulse = Mathf.Sin(t * Mathf.PI);
                _visualRoot.localPosition = _restPosition + direction * (_kickDistance * impulse) +
                    Vector3.up * (_liftDistance * impulse);
                _visualRoot.localRotation = _restRotation * Quaternion.Euler(0f, 0f, -10f * impulse);
                yield return null;
            }

            RestorePose();
            _routine = null;
        }

        private void ResolveVisualRoot()
        {
            if (_visualRoot != null) return;
            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (!renderer.enabled || renderer.transform == transform) continue;
                var candidate = renderer.transform;
                while (candidate.parent != null && candidate.parent != transform)
                {
                    candidate = candidate.parent;
                }

                if (candidate != transform)
                {
                    _visualRoot = candidate;
                    return;
                }
            }
        }

        private void CacheRestPose()
        {
            if (_visualRoot == null) return;
            _restPosition = _visualRoot.localPosition;
            _restRotation = _visualRoot.localRotation;
        }

        private void RestorePose()
        {
            if (_visualRoot == null) return;
            _visualRoot.localPosition = _restPosition;
            _visualRoot.localRotation = _restRotation;
        }
    }
}
