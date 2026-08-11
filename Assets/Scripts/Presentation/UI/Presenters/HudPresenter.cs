using System;
using System.Collections.Generic;
using Train.Architecture.Events;
using Train.GameFlow.Application;
using Train.GameFlow.Application.Events;
using Train.GameFlow.Core;
using Train.Gameplay.Combat.Application.Events;
using Train.Gameplay.Player.Application.Events;
using Train.Presentation.UI.Contracts;
using Train.Presentation.UI.ViewModels;
using Train.WorldInteraction.Core;
using Train.WorldInteraction.Events;

namespace Train.Presentation.UI.Presenters
{
    /// <summary>
    /// 由事件驱动的 HUD Presenter。
    /// 事件只负责触发刷新或选择提示内容，权威数值始终从关卡只读模型重新获取。
    /// </summary>
    public sealed class HudPresenter : IDisposable
    {
        private readonly ILevelSessionRegistry _levelSessions;
        private readonly IHudView _view;
        private readonly List<IDisposable> _subscriptions = new();
        private bool _disposed;

        /// <summary>
        /// 创建 HUD Presenter、订阅玩法事件，并立即执行首次渲染。
        /// </summary>
        public HudPresenter(
            ILevelSessionRegistry levelSessions,
            IEventBus events,
            IHudView view)
        {
            _levelSessions =
                levelSessions ??
                throw new ArgumentNullException(nameof(levelSessions));
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            _view = view ?? throw new ArgumentNullException(nameof(view));

            _subscriptions.Add(
                events.Subscribe<LevelPhaseChangedEvent>(
                    OnLevelPhaseChanged));
            _subscriptions.Add(
                events.Subscribe<LevelSessionChangedEvent>(
                    OnLevelSessionChanged));
            _subscriptions.Add(
                events.Subscribe<EnemyDefeatedEvent>(
                    OnEnemyDefeated));
            _subscriptions.Add(
                events.Subscribe<LevelCompletedEvent>(
                    OnLevelCompleted));
            _subscriptions.Add(
                events.Subscribe<LevelFailedEvent>(
                    OnLevelFailed));
            _subscriptions.Add(
                events.Subscribe<EntityDamagedEvent>(
                    OnEntityDamaged));
            _subscriptions.Add(
                events.Subscribe<EntityRevivedEvent>(
                    OnEntityRevived));
            _subscriptions.Add(
                events.Subscribe<PlayerRespawnStartedEvent>(
                    OnPlayerRespawnStarted));
            _subscriptions.Add(
                events.Subscribe<PlayerRespawnCompletedEvent>(
                    OnPlayerRespawnCompleted));
            _subscriptions.Add(
                events.Subscribe<InteractionPromptChangedEvent>(
                    OnInteractionPromptChanged));
            _subscriptions.Add(
                events.Subscribe<WorldItemPickupFeedbackEvent>(
                    OnWorldItemPickupFeedback));

            Render(CreatePhaseBanner(ReadSnapshot().Phase));
            _view.RenderInteractionPrompt(
                InteractionPromptViewModel.Hidden);
        }

        /// <summary>
        /// 取消全部事件订阅并停止后续 HUD 渲染。
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            for (var i = _subscriptions.Count - 1; i >= 0; i--)
            {
                _subscriptions[i].Dispose();
            }

            _subscriptions.Clear();
            _disposed = true;
        }

        private void OnLevelPhaseChanged(LevelPhaseChangedEvent message)
        {
            Render(CreatePhaseBanner(message.Current));
        }

        /// <summary>
        /// 关卡刚挂载时立即刷新一次 HUD，确保玩家生命值已经进入数字显示。
        /// </summary>
        private void OnLevelSessionChanged(LevelSessionChangedEvent message)
        {
            Render(message.HasActiveSession
                ? CreatePhaseBanner(ReadSnapshot().Phase)
                : HudBannerViewModel.None);
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent message)
        {
            Render(HudBannerViewModel.None);
        }

        private void OnLevelCompleted(LevelCompletedEvent message)
        {
            Render(
                new HudBannerViewModel(
                    HudBannerKind.Cleared,
                    "作战完成",
                    "区域威胁已清除"));
        }

        private void OnLevelFailed(LevelFailedEvent message)
        {
            Render(
                new HudBannerViewModel(
                    HudBannerKind.Failed,
                    "作战失败",
                    message.Reason));
        }

        private void OnEntityDamaged(EntityDamagedEvent message)
        {
            Render(HudBannerViewModel.None);
        }

        private void OnEntityRevived(EntityRevivedEvent message)
        {
            Render(HudBannerViewModel.None);
        }

        private void OnPlayerRespawnStarted(
            PlayerRespawnStartedEvent message)
        {
            var seconds = Math.Max(0f, message.DelaySeconds);
            Render(
                new HudBannerViewModel(
                    HudBannerKind.Respawning,
                    "代理人重整中",
                    $"{seconds:0.#} 秒后重返战场"));
        }

        private void OnPlayerRespawnCompleted(
            PlayerRespawnCompletedEvent message)
        {
            Render(
                new HudBannerViewModel(
                    HudBannerKind.Revived,
                    "重返战场",
                    "作战继续"));
        }

        /// <summary>
        /// 将世界交互事件转换为 HUD 专用视图模型。
        /// </summary>
        private void OnInteractionPromptChanged(
            InteractionPromptChangedEvent message)
        {
            _view.RenderInteractionPrompt(
                message.IsVisible
                    ? new InteractionPromptViewModel(
                        true,
                        message.DisplayName,
                        message.Quantity,
                        message.Rarity,
                        message.ActionLabel)
                    : InteractionPromptViewModel.Hidden);
        }

        /// <summary>
        /// 将拾取结果转换为短时反馈条。
        /// </summary>
        private void OnWorldItemPickupFeedback(
            WorldItemPickupFeedbackEvent message)
        {
            var succeeded =
                message.ResultCode == InteractResultCode.Success;
            _view.RenderPickupToast(
                new PickupToastViewModel(
                    succeeded,
                    succeeded
                        ? $"获得 {message.DisplayName} ×{message.Quantity}"
                        : "拾取失败",
                    message.Message,
                    message.Rarity));
        }

        private void Render(HudBannerViewModel banner)
        {
            if (_disposed)
            {
                return;
            }

            var snapshot = ReadSnapshot();
            var objective = snapshot.HasLevel
                ? snapshot.Phase == LevelPhase.None
                    ? "靠近训练终端并按 E 完成引导"
                    : $"击败敌人  {snapshot.DefeatedEnemyCount}/{snapshot.EnemyCount}"
                : string.Empty;

            _view.Render(
                new HudViewModel(
                    snapshot.HasLevel,
                    snapshot.LevelId,
                    snapshot.DisplayName,
                    snapshot.Phase,
                    snapshot.Outcome,
                    snapshot.PlayerHealth,
                    snapshot.PlayerMaxHealth,
                    snapshot.DefeatedEnemyCount,
                    snapshot.EnemyCount,
                    snapshot.PlayerDeathCount,
                    objective,
                    banner));
        }

        private LevelReadSnapshot ReadSnapshot()
        {
            var readModel = _levelSessions.Current;
            return readModel != null
                ? readModel.Snapshot
                : default;
        }

        private static HudBannerViewModel CreatePhaseBanner(
            LevelPhase phase)
        {
            switch (phase)
            {
                case LevelPhase.Preparing:
                    return new HudBannerViewModel(
                        HudBannerKind.Phase,
                        "作战准备",
                        "正在部署代理人");

                case LevelPhase.Intro:
                    return new HudBannerViewModel(
                        HudBannerKind.Phase,
                        "目标区域已确认",
                        "准备接敌");

                case LevelPhase.Combat:
                    return new HudBannerViewModel(
                        HudBannerKind.Phase,
                        "战斗开始",
                        "清除区域内全部敌人");

                case LevelPhase.Cleared:
                    return new HudBannerViewModel(
                        HudBannerKind.Cleared,
                        "作战完成",
                        "区域威胁已清除");

                case LevelPhase.Failed:
                    return new HudBannerViewModel(
                        HudBannerKind.Failed,
                        "作战失败",
                        "请重新整备后再次挑战");

                case LevelPhase.Exiting:
                    return new HudBannerViewModel(
                        HudBannerKind.Phase,
                        "作战结束",
                        "正在返回");

                default:
                    return HudBannerViewModel.None;
            }
        }
    }
}
