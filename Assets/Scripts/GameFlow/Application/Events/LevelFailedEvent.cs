namespace Train.GameFlow.Application.Events
{
    /// <summary>
    /// 表示一个关卡以失败告终，并携带失败原因。
    /// </summary>
    public readonly struct LevelFailedEvent
    {
        public LevelFailedEvent(string levelId, string reason)
        {
            LevelId = levelId;
            Reason = reason;
        }

        public string LevelId { get; }
        public string Reason { get; }
    }
}
