namespace Train.GameFlow.Core
{
    /// <summary>
    /// 表示关卡阶段转换请求的执行结果。
    /// 同阶段请求保持幂等，其余非法请求会直接抛出异常。
    /// </summary>
    public enum LevelTransitionResult
    {
        /// <summary>阶段转换已成功完成。</summary>
        Completed = 0,

        /// <summary>请求目标与当前阶段相同，因此没有重复执行。</summary>
        IgnoredSamePhase = 1
    }
}
