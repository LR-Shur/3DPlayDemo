using System;
using Train.Quest.Core;

namespace Train.Quest.Events
{
    /// <summary>
    /// 携带一次任务状态或进度变化后的不可变快照。
    /// </summary>
    public readonly struct QuestChangedEvent
    {
        /// <summary>创建任务变化消息。</summary>
        public QuestChangedEvent(
            QuestChangeKind kind,
            QuestProgressSnapshot snapshot)
        {
            Kind = kind;
            Snapshot = snapshot ??
                throw new ArgumentNullException(nameof(snapshot));
        }

        /// <summary>获取此次变化的种类。</summary>
        public QuestChangeKind Kind { get; }

        /// <summary>获取变化完成后的任务快照。</summary>
        public QuestProgressSnapshot Snapshot { get; }
    }
}
