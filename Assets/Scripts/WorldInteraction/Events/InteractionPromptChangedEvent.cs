using Train.Inventory.Data;

namespace Train.WorldInteraction.Events
{
    /// <summary>
    /// 当前世界交互焦点变化后发送给 UI 层的不可变事件。
    /// UI 只读取展示数据，不持有或调用场景中的拾取物组件。
    /// </summary>
    public readonly struct InteractionPromptChangedEvent
    {
        /// <summary>
        /// 创建一次交互提示变化事件。
        /// </summary>
        public InteractionPromptChangedEvent(
            bool isVisible,
            string interactionId,
            string displayName,
            int quantity,
            ItemRarity rarity,
            string actionLabel = "拾取")
        {
            IsVisible = isVisible;
            InteractionId = interactionId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Quantity = quantity;
            Rarity = rarity;
            ActionLabel = string.IsNullOrWhiteSpace(actionLabel)
                ? "交互"
                : actionLabel;
        }

        /// <summary>
        /// 是否应显示交互提示。
        /// </summary>
        public bool IsVisible { get; }

        /// <summary>
        /// 当前焦点的稳定交互标识。
        /// </summary>
        public string InteractionId { get; }

        /// <summary>
        /// 当前物品的本地化显示名称。
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// 本次可拾取数量。
        /// </summary>
        public int Quantity { get; }

        /// <summary>
        /// 当前物品品质。
        /// </summary>
        public ItemRarity Rarity { get; }

        /// <summary>
        /// 当前交互动作的简短标签，例如“拾取”或“对话”。
        /// </summary>
        public string ActionLabel { get; }

        /// <summary>
        /// 创建隐藏提示所需的空事件。
        /// </summary>
        public static InteractionPromptChangedEvent Hidden()
        {
            return new InteractionPromptChangedEvent(
                false,
                string.Empty,
                string.Empty,
                0,
                ItemRarity.Common,
                string.Empty);
        }
    }
}
