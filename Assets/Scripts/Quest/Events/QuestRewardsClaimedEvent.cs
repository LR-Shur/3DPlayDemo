using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Train.Quest.Core;

namespace Train.Quest.Events
{
    /// <summary>
    /// 表示一项任务的全部背包奖励已经成功发放。
    /// </summary>
    public sealed class QuestRewardsClaimedEvent
    {
        private readonly ReadOnlyCollection<QuestRewardSpec> _rewards;

        /// <summary>创建奖励领取完成消息。</summary>
        public QuestRewardsClaimedEvent(
            string questId,
            QuestRewardSpec[] rewards)
        {
            QuestId = questId ??
                throw new ArgumentNullException(nameof(questId));
            _rewards = Array.AsReadOnly(
                rewards ??
                throw new ArgumentNullException(nameof(rewards)));
        }

        /// <summary>获取已领取奖励所属任务的稳定标识。</summary>
        public string QuestId { get; }

        /// <summary>获取本次成功发放的只读奖励列表。</summary>
        public IReadOnlyList<QuestRewardSpec> Rewards => _rewards;
    }
}
