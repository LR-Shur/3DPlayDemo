using Train.Gameplay.Common.StateMachine;

namespace Train.GameFlow.Core.States
{
    /// <summary>为关卡流程状态提供共享上下文和默认退出行为。</summary>
    internal abstract class LevelFlowState
        : AsyncStateBase<LevelFlowContext>, ILevelFlowState
    {
        /// <summary>使用关卡流程上下文初始化状态。</summary>
        protected LevelFlowState(LevelFlowContext context)
            : base(context)
        {
        }

        /// <summary>获取派生状态对应的关卡阶段。</summary>
        public abstract LevelPhase Phase { get; }
    }
}
