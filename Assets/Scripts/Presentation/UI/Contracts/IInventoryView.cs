using System;
using Train.Presentation.UI.ViewModels;

namespace Train.Presentation.UI.Contracts
{
    /// <summary>
    /// 被动式背包视图，只渲染不可变数据并上报用户意图。
    /// 选择状态和应用逻辑统一由 Presenter 负责。
    /// </summary>
    public interface IInventoryView
    {
        /// <summary>当玩家选择一个背包槽位时触发。</summary>
        event Action<int> SlotSelected;

        /// <summary>当玩家切换左侧物品分类时触发，0 表示全部。</summary>
        event Action<int> CategorySelected;

        /// <summary>请求丢弃当前选中物品。</summary>
        event Action DiscardRequested;

        /// <summary>请求把当前选中的消耗品装备到主动道具快捷槽。</summary>
        /// <summary>当玩家请求关闭背包界面时触发。</summary>
        event Action CloseRequested;

        /// <summary>将完整背包视图模型渲染到界面。</summary>
        void Render(InventoryScreenViewModel viewModel);
    }
}
