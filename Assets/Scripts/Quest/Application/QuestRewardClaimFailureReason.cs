namespace Train.Quest.Application
{
    /// <summary>
    /// 说明任务奖励领取失败的可预期原因。
    /// </summary>
    public enum QuestRewardClaimFailureReason
    {
        None = 0,
        QuestNotFound = 1,
        QuestNotCompleted = 2,
        InvalidReward = 3,
        InventoryFull = 4,
        InventoryChanged = 5
    }
}
