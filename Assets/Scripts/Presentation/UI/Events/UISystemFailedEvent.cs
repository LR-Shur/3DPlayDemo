namespace Train.Presentation.UI.Events
{
    /// <summary>
    /// 表示 UI 系统在启动或运行过程中初始化失败。
    /// </summary>
    public readonly struct UISystemFailedEvent
    {
        /// <summary>创建一条 UI 系统失败消息。</summary>
        public UISystemFailedEvent(string reason)
        {
            Reason = reason;
        }

        /// <summary>获取便于诊断的失败原因。</summary>
        public string Reason { get; }
    }
}
