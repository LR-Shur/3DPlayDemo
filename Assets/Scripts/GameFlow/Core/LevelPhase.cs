namespace Train.GameFlow.Core
{
    /// <summary>
    /// 表示关卡会话的粗粒度生命周期阶段。
    /// </summary>
    public enum LevelPhase
    {
        /// <summary>尚未启动关卡流程。</summary>
        None = 0,

        /// <summary>正在准备资源和参战对象。</summary>
        Preparing = 1,

        /// <summary>正在播放关卡开场流程。</summary>
        Intro = 2,

        /// <summary>正在进行战斗。</summary>
        Combat = 3,

        /// <summary>已满足通关条件。</summary>
        Cleared = 4,

        /// <summary>已满足失败条件。</summary>
        Failed = 5,

        /// <summary>正在退出关卡。</summary>
        Exiting = 6
    }
}
