using Train.Inventory.Data;
using Train.WorldInteraction.Core;

namespace Train.WorldInteraction.Events
{
    /// <summary>
    /// 世界物品拾取完成后发送给 UI、音效和任务系统的反馈事件。
    /// </summary>
    public readonly struct WorldItemPickupFeedbackEvent
    {
        /// <summary>
        /// 创建一次拾取反馈。
        /// </summary>
        public WorldItemPickupFeedbackEvent(
            string itemId,
            string displayName,
            int quantity,
            ItemRarity rarity,
            InteractResultCode resultCode,
            string message)
        {
            ItemId = itemId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Quantity = quantity;
            Rarity = rarity;
            ResultCode = resultCode;
            Message = message ?? string.Empty;
        }

        /// <summary>
        /// 物品稳定标识。
        /// </summary>
        public string ItemId { get; }

        /// <summary>
        /// 物品显示名称。
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// 尝试拾取的数量。
        /// </summary>
        public int Quantity { get; }

        /// <summary>
        /// 物品品质。
        /// </summary>
        public ItemRarity Rarity { get; }

        /// <summary>
        /// 交互业务结果。
        /// </summary>
        public InteractResultCode ResultCode { get; }

        /// <summary>
        /// 可直接展示的结果说明。
        /// </summary>
        public string Message { get; }
    }
}
