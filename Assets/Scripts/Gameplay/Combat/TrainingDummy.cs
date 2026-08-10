using System.Collections;
using UnityEngine;

namespace Train.Gameplay.Combat
{
    /// <summary>
    /// 训练假人行为：显示受击日志，死亡后自动恢复生命值。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class TrainingDummy : MonoBehaviour
    {
        [SerializeField] private bool _autoRevive = true;
        [SerializeField, Min(0f)] private float _reviveDelay = 2f;
        [SerializeField] private bool _logDamage = true;

        private Health _health;
        private Coroutine _reviveRoutine;

        private void Awake()
        {
            _health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            _health ??= GetComponent<Health>();
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (_health == null)
            {
                return;
            }

            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }

        private void OnDamaged(DamageInfo info, DamageResult result)
        {
            if (_logDamage)
            {
                Debug.Log(
                    $"{name} 受到 {result.AppliedDamage:0.#} 点 {info.DamageType} 伤害，" +
                    $"剩余生命 {result.RemainingHealth:0.#}/{_health.MaxHealth:0.#}",
                    this);
            }
        }

        private void OnDied(DamageInfo info)
        {
            if (_autoRevive && _reviveRoutine == null)
            {
                _reviveRoutine = StartCoroutine(ReviveAfterDelay());
            }
        }

        private IEnumerator ReviveAfterDelay()
        {
            yield return new WaitForSeconds(_reviveDelay);
            _health.Revive();
            _reviveRoutine = null;
        }
    }
}
