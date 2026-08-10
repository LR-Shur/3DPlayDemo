namespace Train.Quest.Events
{
    /// <summary>
    /// 表示任务系统因配置或构建错误而初始化失败。
    /// </summary>
    public readonly struct QuestSystemFailedEvent
    {
        /// <summary>创建任务系统失败消息。</summary>
        public QuestSystemFailedEvent(string reason)
        {
            Reason = reason ?? string.Empty;
        }

        /// <summary>获取便于定位问题的失败原因。</summary>
        public string Reason { get; }
    }
}
