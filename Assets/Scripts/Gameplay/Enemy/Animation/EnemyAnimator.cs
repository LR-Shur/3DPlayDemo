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

        private EnemyAttackMotion _attackMotion;
        private float _resumeSpeed = 1f;

        public Animator Animator => ResolveAnimator();
        public bool IsReady =>
            ResolveAnimator() != null &&
            ResolveAnimator().runtimeAnimatorController != null;

        private void Awake()
        {
            ResolveAnimator();
            _attackMotion = GetComponent<EnemyAttackMotion>();
            if (!IsReady)
            {
                Debug.LogError(
                    "EnemyAnimator needs an Animator with a Runtime Animator Controller.",
                    this);
            }
        }

        public void Play(EnemyAnimationId animationId, float fadeSeconds = 0.12f)
        {
            if (animationId == EnemyAnimationId.Attack)
            {
                _attackMotion ??= GetComponent<EnemyAttackMotion>();
                _attackMotion?.PlayAttack();
            }

            var animator = ResolveAnimator();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            animator.CrossFade(animationId.ToString(), Mathf.Max(0f, fadeSeconds), 0);
        }

        /// <summary>冻结时暂停 Animator，解除冻结后恢复原播放速度。</summary>
        public void SetPlaybackPaused(bool paused)
        {
            var animator = ResolveAnimator();
            if (animator == null)
            {
                return;
            }

            if (paused)
            {
                _resumeSpeed = Mathf.Max(0.01f, animator.speed);
                animator.speed = 0f;
            }
            else
            {
                animator.speed = _resumeSpeed;
            }
        }

        private Animator ResolveAnimator()
        {
            _animator ??= GetComponentInChildren<Animator>(true);
            return _animator;
        }
    }
}
