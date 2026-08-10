namespace Train.GameFlow.Application.Events
{
    /// <summary>
    /// 表示一个关卡已经被玩家成功完成。
    /// </summary>
    public readonly struct LevelCompletedEvent
    {
        public LevelCompletedEvent(string levelId, float elapsedSeconds)
        {
            LevelId = levelId;
            ElapsedSeconds = elapsedSeconds;
        }

        public string LevelId { get; }
        public float ElapsedSeconds { get; }
    }
}
