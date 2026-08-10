namespace Train.Quest.Events
{
    /// <summary>
    /// 区分一条任务变化消息对应的领域状态转换。
    /// </summary>
    public enum QuestChangeKind
    {
        Accepted = 0,
        Progressed = 1,
        Completed = 2,
        Claimed = 3
    }
}
