using System.Collections.Generic;
using Train.Quest.Core;
using Train.Quest.Data;

namespace Train.Quest.Application
{
    /// <summary>
    /// 向表现层和其他玩法模块提供任务接取、追踪、推进与奖励领取用例。
    /// </summary>
    public interface IQuestService
    {
        /// <summary>获取按策划顺序排列的任务定义目录。</summary>
        IReadOnlyList<QuestDefinition> Catalog { get; }

        /// <summary>获取全部任务当前状态的不可变快照。</summary>
        IReadOnlyList<QuestProgressSnapshot> Snapshots { get; }

        /// <summary>获取当前追踪任务标识；没有追踪任务时为空。</summary>
        string TrackedQuestId { get; }

        /// <summary>尝试接取一项尚未激活的任务。</summary>
        bool AcceptQuest(string questId);

        /// <summary>尝试把已接取且未领取的任务设为当前追踪任务。</summary>
        bool TrackQuest(string questId);

        /// <summary>取消当前任务追踪。</summary>
        void ClearTrackedQuest();

        /// <summary>把一条领域事实应用到所有活动任务。</summary>
        bool ApplyFact(QuestFact fact);

        /// <summary>尝试原子发放奖励，并在成功后把任务标记为已领取。</summary>
        QuestRewardClaimResult ClaimRewards(string questId);

        /// <summary>尝试获取指定任务的即时进度快照。</summary>
        bool TryGetSnapshot(
            string questId,
            out QuestProgressSnapshot snapshot);

        /// <summary>尝试获取指定任务的静态定义。</summary>
        bool TryGetDefinition(
            string questId,
            out QuestDefinition definition);
    }
}
