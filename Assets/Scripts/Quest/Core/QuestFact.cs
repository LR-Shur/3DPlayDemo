using System;

namespace Train.Quest.Core
{
    /// <summary>
    /// 描述一条可能推进一个或多个活动任务目标的领域事实。
    /// 目标标识使用序号精确匹配，不包含隐式通配规则。
    /// </summary>
    public readonly struct QuestFact
    {
        public QuestFact(
            QuestFactType type,
            string targetId,
            int amount = 1)
        {
            Type = type;
            TargetId = targetId;
            Amount = amount;
            Validate();
        }

        public QuestFactType Type { get; }

        public string TargetId { get; }

        public int Amount { get; }

        internal void Validate()
        {
            ValidateFactType(Type);

            if (string.IsNullOrWhiteSpace(TargetId))
            {
                throw new ArgumentException(
                    "Quest fact target id cannot be null, empty, or whitespace.",
                    nameof(TargetId));
            }

            if (Amount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(Amount),
                    Amount,
                    "Quest fact amount must be greater than zero.");
            }
        }

        private static void ValidateFactType(QuestFactType type)
        {
            if (!Enum.IsDefined(typeof(QuestFactType), type))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(type),
                    type,
                    "Quest fact type is not defined.");
            }
        }
    }
}
