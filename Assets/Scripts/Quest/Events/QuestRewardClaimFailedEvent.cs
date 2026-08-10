using Train.Quest.Application;

namespace Train.Quest.Events
{
    /// <summary>
    /// 表示任务奖励未能发放，任务会保持在可再次领取的状态。
    /// </summary>
    public readonly struct QuestRewardClaimFailedEvent
    {
        /// <summary>创建奖励领取失败消息。</summary>
        public QuestRewardClaimFailedEvent(
            string questId,
            QuestRewardClaimFailureReason reason)
        {
            QuestId = questId ?? string.Empty;
            Reason = reason;
        }

        /// <summary>获取尝试领取的任务标识。</summary>
        public string QuestId { get; }

        /// <summary>获取结构化失败原因。</summary>
        public QuestRewardClaimFailureReason Reason { get; }
    }
}
