using Train.Dialogue.Core;

namespace Train.Dialogue.Events
{
    /// <summary>
    /// 表示应用层已经成功创建并启动一场对话会话。
    /// </summary>
    public readonly struct DialogueStartedEvent
    {
        /// <summary>创建对话开始事件。</summary>
        public DialogueStartedEvent(DialogueSessionSnapshot snapshot)
        {
            Snapshot = snapshot;
        }

        /// <summary>获取开始后的不可变会话快照。</summary>
        public DialogueSessionSnapshot Snapshot { get; }
    }
}
