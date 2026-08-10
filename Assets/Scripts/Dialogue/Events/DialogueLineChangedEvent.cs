using Train.Dialogue.Core;

namespace Train.Dialogue.Events
{
    /// <summary>
    /// 表示当前台词或选择提示已经改变，表现层可据此刷新。
    /// </summary>
    public readonly struct DialogueLineChangedEvent
    {
        /// <summary>创建当前内容改变事件。</summary>
        public DialogueLineChangedEvent(DialogueSessionSnapshot snapshot)
        {
            Snapshot = snapshot;
        }

        /// <summary>获取变化后的不可变会话快照。</summary>
        public DialogueSessionSnapshot Snapshot { get; }
    }
}
