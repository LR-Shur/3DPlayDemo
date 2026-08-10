using System;

namespace Train.Quest.Core
{
    /// <summary>
    /// 保存一条背包奖励的不可变定义。
    /// 奖励发放属于应用服务职责，不由任务进度领域模型直接操作背包。
    /// </summary>
    public sealed class QuestRewardSpec
    {
        public QuestRewardSpec(string itemId, int amount)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                throw new ArgumentException(
                    "Quest reward item id cannot be null, empty, or whitespace.",
                    nameof(itemId));
            }

            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    amount,
                    "Quest reward amount must be greater than zero.");
            }

            ItemId = itemId;
            Amount = amount;
        }

        public string ItemId { get; }

        public int Amount { get; }
    }
}
