namespace Train.Quest.Events
{
    /// <summary>
    /// 表示任务目录、任务实例和事件订阅已经可以使用。
    /// </summary>
    public readonly struct QuestSystemReadyEvent
    {
        /// <summary>创建任务系统就绪消息。</summary>
        public QuestSystemReadyEvent(
            int questCount,
            int acceptedQuestCount,
            string trackedQuestId)
        {
            QuestCount = questCount;
            AcceptedQuestCount = acceptedQuestCount;
            TrackedQuestId = trackedQuestId;
        }

        /// <summary>获取任务目录总数。</summary>
        public int QuestCount { get; }

        /// <summary>获取初始化时已经接取的任务数量。</summary>
        public int AcceptedQuestCount { get; }

        /// <summary>获取初始化时正在追踪的任务标识。</summary>
        public string TrackedQuestId { get; }
    }
}
