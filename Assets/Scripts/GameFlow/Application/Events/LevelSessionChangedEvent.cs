namespace Train.GameFlow.Application.Events
{
    /// <summary>
    /// 表示当前激活的关卡会话发生了挂载或卸载。
    /// </summary>
    public readonly struct LevelSessionChangedEvent
    {
        /// <summary>
        /// 创建一条关卡会话变化消息。
        /// </summary>
        /// <param name="hasActiveSession">变化后是否存在激活会话。</param>
        public LevelSessionChangedEvent(bool hasActiveSession)
        {
            HasActiveSession = hasActiveSession;
        }

        /// <summary>
        /// 获取变化后是否存在激活会话。
        /// </summary>
        public bool HasActiveSession { get; }
    }
}
