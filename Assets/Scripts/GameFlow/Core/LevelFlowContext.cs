namespace Train.GameFlow.Core
{
    /// <summary>
    /// 关卡流程状态共享的业务上下文。
    /// 状态只依赖这个端口，不直接依赖场景控制器或 Unity 对象。
    /// </summary>
    public sealed class LevelFlowContext
    {
        /// <summary>创建关卡流程上下文。</summary>
        public LevelFlowContext(ILevelFlowActions actions)
        {
            Actions = actions;
        }

        /// <summary>获取执行场景工作的流程端口。</summary>
        public ILevelFlowActions Actions { get; }
    }
}
