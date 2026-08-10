using System;

namespace Train.Quest.Core
{
    /// <summary>
    /// 携带任务领域模型一次提交前后的不可变快照。
    /// </summary>
    public sealed class QuestProgressChangedEventArgs : EventArgs
    {
        public QuestProgressChangedEventArgs(
            QuestProgressSnapshot oldSnapshot,
            QuestProgressSnapshot newSnapshot)
        {
            OldSnapshot = oldSnapshot ??
                throw new ArgumentNullException(nameof(oldSnapshot));
            NewSnapshot = newSnapshot ??
                throw new ArgumentNullException(nameof(newSnapshot));
        }

        public QuestProgressSnapshot OldSnapshot { get; }

        public QuestProgressSnapshot NewSnapshot { get; }
    }
}
