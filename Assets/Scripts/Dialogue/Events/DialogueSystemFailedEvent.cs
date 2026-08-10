namespace Train.Dialogue.Events
{
    /// <summary>
    /// 表示对话系统初始化失败。
    /// </summary>
    public readonly struct DialogueSystemFailedEvent
    {
        /// <summary>创建对话系统失败消息。</summary>
        public DialogueSystemFailedEvent(string reason)
        {
            Reason = reason ?? string.Empty;
        }

        /// <summary>获取失败原因。</summary>
        public string Reason { get; }
    }
}
