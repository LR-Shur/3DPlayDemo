namespace Train.Gameplay.Common.StateMachine
{
    /// <summary>描述异步状态切换是否真正发生。</summary>
    public enum AsyncStateTransitionResult
    {
        /// <summary>成功进入了目标状态。</summary>
        Completed = 0,

        /// <summary>目标就是当前状态，因此没有重复执行副作用。</summary>
        IgnoredSameState = 1
    }
}
