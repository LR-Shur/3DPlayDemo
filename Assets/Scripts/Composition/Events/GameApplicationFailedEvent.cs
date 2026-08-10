namespace Train.Composition.Events
{
    /// <summary>
    /// 表示应用级启动失败，并携带失败原因供 UI 与日志展示。
    /// </summary>
    public readonly struct GameApplicationFailedEvent
    {
        public GameApplicationFailedEvent(string reason)
        {
            Reason = reason;
        }

        public string Reason { get; }
    }
}
