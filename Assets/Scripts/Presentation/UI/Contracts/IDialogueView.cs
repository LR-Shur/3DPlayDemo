using System;
using Train.Presentation.UI.ViewModels;

namespace Train.Presentation.UI.Contracts
{
    /// <summary>
    /// 对话浮层的被动视图接口。
    /// View 只上报继续、选项和关闭意图。
    /// </summary>
    public interface IDialogueView
    {
        /// <summary>玩家请求继续下一句台词时触发。</summary>
        event Action ContinueRequested;

        /// <summary>玩家选择一个选项时触发。</summary>
        event Action<int> ChoiceRequested;

        /// <summary>玩家请求取消对话时触发。</summary>
        event Action CloseRequested;

        /// <summary>渲染对话浮层完整视图模型。</summary>
        void Render(DialogueScreenViewModel viewModel);
    }
}
