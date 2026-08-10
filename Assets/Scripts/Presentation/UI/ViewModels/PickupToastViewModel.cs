using Train.Inventory.Data;

namespace Train.Presentation.UI.ViewModels
{
    /// <summary>
    /// HUD 拾取反馈条使用的不可变展示数据。
    /// </summary>
    public sealed class PickupToastViewModel
    {
        /// <summary>
        /// 创建一次拾取反馈视图模型。
        /// </summary>
        public PickupToastViewModel(
            bool isSuccess,
            string title,
            string message,
            ItemRarity rarity)
        {
            IsSuccess = isSuccess;
            Title = title ?? string.Empty;
            Message = message ?? string.Empty;
            Rarity = rarity;
        }

        /// <summary>拾取是否成功。</summary>
        public bool IsSuccess { get; }

        /// <summary>反馈标题。</summary>
        public string Title { get; }

        /// <summary>反馈说明。</summary>
        public string Message { get; }

        /// <summary>物品品质，用于成功反馈的强调色。</summary>
        public ItemRarity Rarity { get; }
    }
}
