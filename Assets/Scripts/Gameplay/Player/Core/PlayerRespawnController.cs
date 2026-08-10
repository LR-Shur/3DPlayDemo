using System.Collections;
using Train.Architecture.Bootstrap;
using Train.Architecture.Events;
using Train.Gameplay.Combat;
using Train.Gameplay.Player.Application.Events;
using Train.Gameplay.Player.Input;
using UnityEngine;

namespace Train.Gameplay.Player.Core
{
    /// <summary>
    /// 玩家死亡后的延迟复活控制器，负责冻结输入、位移并返回出生点。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class PlayerRespawnController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _respawnDelay = 3f;
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerCombat _combat;
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private Collider _bodyCollider;

        private Health _health;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private bool _originalKinematic;
        private Coroutine _respawnRoutine;
        private IEventBus _events;

        public bool IsRespawning => _respawnRoutine != null;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _events = SceneBootstrap.ResolveEvents(this);
            if (_playerController == null)
            {
                _playerController = GetComponent<PlayerController>();
            }

            if (_input == null)
            {
                _input = GetComponent<PlayerInputReader>();
            }

            if (_combat == null)
            {
                _combat = GetComponent<PlayerCombat>();
            }

            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }

            if (_bodyCollider == null)
            {
                _bodyCollider = GetComponent<Collider>();
            }

            var spawnTransform = _spawnPoint != null ? _spawnPoint : transform;
            _spawnPosition = spawnTransform.position;
            _spawnRotation = spawnTransform.rotation;
            _originalKinematic = _rigidbody != null && _rigidbody.isKinematic;
        }

        private void OnEnable()
        {
            _health ??= GetComponent<Health>();
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.Died -= OnDied;
            }
        }

        private void OnDied(DamageInfo damageInfo)
        {
            if (_respawnRoutine != null)
            {
                return;
            }

            _combat?.SwordHitbox?.EndAttack();
            _playerController?.TryPlayDeath();
            if (_input != null)
            {
                _input.enabled = false;
            }

            if (_bodyCollider != null)
            {
                _bodyCollider.enabled = false;
            }

            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _rigidbody.isKinematic = true;
            }

            _respawnRoutine = StartCoroutine(RespawnAfterDelay());
            _events.Publish(new PlayerRespawnStartedEvent(gameObject, _respawnDelay));
        }

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(_respawnDelay);

            if (_rigidbody != null)
            {
                _rigidbody.position = _spawnPosition;
                _rigidbody.rotation = _spawnRotation;
                _rigidbody.isKinematic = _originalKinematic;
                if (!_rigidbody.isKinematic)
                {
                    _rigidbody.linearVelocity = Vector3.zero;
                    _rigidbody.angularVelocity = Vector3.zero;
                }
            }
            else
            {
                transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
            }

            _health.Revive();
            if (_bodyCollider != null)
            {
                _bodyCollider.enabled = true;
            }

            if (_input != null)
            {
                _input.enabled = true;
            }

            _playerController?.ReturnToLocomotion();
            _respawnRoutine = null;
            _events.Publish(new PlayerRespawnCompletedEvent(gameObject));
        }
    }
}
