using Train.GameFlow.Core;

namespace Train.GameFlow.Application.Events
{
    /// <summary>
    /// 表示关卡流程状态机切换到了新的阶段。
    /// </summary>
    public readonly struct LevelPhaseChangedEvent
    {
        public LevelPhaseChangedEvent(
            string levelId,
            LevelPhase previous,
            LevelPhase current,
            LevelOutcome outcome)
        {
            LevelId = levelId;
            Previous = previous;
            Current = current;
            Outcome = outcome;
        }

        public string LevelId { get; }
        public LevelPhase Previous { get; }
        public LevelPhase Current { get; }
        public LevelOutcome Outcome { get; }
    }
}
