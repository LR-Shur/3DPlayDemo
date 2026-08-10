using Train.GameFlow.Core;

namespace Train.Presentation.UI.ViewModels
{
    /// <summary>
    /// 汇总战斗 HUD 一次完整渲染所需的不可变数据。
    /// </summary>
    public sealed class HudViewModel
    {
        /// <summary>创建一份战斗 HUD 视图模型。</summary>
        public HudViewModel(
            bool hasLevel,
            string levelId,
            string levelName,
            LevelPhase phase,
            LevelOutcome outcome,
            float playerHealth,
            float playerMaxHealth,
            int defeatedEnemyCount,
            int enemyCount,
            int playerDeathCount,
            string objectiveText,
            HudBannerViewModel banner)
        {
            HasLevel = hasLevel;
            LevelId = levelId ?? string.Empty;
            LevelName = levelName ?? string.Empty;
            Phase = phase;
            Outcome = outcome;
            PlayerHealth = playerHealth;
            PlayerMaxHealth = playerMaxHealth;
            DefeatedEnemyCount = defeatedEnemyCount;
            EnemyCount = enemyCount;
            PlayerDeathCount = playerDeathCount;
            ObjectiveText = objectiveText ?? string.Empty;
            Banner = banner ?? HudBannerViewModel.None;
        }

        /// <summary>获取当前是否存在有效关卡会话。</summary>
        public bool HasLevel { get; }

        /// <summary>获取关卡稳定标识。</summary>
        public string LevelId { get; }

        /// <summary>获取关卡展示名称。</summary>
        public string LevelName { get; }

        /// <summary>获取当前关卡阶段。</summary>
        public LevelPhase Phase { get; }

        /// <summary>获取当前关卡结果。</summary>
        public LevelOutcome Outcome { get; }

        /// <summary>获取玩家当前生命值。</summary>
        public float PlayerHealth { get; }

        /// <summary>获取玩家生命值上限。</summary>
        public float PlayerMaxHealth { get; }

        /// <summary>获取被限制在零到一之间的生命值比例。</summary>
        public float Health01 =>
            PlayerMaxHealth <= 0f
                ? 0f
                : Clamp01(PlayerHealth / PlayerMaxHealth);

        /// <summary>获取已经击败的敌人数。</summary>
        public int DefeatedEnemyCount { get; }

        /// <summary>获取本关敌人总数。</summary>
        public int EnemyCount { get; }

        /// <summary>获取尚未击败的敌人数。</summary>
        public int RemainingEnemyCount =>
            System.Math.Max(0, EnemyCount - DefeatedEnemyCount);

        /// <summary>获取玩家在本关中的死亡次数。</summary>
        public int PlayerDeathCount { get; }

        /// <summary>获取当前任务目标文案。</summary>
        public string ObjectiveText { get; }

        /// <summary>获取当前中央提示横幅数据。</summary>
        public HudBannerViewModel Banner { get; }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }
    }
}
