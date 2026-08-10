using System;
using UnityEngine;
using UnityEngine.Events;

namespace Train.Gameplay.Combat
{
    /// <summary>
    /// 通用生命值组件。玩家、敌人和场景物件均可复用。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float _maxHealth = 100f;
        [SerializeField] private bool _startAtFullHealth = true;
        [SerializeField, Min(0f)] private float _invulnerabilitySeconds;
        [SerializeField] private bool _destroyOnDeath;
        [SerializeField, Min(0f)] private float _destroyDelay;
        [SerializeField] private UnityEvent _onDamaged = new();
        [SerializeField] private UnityEvent _onDeath = new();
        [SerializeField] private UnityEvent _onRevived = new();

        private float _currentHealth;
        private float _lastDamageTime = float.NegativeInfinity;

        public event Action<DamageInfo, DamageResult> Damaged;
        public event Action<DamageInfo> Died;
        public event Action Revived;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public bool IsAlive => _currentHealth > 0f;

        private void Awake()
        {
            if (_startAtFullHealth || _currentHealth <= 0f)
            {
                _currentHealth = _maxHealth;
            }
        }

        private void OnValidate()
        {
            _maxHealth = Mathf.Max(1f, _maxHealth);
            _currentHealth = Mathf.Clamp(_currentHealth, 0f, _maxHealth);
        }

        public DamageResult TakeDamage(DamageInfo damageInfo)
        {
            if (!IsAlive ||
                damageInfo.Amount <= 0f ||
                Time.time < _lastDamageTime + _invulnerabilitySeconds)
            {
                return new DamageResult(0f, _currentHealth, false);
            }

            _lastDamageTime = Time.time;
            var previousHealth = _currentHealth;
            _currentHealth = Mathf.Max(0f, _currentHealth - damageInfo.Amount);
            var killed = previousHealth > 0f && _currentHealth <= 0f;
            var result = new DamageResult(previousHealth - _currentHealth, _currentHealth, killed);

            _onDamaged.Invoke();
            Damaged?.Invoke(damageInfo, result);

            if (killed)
            {
                _onDeath.Invoke();
                Died?.Invoke(damageInfo);
                if (_destroyOnDeath)
                {
                    Destroy(gameObject, _destroyDelay);
                }
            }

            return result;
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || !IsAlive)
            {
                return;
            }

            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
        }

        public void Revive(float healthPercent = 1f)
        {
            _currentHealth = _maxHealth * Mathf.Clamp01(healthPercent);
            if (_currentHealth <= 0f)
            {
                _currentHealth = _maxHealth;
            }

            _lastDamageTime = float.NegativeInfinity;
            _onRevived.Invoke();
            Revived?.Invoke();
        }

        public void SetMaxHealth(float maxHealth, bool refill = true)
        {
            _maxHealth = Mathf.Max(1f, maxHealth);
            _currentHealth = refill
                ? _maxHealth
                : Mathf.Clamp(_currentHealth, 0f, _maxHealth);
        }
    }
}
