namespace Train.GameFlow.Core
{
    /// <summary>
    /// 表示一次关卡会话的最终结果。
    /// </summary>
    public enum LevelOutcome
    {
        /// <summary>尚未产生结果。</summary>
        None = 0,

        /// <summary>关卡已成功完成。</summary>
        Cleared = 1,

        /// <summary>关卡挑战失败。</summary>
        Failed = 2
    }
}
