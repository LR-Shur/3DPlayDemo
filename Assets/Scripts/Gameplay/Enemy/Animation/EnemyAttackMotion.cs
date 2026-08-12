using System.Collections;
using UnityEngine;

namespace Train.Gameplay.Enemy.Animation
{
    /// <summary>给没有骨骼动画的敌人提供明确的蓄力、出招、收招动作。</summary>
    [DisallowMultipleComponent]
    public sealed class EnemyAttackMotion : MonoBehaviour
    {
        public enum AttackStyle
        {
            QuickSlash,
            HeavyStrike,
            CasterBurst,
            MachineLunge,
        }

        [SerializeField] private Transform _visualRoot;
        [SerializeField] private Transform _weaponRoot;
        [SerializeField] private AttackStyle _style = AttackStyle.MachineLunge;

        private Coroutine _routine;
        private Vector3 _visualPosition;
        private Quaternion _visualRotation;
        private Vector3 _weaponPosition;
        private Quaternion _weaponRotation;

        private void Awake()
        {
            ResolveReferences();
            CacheRestPose();
        }

        public void PlayAttack()
        {
            ResolveReferences();
            if (_routine != null) StopCoroutine(_routine);
            RestorePose();
            _routine = StartCoroutine(AttackRoutine());
        }

        private IEnumerator AttackRoutine()
        {
            var windup = _style == AttackStyle.QuickSlash ? .12f : .32f;
            var strike = _style == AttackStyle.QuickSlash ? .14f : .2f;
            var recover = _style == AttackStyle.QuickSlash ? .22f : .34f;

            yield return AnimatePhase(windup, 0f, 1f);
            yield return AnimatePhase(strike, 1f, 0f);
            yield return AnimatePhase(recover, 0f, -1f);
            RestorePose();
            _routine = null;
        }

        private IEnumerator AnimatePhase(float duration, float from, float to)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = t * t * (3f - 2f * t);
                ApplyPose(Mathf.Lerp(from, to, eased));
                yield return null;
            }

            ApplyPose(to);
        }

        private void ApplyPose(float phase)
        {
            if (_visualRoot != null)
            {
                var forward = _style == AttackStyle.CasterBurst ? .08f : .16f;
                var lift = _style == AttackStyle.CasterBurst ? .12f : .035f;
                var pitch = _style == AttackStyle.HeavyStrike ? -18f : -10f;
                var roll = _style == AttackStyle.QuickSlash ? -10f : 0f;
                _visualRoot.localPosition = _visualPosition + Vector3.forward * (forward * phase) + Vector3.up * (lift * phase);
                _visualRoot.localRotation = _visualRotation * Quaternion.Euler(pitch * phase, 0f, roll * phase);
            }

            if (_weaponRoot != null)
            {
                var swing = _style == AttackStyle.CasterBurst ? 28f : 62f;
                var lift = _style == AttackStyle.HeavyStrike ? 18f : 8f;
                _weaponRoot.localPosition = _weaponPosition + Vector3.forward * (.1f * phase) + Vector3.up * (.08f * phase);
                _weaponRoot.localRotation = _weaponRotation * Quaternion.Euler(-swing * phase, lift * phase, 0f);
            }
        }

        private void ResolveReferences()
        {
            if (_visualRoot == null)
            {
                var renderers = GetComponentsInChildren<Renderer>(true);
                for (var i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i].transform == transform) continue;
                    _visualRoot = renderers[i].transform.root == transform
                        ? renderers[i].transform.parent
                        : renderers[i].transform.root;
                    if (_visualRoot != null) break;
                }
            }

            if (_weaponRoot == null)
            {
                var transforms = GetComponentsInChildren<Transform>(true);
                for (var i = 0; i < transforms.Length; i++)
                {
                    if (transforms[i].name.StartsWith("WeaponSilhouette"))
                    {
                        _weaponRoot = transforms[i];
                        break;
                    }
                }
            }
        }

        private void CacheRestPose()
        {
            if (_visualRoot != null)
            {
                _visualPosition = _visualRoot.localPosition;
                _visualRotation = _visualRoot.localRotation;
            }

            if (_weaponRoot != null)
            {
                _weaponPosition = _weaponRoot.localPosition;
                _weaponRotation = _weaponRoot.localRotation;
            }
        }

        private void RestorePose()
        {
            if (_visualRoot != null)
            {
                _visualRoot.localPosition = _visualPosition;
                _visualRoot.localRotation = _visualRotation;
            }

            if (_weaponRoot != null)
            {
                _weaponRoot.localPosition = _weaponPosition;
                _weaponRoot.localRotation = _weaponRotation;
            }
        }
    }
}
