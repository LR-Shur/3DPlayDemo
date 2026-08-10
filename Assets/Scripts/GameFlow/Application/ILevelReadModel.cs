namespace Train.GameFlow.Application
{
    /// <summary>
    /// 向表现层提供只读关卡数据，避免 UI 直接依赖可变的运行时控制器。
    /// </summary>
    public interface ILevelReadModel
    {
        /// <summary>
        /// 获取当前关卡会话的只读快照。
        /// </summary>
        LevelReadSnapshot Snapshot { get; }
    }
}
