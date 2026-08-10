namespace Train.Quest.Core
{
    /// <summary>
    /// 表示任务从未接取到奖励已领取的生命周期状态。
    /// </summary>
    public enum QuestStatus
    {
        Inactive = 0,
        Active = 1,
        Completed = 2,
        Claimed = 3
    }
}
