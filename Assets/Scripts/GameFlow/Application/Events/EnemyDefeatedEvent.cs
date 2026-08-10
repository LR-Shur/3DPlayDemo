namespace Train.GameFlow.Application.Events
{
    /// <summary>
    /// A stable, gameplay-level fact for quests, rewards and analytics.
    /// It intentionally contains IDs instead of scene object references.
    /// </summary>
    public readonly struct EnemyDefeatedEvent
    {
        public EnemyDefeatedEvent(
            string levelId,
            string enemyArchetypeId,
            string enemyInstanceId,
            string killerId)
        {
            LevelId = levelId;
            EnemyArchetypeId = enemyArchetypeId;
            EnemyInstanceId = enemyInstanceId;
            KillerId = killerId;
        }

        public string LevelId { get; }
        public string EnemyArchetypeId { get; }
        public string EnemyInstanceId { get; }
        public string KillerId { get; }
    }
}
