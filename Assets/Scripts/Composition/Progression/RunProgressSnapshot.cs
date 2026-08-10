namespace Train.Composition.Progression
{
    /// <summary>
    /// 表现层读取的跨关卡成长快照。
    /// </summary>
    public readonly struct RunProgressSnapshot
    {
        public RunProgressSnapshot(
            int coins,
            int currentNodeIndex,
            int clearedNodeCount,
            string currentLevelId)
        {
            Coins = coins;
            CurrentNodeIndex = currentNodeIndex;
            ClearedNodeCount = clearedNodeCount;
            CurrentLevelId = currentLevelId;
        }

        public int Coins { get; }
        public int CurrentNodeIndex { get; }
        public int ClearedNodeCount { get; }
        public string CurrentLevelId { get; }
    }
}
