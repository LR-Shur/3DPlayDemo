namespace Train.WorldInteraction.Core
{
    /// <summary>
    /// 可交互目标的最小领域接口。
    /// Unity 场景组件、宝箱或地图拾取物可以在外层实现此接口。
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// 目标的稳定唯一标识，用于候选去重和稳定排序。
        /// </summary>
        string InteractionId { get; }

        /// <summary>
        /// 目标此刻是否允许交互。
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// 执行一次交互并返回业务结果。
        /// </summary>
        InteractResult Interact();
    }
}
