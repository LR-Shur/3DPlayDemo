using System;

namespace Train.Quest.Core
{
    /// <summary>
    /// 保存单个任务目标在某一时刻的不可变进度。
    /// </summary>
    public sealed class QuestObjectiveProgressSnapshot
    {
        internal QuestObjectiveProgressSnapshot(
            string objectiveId,
            QuestFactType type,
            string targetId,
            int currentAmount,
            int requiredAmount)
        {
            ObjectiveId = objectiveId ??
                throw new ArgumentNullException(nameof(objectiveId));
            Type = type;
            TargetId = targetId ??
                throw new ArgumentNullException(nameof(targetId));
            CurrentAmount = currentAmount;
            RequiredAmount = requiredAmount;
        }

        public string ObjectiveId { get; }

        public QuestFactType Type { get; }

        public string TargetId { get; }

        public int CurrentAmount { get; }

        public int RequiredAmount { get; }

        public bool IsCompleted => CurrentAmount >= RequiredAmount;
    }
}
