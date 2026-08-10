namespace Train.Presentation.UI.ViewModels
{
    /// <summary>
    /// 表示 HUD 中央提示横幅的语义类型。
    /// </summary>
    public enum HudBannerKind
    {
        /// <summary>不显示提示横幅。</summary>
        None = 0,

        /// <summary>关卡阶段变化提示。</summary>
        Phase = 1,

        /// <summary>关卡通关提示。</summary>
        Cleared = 2,

        /// <summary>关卡失败提示。</summary>
        Failed = 3,

        /// <summary>玩家正在等待复活。</summary>
        Respawning = 4,

        /// <summary>玩家已经复活。</summary>
        Revived = 5
    }
}
