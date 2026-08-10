namespace Train.Composition.Progression
{
    /// <summary>
    /// 关卡成长进度变化事件。
    /// </summary>
    public readonly struct ProgressionChangedEvent
    {
        public ProgressionChangedEvent(RunProgressSnapshot snapshot)
        {
            Snapshot = snapshot;
        }

        public RunProgressSnapshot Snapshot { get; }
    }
}
