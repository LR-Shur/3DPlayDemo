using Train.GameFlow.Core;

namespace Train.GameFlow.Application
{
    /// <summary>
    /// 表示某一时刻的关卡状态、敌人进度和玩家生命数据。
    /// </summary>
    public readonly struct LevelReadSnapshot
    {
        /// <summary>
        /// 创建一份不可变的关卡只读快照。
        /// </summary>
        public LevelReadSnapshot(
            string levelId,
            string displayName,
            LevelPhase phase,
            LevelOutcome outcome,
            int enemyCount,
            int defeatedEnemyCount,
            float playerHealth,
            float playerMaxHealth,
            int playerDeathCount)
        {
            LevelId = levelId;
            DisplayName = displayName;
            Phase = phase;
            Outcome = outcome;
            EnemyCount = enemyCount;
            DefeatedEnemyCount = defeatedEnemyCount;
            PlayerHealth = playerHealth;
            PlayerMaxHealth = playerMaxHealth;
            PlayerDeathCount = playerDeathCount;
        }

        /// <summary>获取关卡的稳定标识。</summary>
        public string LevelId { get; }

        /// <summary>获取用于界面展示的关卡名称。</summary>
        public string DisplayName { get; }

        /// <summary>获取当前关卡阶段。</summary>
        public LevelPhase Phase { get; }

        /// <summary>获取当前关卡结果。</summary>
        public LevelOutcome Outcome { get; }

        /// <summary>获取本关敌人总数。</summary>
        public int EnemyCount { get; }

        /// <summary>获取已经击败的敌人数。</summary>
        public int DefeatedEnemyCount { get; }

        /// <summary>获取玩家当前生命值。</summary>
        public float PlayerHealth { get; }

        /// <summary>获取玩家生命值上限。</summary>
        public float PlayerMaxHealth { get; }

        /// <summary>获取玩家在本关中的死亡次数。</summary>
        public int PlayerDeathCount { get; }

        /// <summary>获取这份快照是否关联了有效关卡。</summary>
        public bool HasLevel => !string.IsNullOrWhiteSpace(LevelId);
    }
}
