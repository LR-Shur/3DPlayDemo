using System;
using Train.Architecture.Bootstrap;
using Train.Composition.Progression;
using Train.Dialogue.Events;
using UnityEngine;

namespace Train.Shop.Runtime
{
    /// <summary>
    /// 商店 NPC 的应用层桥接：只监听指定对话完成事件，不依赖 UI 具体实现。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShopkeeperDialogueBridge : MonoBehaviour
    {
        [SerializeField] private string _dialogueId = "dialogue_shopkeeper";
        private IDisposable _subscription;

        /// <summary>配置此 NPC 完成后应打开的对话图 ID。</summary>
        public void Configure(string dialogueId)
        {
            _dialogueId = dialogueId;
        }

        private void Awake()
        {
            _subscription = GameBootstrap.EnsureExists()
                .Context.Events
                .Subscribe<DialogueCompletedEvent>(OnDialogueCompleted);
        }

        private void OnDialogueCompleted(DialogueCompletedEvent message)
        {
            if (!message.WasCompleted ||
                message.Snapshot == null ||
                !string.Equals(message.Snapshot.DialogueId, _dialogueId, StringComparison.Ordinal))
            {
                return;
            }

            FindFirstObjectByType<ProgressionRuntimeController>()?.OpenShopFromNpc();
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
            _subscription = null;
        }
    }
}
