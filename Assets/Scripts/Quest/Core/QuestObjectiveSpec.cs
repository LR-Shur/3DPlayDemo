using System;

namespace Train.Quest.Core
{
    /// <summary>
    /// 保存一条任务目标的不可变领域定义。
    /// </summary>
    public sealed class QuestObjectiveSpec
    {
        public QuestObjectiveSpec(
            string id,
            QuestFactType type,
            string targetId,
            int requiredAmount)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(
                    "Quest objective id cannot be null, empty, or whitespace.",
                    nameof(id));
            }

            if (!Enum.IsDefined(typeof(QuestFactType), type))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(type),
                    type,
                    "Quest objective fact type is not defined.");
            }

            if (string.IsNullOrWhiteSpace(targetId))
            {
                throw new ArgumentException(
                    "Quest objective target id cannot be null, empty, or whitespace.",
                    nameof(targetId));
            }

            if (requiredAmount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredAmount),
                    requiredAmount,
                    "Quest objective required amount must be greater than zero.");
            }

            Id = id;
            Type = type;
            TargetId = targetId;
            RequiredAmount = requiredAmount;
        }

        public string Id { get; }

        public QuestFactType Type { get; }

        public string TargetId { get; }

        public int RequiredAmount { get; }
    }
}
