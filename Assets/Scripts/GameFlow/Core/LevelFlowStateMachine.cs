using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Train.GameFlow.Core.States;
using Train.Gameplay.Common.StateMachine;

namespace Train.GameFlow.Core
{
    /// <summary>
    /// 关卡流程状态机的领域外观。
    /// 具体的串行切换、重复请求、异常恢复由 Gameplay.Common 的通用状态机负责，
    /// 本类只保留关卡阶段映射、转换策略和领域事件。
    /// </summary>
    public sealed class LevelFlowStateMachine
        : StateMachineBase<LevelFlowContext, ILevelFlowState>
    {
        private readonly ILevelTransitionPolicy _transitionPolicy;
        private readonly IReadOnlyDictionary<LevelPhase, ILevelFlowState> _states;
        private readonly IReadOnlyDictionary<LevelPhase, LevelOutcome> _outcomes =
            new Dictionary<LevelPhase, LevelOutcome>
            {
                { LevelPhase.Cleared, LevelOutcome.Cleared },
                { LevelPhase.Failed, LevelOutcome.Failed }
            };

        /// <summary>
        /// 创建关卡流程状态机。
        /// </summary>
        /// <param name="actions">执行场景具体工作的流程端口。</param>
        /// <param name="transitionPolicy">阶段转换策略；为空时使用默认策略。</param>
        public LevelFlowStateMachine(
            ILevelFlowActions actions,
            ILevelTransitionPolicy transitionPolicy = null)
            : base(new LevelFlowContext(
                actions ?? throw new ArgumentNullException(nameof(actions))))
        {
            _transitionPolicy =
                transitionPolicy ?? DefaultLevelTransitionPolicy.Instance;
            _states = CreateStates(Context);
        }

        /// <summary>在一个阶段成功进入后触发。</summary>
        public event Action<LevelPhaseChanged> PhaseChanged;

        /// <summary>获取当前关卡阶段。</summary>
        public LevelPhase CurrentPhase =>
            CurrentState?.Phase ?? LevelPhase.None;

        /// <summary>获取当前关卡结果。</summary>
        public LevelOutcome Outcome { get; private set; }

        /// <summary>从未开始状态进入准备阶段。</summary>
        public Task<LevelTransitionResult> StartAsync(
            CancellationToken cancellationToken = default)
        {
            return TransitionAsync(LevelPhase.Preparing, cancellationToken);
        }

        /// <summary>从准备阶段进入开场阶段。</summary>
        public Task<LevelTransitionResult> ShowIntroAsync(
            CancellationToken cancellationToken = default)
        {
            return TransitionAsync(LevelPhase.Intro, cancellationToken);
        }

        /// <summary>从开场阶段进入战斗阶段。</summary>
        public Task<LevelTransitionResult> BeginCombatAsync(
            CancellationToken cancellationToken = default)
        {
            return TransitionAsync(LevelPhase.Combat, cancellationToken);
        }

        /// <summary>将正在战斗的关卡标记为通关。</summary>
        public Task<LevelTransitionResult> CompleteAsync(
            CancellationToken cancellationToken = default)
        {
            return TransitionAsync(LevelPhase.Cleared, cancellationToken);
        }

        /// <summary>将正在战斗的关卡标记为失败。</summary>
        public Task<LevelTransitionResult> FailAsync(
            CancellationToken cancellationToken = default)
        {
            return TransitionAsync(LevelPhase.Failed, cancellationToken);
        }

        /// <summary>从关卡结果阶段进入退出阶段。</summary>
        public Task<LevelTransitionResult> ExitAsync(
            CancellationToken cancellationToken = default)
        {
            return TransitionAsync(LevelPhase.Exiting, cancellationToken);
        }

        /// <summary>
        /// 按转换策略切换到指定阶段；通用基类负责锁、进入、退出和恢复。
        /// </summary>
        public async Task<LevelTransitionResult> TransitionAsync(
            LevelPhase targetPhase,
            CancellationToken cancellationToken = default)
        {
            var targetState = ResolveState(targetPhase);
            var result = await base.TransitionAsync(
                targetState,
                CanTransition,
                cancellationToken);

            return result == AsyncStateTransitionResult.IgnoredSameState
                ? LevelTransitionResult.IgnoredSamePhase
                : LevelTransitionResult.Completed;
        }

        /// <inheritdoc />
        protected override Exception CreateInvalidTransitionException(
            ILevelFlowState previousState,
            ILevelFlowState targetState)
        {
            return new InvalidLevelTransitionException(
                previousState?.Phase ?? LevelPhase.None,
                targetState.Phase);
        }

        /// <inheritdoc />
        protected override Exception CreateRecoveryException(
            ILevelFlowState previousState,
            Exception transitionException,
            Exception recoveryException)
        {
            return new LevelFlowRecoveryException(
                previousState.Phase,
                transitionException,
                recoveryException);
        }

        /// <inheritdoc />
        protected override void OnStateChanged(
            ILevelFlowState previousState,
            ILevelFlowState currentState)
        {
            if (_outcomes.TryGetValue(currentState.Phase, out var outcome))
            {
                Outcome = outcome;
            }

            PhaseChanged?.Invoke(
                new LevelPhaseChanged(
                    previousState?.Phase ?? LevelPhase.None,
                    currentState.Phase,
                    Outcome));
        }

        private bool CanTransition(
            ILevelFlowState previousState,
            ILevelFlowState targetState)
        {
            return _transitionPolicy.CanTransition(
                previousState?.Phase ?? LevelPhase.None,
                targetState.Phase);
        }

        private ILevelFlowState ResolveState(LevelPhase phase)
        {
            if (_states.TryGetValue(phase, out var state))
            {
                return state;
            }

            throw new ArgumentOutOfRangeException(
                nameof(phase),
                phase,
                "No state implementation is registered for this phase.");
        }

        private static IReadOnlyDictionary<LevelPhase, ILevelFlowState>
            CreateStates(LevelFlowContext context)
        {
            return new Dictionary<LevelPhase, ILevelFlowState>
            {
                { LevelPhase.Preparing, new PreparingLevelState(context) },
                { LevelPhase.Intro, new IntroLevelState(context) },
                { LevelPhase.Combat, new CombatLevelState(context) },
                {
                    LevelPhase.Cleared,
                    new ResultLevelState(
                        context,
                        LevelPhase.Cleared,
                        LevelOutcome.Cleared)
                },
                {
                    LevelPhase.Failed,
                    new ResultLevelState(
                        context,
                        LevelPhase.Failed,
                        LevelOutcome.Failed)
                },
                { LevelPhase.Exiting, new ExitingLevelState(context) }
            };
        }
    }
}
