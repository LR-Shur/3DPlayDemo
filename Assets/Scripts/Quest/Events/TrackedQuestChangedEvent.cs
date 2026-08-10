namespace Train.Quest.Events
{
    /// <summary>
    /// 表示玩家当前追踪任务发生变化。
    /// </summary>
    public readonly struct TrackedQuestChangedEvent
    {
        /// <summary>创建任务追踪变化消息。</summary>
        public TrackedQuestChangedEvent(
            string previousQuestId,
            string currentQuestId)
        {
            PreviousQuestId = previousQuestId;
            CurrentQuestId = currentQuestId;
        }

        /// <summary>获取变化前的追踪任务标识。</summary>
        public string PreviousQuestId { get; }

        /// <summary>获取变化后的追踪任务标识。</summary>
        public string CurrentQuestId { get; }
    }
}
