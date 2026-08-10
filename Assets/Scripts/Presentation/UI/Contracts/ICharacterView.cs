using System;
using Train.Presentation.UI.ViewModels;

namespace Train.Presentation.UI.Contracts
{
    /// <summary>
    /// 角色页面的被动视图接口。
    /// View 只上报角色选择、切换、解锁和关闭意图。
    /// </summary>
    public interface ICharacterView
    {
        /// <summary>玩家选择一张角色卡片时触发。</summary>
        event Action<int> EntrySelected;

        /// <summary>玩家请求切换当前角色时触发。</summary>
        event Action SelectRequested;

        /// <summary>玩家请求解锁当前角色时触发。</summary>
        event Action UnlockRequested;

        /// <summary>玩家请求关闭主菜单时触发。</summary>
        event Action CloseRequested;

        /// <summary>渲染角色页面完整视图模型。</summary>
        void Render(CharacterScreenViewModel viewModel);
    }
}
