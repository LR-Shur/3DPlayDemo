using Train.Dialogue.Core;

namespace Train.Dialogue.Events
{
    /// <summary>
    /// 表示对话已经正常完成或被取消，二者可通过快照状态区分。
    /// </summary>
    public readonly struct DialogueCompletedEvent
    {
        /// <summary>创建对话终止事件。</summary>
        public DialogueCompletedEvent(DialogueSessionSnapshot snapshot)
        {
            Snapshot = snapshot;
        }

        /// <summary>获取终止后的不可变会话快照。</summary>
        public DialogueSessionSnapshot Snapshot { get; }

        /// <summary>获取对话是否沿图正常完成。</summary>
        public bool WasCompleted =>
            Snapshot?.Status == DialogueSessionStatus.Completed;
    }
}
