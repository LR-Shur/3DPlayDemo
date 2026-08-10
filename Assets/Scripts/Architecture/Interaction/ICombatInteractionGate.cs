namespace Train.Architecture.Interaction
{
    /// <summary>
    /// 向通用交互层暴露战斗阶段，避免交互组件直接依赖关卡流程实现。
    /// </summary>
    public interface ICombatInteractionGate
    {
        /// <summary>当前是否处于战斗阶段。</summary>
        bool IsCombatActive { get; }
    }
}
