namespace Train.WorldInteraction.Runtime
{
    /// <summary>为特定交互目标提供独立的有效距离。</summary>
    public interface IInteractionRangeProvider
    {
        /// <summary>玩家与交互点之间允许触发交互的最大距离。</summary>
        float InteractionRange { get; }
    }
}
