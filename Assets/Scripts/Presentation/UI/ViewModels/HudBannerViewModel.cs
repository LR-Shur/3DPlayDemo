namespace Train.Presentation.UI.ViewModels
{
    /// <summary>
    /// 提供 HUD 中央提示横幅所需的不可变展示数据。
    /// </summary>
    public sealed class HudBannerViewModel
    {
        /// <summary>获取一个表示不显示横幅的共享视图模型。</summary>
        public static readonly HudBannerViewModel None =
            new(HudBannerKind.None, string.Empty, string.Empty);

        /// <summary>创建一份 HUD 提示横幅视图模型。</summary>
        public HudBannerViewModel(
            HudBannerKind kind,
            string title,
            string subtitle)
        {
            Kind = kind;
            Title = title ?? string.Empty;
            Subtitle = subtitle ?? string.Empty;
        }

        /// <summary>获取横幅的语义类型。</summary>
        public HudBannerKind Kind { get; }

        /// <summary>获取横幅主标题。</summary>
        public string Title { get; }

        /// <summary>获取横幅副标题。</summary>
        public string Subtitle { get; }

        /// <summary>获取横幅当前是否应当显示。</summary>
        public bool IsVisible => Kind != HudBannerKind.None;
    }
}
