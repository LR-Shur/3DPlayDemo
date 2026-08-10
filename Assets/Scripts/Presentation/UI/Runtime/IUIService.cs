namespace Train.Presentation.UI.Runtime
{
    /// <summary>
    /// 提供给玩法层和快捷键驱动调用的顶层 UI 页面控制接口。
    /// </summary>
    public interface IUIService
    {
        /// <summary>获取主菜单当前是否打开。</summary>
        bool IsMenuOpen { get; }

        /// <summary>获取主菜单当前页面。</summary>
        GameMenuPage CurrentPage { get; }

        /// <summary>获取背包页面当前是否打开。</summary>
        bool IsInventoryOpen { get; }

        /// <summary>获取装备页面当前是否打开。</summary>
        bool IsEquipmentOpen { get; }

        /// <summary>打开指定主菜单页面。</summary>
        void ShowPage(GameMenuPage page);

        /// <summary>关闭整个主菜单并恢复玩法输入。</summary>
        void HideMenu();

        /// <summary>打开背包页面并阻塞游戏输入。</summary>
        void ShowInventory();

        /// <summary>关闭背包页面并在适当时恢复游戏输入。</summary>
        void HideInventory();

        /// <summary>在打开和关闭背包页面之间切换。</summary>
        void ToggleInventory();

        /// <summary>打开装备页面并阻塞玩法输入。</summary>
        void ShowEquipment();

        /// <summary>在打开和关闭装备页面之间切换。</summary>
        void ToggleEquipment();
    }
}
