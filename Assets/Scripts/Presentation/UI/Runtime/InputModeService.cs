using System;
using System.Collections.Generic;
using Train.Architecture.Events;
using Train.Architecture.Input;
using Train.Gameplay.Camera;
using Train.Gameplay.Player.Input;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Train.Presentation.UI.Runtime
{
    /// <summary>
    /// 通过可叠加的模态租约统一阻塞玩家与镜头输入，并在场景切换后恢复正确状态。
    /// </summary>
    public sealed class InputModeService : IInputModeService, IDisposable
    {
        private readonly IEventBus _events;
        private readonly Dictionary<int, string> _modalOwners = new();
        private int _nextLeaseId;
        private bool _disposed;

        /// <summary>
        /// 创建输入模式服务并开始监听场景加载事件。
        /// </summary>
        public InputModeService(IEventBus events)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <inheritdoc />
        public bool IsModalActive => _modalOwners.Count > 0;

        /// <inheritdoc />
        public IDisposable AcquireModal(string owner)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(owner))
            {
                throw new ArgumentException(
                    "A modal input lease requires an owner name.",
                    nameof(owner));
            }

            var wasModal = IsModalActive;
            var leaseId = checked(++_nextLeaseId);
            _modalOwners.Add(leaseId, owner);
            NotifyStateChanged(wasModal);
            return new ModalLease(this, leaseId);
        }

        /// <summary>
        /// 停止监听场景事件、释放全部模态状态并恢复游戏输入。
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
            var wasModal = IsModalActive;
            _modalOwners.Clear();
            _disposed = true;
            if (wasModal)
            {
                ApplyGameplayInputBlocked(false);
                _events.Publish(new InputModeChangedEvent(false, 0));
            }
        }

        private void Release(int leaseId)
        {
            if (_disposed || !_modalOwners.Remove(leaseId))
            {
                return;
            }

            NotifyStateChanged(wasModal: true);
        }

        private void NotifyStateChanged(bool wasModal)
        {
            var isModal = IsModalActive;
            if (wasModal != isModal)
            {
                ApplyGameplayInputBlocked(isModal);
            }

            _events.Publish(
                new InputModeChangedEvent(
                    isModal,
                    _modalOwners.Count));
        }

        private static void ApplyGameplayInputBlocked(bool blocked)
        {
            foreach (var input in UnityEngine.Object.FindObjectsByType<
                         PlayerInputReader>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                input.SetExternalInputBlocked(blocked);
            }

            foreach (var cameraInput in UnityEngine.Object.FindObjectsByType<
                         CinemachineLookInputAdapter>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                cameraInput.SetExternalLookBlocked(blocked);
            }

            Cursor.lockState = blocked
                ? CursorLockMode.None
                : CursorLockMode.Locked;
            Cursor.visible = blocked;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyGameplayInputBlocked(IsModalActive);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(InputModeService));
            }
        }

        /// <summary>
        /// 表示一个模态界面对游戏输入阻塞状态的所有权。
        /// </summary>
        private sealed class ModalLease : IDisposable
        {
            private InputModeService _owner;
            private readonly int _leaseId;

            /// <summary>
            /// 创建一份与输入模式服务关联的模态租约。
            /// </summary>
            public ModalLease(InputModeService owner, int leaseId)
            {
                _owner = owner;
                _leaseId = leaseId;
            }

            /// <summary>
            /// 释放此租约；重复释放不会产生额外影响。
            /// </summary>
            public void Dispose()
            {
                var owner = _owner;
                _owner = null;
                owner?.Release(_leaseId);
            }
        }
    }
}
