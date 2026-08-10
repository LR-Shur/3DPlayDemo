using System;
using Train.Presentation.UI.ViewModels;

namespace Train.Presentation.UI.Contracts
{
    /// <summary>
    /// 任务页面的被动视图接口。
    /// View 只上报列表选择、追踪、领取和关闭意图。
    /// </summary>
    public interface IQuestView
    {
        /// <summary>玩家选择一行任务时触发。</summary>
        event Action<int> EntrySelected;

        /// <summary>玩家请求追踪当前任务时触发。</summary>
        event Action TrackRequested;

        /// <summary>玩家请求领取当前任务奖励时触发。</summary>
        event Action ClaimRequested;

        /// <summary>玩家请求关闭主菜单时触发。</summary>
        event Action CloseRequested;

        /// <summary>渲染任务页面完整视图模型。</summary>
        void Render(QuestScreenViewModel viewModel);
    }
}
