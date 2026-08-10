using Train.Inventory.Data;

namespace Train.Presentation.UI.ViewModels
{
    /// <summary>
    /// 交给 HUD 展示的不可变世界交互提示数据。
    /// </summary>
    public sealed class InteractionPromptViewModel
    {
        /// <summary>
        /// 创建交互提示视图模型。
        /// </summary>
        public InteractionPromptViewModel(
            bool isVisible,
            string displayName,
            int quantity,
            ItemRarity rarity,
            string actionLabel = "拾取")
        {
            IsVisible = isVisible;
            DisplayName = displayName ?? string.Empty;
            Quantity = quantity;
            Rarity = rarity;
            ActionLabel = string.IsNullOrWhiteSpace(actionLabel)
                ? "交互"
                : actionLabel;
        }

        /// <summary>是否显示提示。</summary>
        public bool IsVisible { get; }

        /// <summary>焦点物品名称。</summary>
        public string DisplayName { get; }

        /// <summary>可拾取数量。</summary>
        public int Quantity { get; }

        /// <summary>物品品质。</summary>
        public ItemRarity Rarity { get; }

        /// <summary>当前交互动作标签。</summary>
        public string ActionLabel { get; }

        /// <summary>获取隐藏状态的共享实例。</summary>
        public static InteractionPromptViewModel Hidden { get; } =
            new(false, string.Empty, 0, ItemRarity.Common, string.Empty);
    }
}
