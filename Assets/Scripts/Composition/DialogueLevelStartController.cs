using System;
using Train.Architecture.Bootstrap;
using Train.Dialogue.Events;
using Train.GameFlow.Runtime;
using UnityEngine;

namespace Train.Composition
{
    /// <summary>
    /// 将训练终端对话完成事件转换为第一关开始指令。
    /// 该适配器位于组合层，避免 GameFlow 直接依赖 Dialogue 模块。
    /// </summary>
    [DefaultExecutionOrder(-8300)]
    [DisallowMultipleComponent]
    public sealed class DialogueLevelStartController : MonoBehaviour
    {
        private IDisposable _dialogueSubscription;

        private void Awake()
        {
            _dialogueSubscription = GameBootstrap.EnsureExists()
                .Context.Events
                .Subscribe<DialogueCompletedEvent>(OnDialogueCompleted);
        }

        private void OnDialogueCompleted(DialogueCompletedEvent message)
        {
            if (!message.WasCompleted ||
                message.Snapshot == null ||
                !string.Equals(
                    message.Snapshot.DialogueId,
                    "dialogue_training_operator",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var levels = FindObjectsByType<LevelRuntimeController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (var level in levels)
            {
                if (level.Definition == null ||
                    !string.Equals(
                        level.Definition.LevelId,
                        "combat_001",
                        StringComparison.OrdinalIgnoreCase) ||
                    !level.IsWaitingForStart)
                {
                    continue;
                }

                level.StartLevel();
            }
        }

        private void OnDestroy()
        {
            _dialogueSubscription?.Dispose();
            _dialogueSubscription = null;
        }
    }
}
