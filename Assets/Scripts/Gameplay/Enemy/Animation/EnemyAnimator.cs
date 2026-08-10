using Train.Gameplay.Enemy.Abstractions;
using UnityEngine;

namespace Train.Gameplay.Enemy.Animation
{
    /// <summary>
    /// 使用 Unity Animator 播放敌人动作的表现组件。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyAnimator : MonoBehaviour, IEnemyAnimation
    {
        [SerializeField] private Animator _animator;

        public Animator Animator => ResolveAnimator();
        public bool IsReady =>
            ResolveAnimator() != null &&
            ResolveAnimator().runtimeAnimatorController != null;

        private void Awake()
        {
            ResolveAnimator();
            if (!IsReady)
            {
                Debug.LogError(
                    "EnemyAnimator needs an Animator with a Runtime Animator Controller.",
                    this);
            }
        }

        public void Play(EnemyAnimationId animationId, float fadeSeconds = 0.12f)
        {
            var animator = ResolveAnimator();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            animator.CrossFade(animationId.ToString(), Mathf.Max(0f, fadeSeconds), 0);
        }

        private Animator ResolveAnimator()
        {
            _animator ??= GetComponentInChildren<Animator>(true);
            return _animator;
        }
    }
}
