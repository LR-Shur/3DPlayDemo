using Train.Presentation.UI.ViewModels;

namespace Train.Presentation.UI.Contracts
{
    /// <summary>
    /// 被动式战斗 HUD 视图，仅负责渲染 Presenter 提供的数据。
    /// </summary>
    public interface IHudView
    {
        /// <summary>将完整 HUD 视图模型渲染到界面。</summary>
        void Render(HudViewModel viewModel);

        /// <summary>渲染地图交互焦点提示。</summary>
        void RenderInteractionPrompt(
            InteractionPromptViewModel viewModel);

        /// <summary>渲染一次拾取成功或失败反馈。</summary>
        void RenderPickupToast(PickupToastViewModel viewModel);
    }
}
