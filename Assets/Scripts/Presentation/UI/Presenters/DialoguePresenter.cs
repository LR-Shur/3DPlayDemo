using System;
using System.Collections.Generic;
using Train.Architecture.Events;
using Train.Architecture.Input;
using Train.Dialogue.Application;
using Train.Dialogue.Core;
using Train.Dialogue.Events;
using Train.Presentation.UI.Contracts;
using Train.Presentation.UI.ViewModels;

namespace Train.Presentation.UI.Presenters
{
    /// <summary>
    /// 连接对话 Server、输入模态和对话浮层。
    /// 会话打开时申请模态输入租约，关闭或结束时自动释放。
    /// </summary>
    public sealed class DialoguePresenter : IDisposable
    {
        private readonly IDialogueService _dialogue;
        private readonly IDialogueView _view;
        private readonly IInputModeService _inputMode;
        private readonly List<IDisposable> _subscriptions = new();
        private IDisposable _modalLease;
        private bool _disposed;

        /// <summary>
        /// 创建对话 Presenter、订阅会话事件并隐藏浮层。
        /// </summary>
        public DialoguePresenter(
            IDialogueService dialogue,
            IEventBus events,
            IInputModeService inputMode,
            IDialogueView view)
        {
            _dialogue = dialogue ??
                throw new ArgumentNullException(nameof(dialogue));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _inputMode = inputMode ??
                throw new ArgumentNullException(nameof(inputMode));
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            _view.ContinueRequested += OnContinueRequested;
            _view.ChoiceRequested += OnChoiceRequested;
            _view.CloseRequested += OnCloseRequested;
            _subscriptions.Add(
                events.Subscribe<DialogueStartedEvent>(
                    OnDialogueStarted));
            _subscriptions.Add(
                events.Subscribe<DialogueLineChangedEvent>(
                    OnDialogueChanged));
            _subscriptions.Add(
                events.Subscribe<DialogueCompletedEvent>(
                    OnDialogueCompleted));

            _view.Render(DialogueScreenViewModel.Hidden);
        }

        /// <summary>获取对话浮层当前是否显示。</summary>
        public bool IsOpen =>
            _modalLease != null;

        /// <summary>断开视图与事件订阅并释放模态租约。</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _view.ContinueRequested -= OnContinueRequested;
            _view.ChoiceRequested -= OnChoiceRequested;
            _view.CloseRequested -= OnCloseRequested;
            for (var i = _subscriptions.Count - 1; i >= 0; i--)
            {
                _subscriptions[i].Dispose();
            }

            _subscriptions.Clear();
            _modalLease?.Dispose();
            _modalLease = null;
            _view.Render(DialogueScreenViewModel.Hidden);
            _disposed = true;
        }

        /// <summary>取消当前对话并隐藏浮层。</summary>
        public void HideDialogue()
        {
            if (_disposed || !_dialogue.IsActive)
            {
                return;
            }

            _dialogue.Cancel();
            _view.Render(DialogueScreenViewModel.Hidden);
            ReleaseModal();
        }

        private void OnDialogueStarted(DialogueStartedEvent message)
        {
            _modalLease ??=
                _inputMode.AcquireModal("Dialogue");
            Render(message.Snapshot);
        }

        private void OnDialogueChanged(DialogueLineChangedEvent message)
        {
            Render(message.Snapshot);
        }

        private void OnDialogueCompleted(DialogueCompletedEvent message)
        {
            _view.Render(DialogueScreenViewModel.Hidden);
            ReleaseModal();
        }

        private void OnContinueRequested()
        {
            _dialogue.Continue();
        }

        private void OnChoiceRequested(int index)
        {
            var snapshot = _dialogue.Current;
            if (snapshot == null || index < 0 || index >= snapshot.Choices.Count)
            {
                return;
            }

            _dialogue.Choose(snapshot.Choices[index].ChoiceId);
        }

        private void OnCloseRequested()
        {
            HideDialogue();
        }

        private void Render(DialogueSessionSnapshot snapshot)
        {
            if (_disposed || snapshot == null)
            {
                return;
            }

            var choices = new List<string>();
            foreach (var choice in snapshot.Choices)
            {
                choices.Add(choice.Text);
            }

            _view.Render(
                new DialogueScreenViewModel(
                    true,
                    snapshot.DialogueId,
                    snapshot.Title,
                    FormatSpeaker(snapshot.SpeakerId),
                    snapshot.Text,
                    choices,
                    snapshot.Status ==
                        DialogueSessionStatus.AwaitingChoice));
        }

        private void ReleaseModal()
        {
            _modalLease?.Dispose();
            _modalLease = null;
        }

        /// <summary>
        /// 把角色稳定标识映射为更适合展示的中文称呼。
        /// </summary>
        private static string FormatSpeaker(string speakerId)
        {
            return speakerId switch
            {
                "character.rusk" => "鲁斯克",
                "character.captain_lyra" => "莱拉队长",
                "character.training_operator" => "训练终端",
                "character.player" => "你",
                _ => speakerId
            };
        }
    }
}
