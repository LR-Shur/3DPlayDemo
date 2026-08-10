using Train.Dialogue.Core;

namespace Train.Dialogue.Events
{
    /// <summary>
    /// 表示玩家已经提交一个有效对话选项。
    /// </summary>
    public readonly struct DialogueChoiceSelectedEvent
    {
        /// <summary>创建选项提交事件。</summary>
        public DialogueChoiceSelectedEvent(
            string sessionId,
            string dialogueId,
            DialogueChoiceSnapshot choice,
            long revision)
        {
            SessionId = sessionId;
            DialogueId = dialogueId;
            Choice = choice;
            Revision = revision;
        }

        /// <summary>获取所属会话稳定标识。</summary>
        public string SessionId { get; }

        /// <summary>获取所属对话图稳定标识。</summary>
        public string DialogueId { get; }

        /// <summary>获取被选择选项的不可变快照。</summary>
        public DialogueChoiceSnapshot Choice { get; }

        /// <summary>获取选择完成后的会话修订号。</summary>
        public long Revision { get; }
    }
}
