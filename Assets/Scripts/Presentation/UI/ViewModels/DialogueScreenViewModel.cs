using System;
using System.Collections.Generic;

namespace Train.Presentation.UI.ViewModels
{
    /// <summary>
    /// 对话浮层一次渲染所需的不可变数据。
    /// </summary>
    public sealed class DialogueScreenViewModel
    {
        /// <summary>
        /// 创建对话视图模型。
        /// </summary>
        public DialogueScreenViewModel(
            bool isActive,
            string dialogueId,
            string title,
            string speakerId,
            string text,
            IReadOnlyList<string> choiceTexts,
            bool awaitingChoice)
        {
            IsActive = isActive;
            DialogueId = dialogueId ?? string.Empty;
            Title = title ?? string.Empty;
            SpeakerId = speakerId ?? string.Empty;
            Text = text ?? string.Empty;
            ChoiceTexts = choiceTexts ??
                throw new ArgumentNullException(nameof(choiceTexts));
            AwaitingChoice = awaitingChoice;
        }

        /// <summary>获取对话浮层是否应显示。</summary>
        public bool IsActive { get; }

        /// <summary>获取对话图标识。</summary>
        public string DialogueId { get; }

        /// <summary>获取对话标题。</summary>
        public string Title { get; }

        /// <summary>获取当前说话角色标识。</summary>
        public string SpeakerId { get; }

        /// <summary>获取当前台词或选择提示。</summary>
        public string Text { get; }

        /// <summary>获取当前可见选项文本。</summary>
        public IReadOnlyList<string> ChoiceTexts { get; }

        /// <summary>获取当前是否等待玩家选择。</summary>
        public bool AwaitingChoice { get; }

        /// <summary>获取隐藏状态的共享实例。</summary>
        public static DialogueScreenViewModel Hidden { get; } =
            new(false, string.Empty, string.Empty, string.Empty, string.Empty,
                Array.Empty<string>(), false);
    }
}
