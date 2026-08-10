using Train.Gameplay.Common.StateMachine;

namespace Train.GameFlow.Core.States
{
    /// <summary>定义一个可异步进入、可退出的关卡流程状态。</summary>
    public interface ILevelFlowState : IAsyncState<LevelFlowContext>
    {
        /// <summary>获取此状态对应的关卡阶段。</summary>
        LevelPhase Phase { get; }
    }
}
