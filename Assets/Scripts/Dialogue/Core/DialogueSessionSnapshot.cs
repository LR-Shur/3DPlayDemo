using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Train.Dialogue.Core
{
    /// <summary>
    /// 保存对话会话在某一修订版本的不可变表现数据。
    /// </summary>
    public sealed class DialogueSessionSnapshot
    {
        private readonly ReadOnlyCollection<DialogueChoiceSnapshot> _choices;

        /// <summary>创建一个对话会话快照。</summary>
        public DialogueSessionSnapshot(
            string sessionId,
            string dialogueId,
            string title,
            DialogueSessionStatus status,
            long revision,
            string currentNodeId,
            DialogueNodeKind? currentNodeKind,
            string speakerId,
            string text,
            IEnumerable<DialogueChoiceSnapshot> choices)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                throw new ArgumentException(
                    "对话会话标识不能为空。",
                    nameof(sessionId));
            }

            SessionId = sessionId;
            DialogueId = dialogueId ?? string.Empty;
            Title = title ?? string.Empty;
            Status = status;
            Revision = revision;
            CurrentNodeId = currentNodeId;
            CurrentNodeKind = currentNodeKind;
            SpeakerId = speakerId ?? string.Empty;
            Text = text ?? string.Empty;

            var copy = new List<DialogueChoiceSnapshot>();
            if (choices != null)
            {
                foreach (var choice in choices)
                {
                    copy.Add(
                        choice ?? throw new ArgumentException(
                            "会话快照选项不能包含 null。",
                            nameof(choices)));
                }
            }

            _choices = copy.AsReadOnly();
        }

        /// <summary>获取本次会话的稳定标识。</summary>
        public string SessionId { get; }

        /// <summary>获取对话图稳定标识。</summary>
        public string DialogueId { get; }

        /// <summary>获取对话标题。</summary>
        public string Title { get; }

        /// <summary>获取会话状态。</summary>
        public DialogueSessionStatus Status { get; }

        /// <summary>获取会话修订号。</summary>
        public long Revision { get; }

        /// <summary>获取当前节点标识；终态或未开始时可为空。</summary>
        public string CurrentNodeId { get; }

        /// <summary>获取当前节点类型；终态或未开始时为空。</summary>
        public DialogueNodeKind? CurrentNodeKind { get; }

        /// <summary>获取当前说话角色标识。</summary>
        public string SpeakerId { get; }

        /// <summary>获取当前台词或选项提示文本。</summary>
        public string Text { get; }

        /// <summary>获取当前可见选项的只读防御性副本。</summary>
        public IReadOnlyList<DialogueChoiceSnapshot> Choices => _choices;

        /// <summary>获取会话是否已经正常结束或被取消。</summary>
        public bool IsTerminal =>
            Status == DialogueSessionStatus.Completed ||
            Status == DialogueSessionStatus.Cancelled;
    }
}
