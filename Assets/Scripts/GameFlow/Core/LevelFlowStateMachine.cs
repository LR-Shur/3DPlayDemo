using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Train.GameFlow.Core.States;

namespace Train.GameFlow.Core
{
    /// <summary>
    /// 串行执行单次关卡会话中的所有阶段变化。
    /// 相同阶段的重复请求会被忽略，其余不允许的跳转会抛出
    /// <see cref="InvalidLevelTransitionException"/>。
    /// </summary>
    public sealed class LevelFlowStateMachine
    {
        private readonly ILevelFlowActions _actions;
        private readonly ILevelTransitionPolicy _transitionPolicy;
        private readonly IReadOnlyDictionary<LevelPhase, ILevelFlowState> _states;
        private readonly SemaphoreSlim _transitionGate = new SemaphoreSlim(1, 1);

        private LevelPhase _currentPhase;
        private LevelOutcome _outcome;
        private bool _isFaulted;

        /// <summary>
        /// 创建关卡流程状态机。
        /// </summary>
        /// <param name="actions">执行场景具体工作的流程端口。</param>
        /// <param name="transitionPolicy">阶段转换策略；为空时使用默认策略。</param>
        public LevelFlowStateMachine(
            ILevelFlowActions actions,
            ILevelTransitionPolicy transitionPolicy = null)
        {
            _actions = actions ?? throw new ArgumentNullException(nameof(actions));
            _transitionPolicy =
                transitionPolicy ?? DefaultLevelTransitionPolicy.Instance;
            _states = CreateStates();
        }

        /// <summary>
        /// 在一个阶段成功进入后触发。
        /// </summary>
        public event Action<LevelPhaseChanged> PhaseChanged;

        /// <summary>获取当前关卡阶段。</summary>
        public LevelPhase CurrentPhase => _currentPhase;

        /// <summary>获取当前关卡结果。</summary>
        public LevelOutcome Outcome => _outcome;

        /// <summary>获取状态机是否因恢复失败而进入故障状态。</summary>
        public bool IsFaulted => _isFaulted;

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
        /// 按转换策略切换到指定阶段，并串行等待已有转换完成。
        /// </summary>
        /// <param name="targetPhase">目标关卡阶段。</param>
        /// <param name="cancellationToken">用于取消等待或阶段动作的令牌。</param>
        /// <returns>转换完成或同阶段忽略结果。</returns>
        public async Task<LevelTransitionResult> TransitionAsync(
            LevelPhase targetPhase,
            CancellationToken cancellationToken = default)
        {
            await _transitionGate.WaitAsync(cancellationToken);
            try
            {
                ThrowIfFaulted();

                var previousPhase = _currentPhase;
                if (previousPhase == targetPhase)
                {
                    return LevelTransitionResult.IgnoredSamePhase;
                }

                if (!_transitionPolicy.CanTransition(previousPhase, targetPhase))
                {
                    throw new InvalidLevelTransitionException(
                        previousPhase,
                        targetPhase);
                }

                var previousState = GetState(previousPhase);
                var targetState = GetState(targetPhase);

                try
                {
                    if (previousState != null)
                    {
                        await previousState.ExitAsync(
                            _actions,
                            cancellationToken);
                    }

                    await targetState.EnterAsync(
                        _actions,
                        cancellationToken);
                }
                catch (Exception transitionException)
                {
                    await RestorePreviousStateAsync(
                        previousState,
                        previousPhase,
                        transitionException);
                    throw;
                }

                _currentPhase = targetPhase;
                UpdateOutcome(targetPhase);

                PhaseChanged?.Invoke(
                    new LevelPhaseChanged(
                        previousPhase,
                        targetPhase,
                        _outcome));

                return LevelTransitionResult.Completed;
            }
            finally
            {
                _transitionGate.Release();
            }
        }

        private async Task RestorePreviousStateAsync(
            ILevelFlowState previousState,
            LevelPhase previousPhase,
            Exception transitionException)
        {
            if (previousState == null)
            {
                return;
            }

            try
            {
                // 恢复过程有意忽略调用方已取消的令牌，
                // 目的是尽可能把运行时恢复到转换前的稳定状态。
                await previousState.EnterAsync(
                    _actions,
                    CancellationToken.None);
            }
            catch (Exception recoveryException)
            {
                _isFaulted = true;
                throw new LevelFlowRecoveryException(
                    previousPhase,
                    transitionException,
                    recoveryException);
            }
        }

        private ILevelFlowState GetState(LevelPhase phase)
        {
            if (phase == LevelPhase.None)
            {
                return null;
            }

            if (_states.TryGetValue(phase, out var state))
            {
                return state;
            }

            throw new ArgumentOutOfRangeException(
                nameof(phase),
                phase,
                "No state implementation is registered for this phase.");
        }

        private void UpdateOutcome(LevelPhase phase)
        {
            switch (phase)
            {
                case LevelPhase.Cleared:
                    _outcome = LevelOutcome.Cleared;
                    break;

                case LevelPhase.Failed:
                    _outcome = LevelOutcome.Failed;
                    break;
            }
        }

        private void ThrowIfFaulted()
        {
            if (_isFaulted)
            {
                throw new InvalidOperationException(
                    "The level flow is faulted because state recovery failed.");
            }
        }

        private static IReadOnlyDictionary<LevelPhase, ILevelFlowState>
            CreateStates()
        {
            return new Dictionary<LevelPhase, ILevelFlowState>
            {
                {
                    LevelPhase.Preparing,
                    new PreparingLevelState()
                },
                {
                    LevelPhase.Intro,
                    new IntroLevelState()
                },
                {
                    LevelPhase.Combat,
                    new CombatLevelState()
                },
                {
                    LevelPhase.Cleared,
                    new ResultLevelState(
                        LevelPhase.Cleared,
                        LevelOutcome.Cleared)
                },
                {
                    LevelPhase.Failed,
                    new ResultLevelState(
                        LevelPhase.Failed,
                        LevelOutcome.Failed)
                },
                {
                    LevelPhase.Exiting,
                    new ExitingLevelState()
                }
            };
        }
    }
}
