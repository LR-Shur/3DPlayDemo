namespace Train.GameFlow.Application
{
    /// <summary>
    /// 维护当前激活的关卡只读会话，供常驻 UI 等跨场景系统查询。
    /// </summary>
    public interface ILevelSessionRegistry
    {
        /// <summary>
        /// 获取当前激活的关卡只读模型；没有关卡时为 <see langword="null"/>。
        /// </summary>
        ILevelReadModel Current { get; }

        /// <summary>
        /// 将一个关卡只读模型登记为当前会话。
        /// </summary>
        /// <param name="readModel">要登记的关卡只读模型。</param>
        void Attach(ILevelReadModel readModel);

        /// <summary>
        /// 当指定模型仍是当前会话时将其移除。
        /// </summary>
        /// <param name="readModel">要移除的关卡只读模型。</param>
        void Detach(ILevelReadModel readModel);
    }
}
