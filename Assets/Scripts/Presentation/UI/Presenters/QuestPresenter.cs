using System;
using System.Collections.Generic;
using System.Text;
using Train.Architecture.Events;
using Train.Presentation.UI.Contracts;
using Train.Presentation.UI.ViewModels;
using Train.Quest.Application;
using Train.Quest.Core;
using Train.Quest.Data;
using Train.Quest.Events;

namespace Train.Presentation.UI.Presenters
{
    /// <summary>
    /// 连接任务 Server 与任务页面，负责列表选择、追踪和奖励领取。
    /// 领域进度和奖励发放仍由 QuestService 权威处理。
    /// </summary>
    public sealed class QuestPresenter : IDisposable
    {
        private readonly IQuestService _quests;
        private readonly IQuestView _view;
        private readonly Action _closeRequested;
        private readonly List<IDisposable> _subscriptions = new();
        private string _selectedQuestId;
        private string _feedbackText = string.Empty;
        private bool _disposed;

        /// <summary>
        /// 创建任务 Presenter、订阅任务事件并立即渲染初始状态。
        /// </summary>
        public QuestPresenter(
            IQuestService quests,
            IEventBus events,
            IQuestView view,
            Action closeRequested)
        {
            _quests = quests ?? throw new ArgumentNullException(nameof(quests));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _closeRequested = closeRequested ??
                throw new ArgumentNullException(nameof(closeRequested));
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            _view.EntrySelected += OnEntrySelected;
            _view.TrackRequested += OnTrackRequested;
            _view.ClaimRequested += OnClaimRequested;
            _view.CloseRequested += OnCloseRequested;
            _subscriptions.Add(
                events.Subscribe<QuestChangedEvent>(OnQuestChanged));
            _subscriptions.Add(
                events.Subscribe<TrackedQuestChangedEvent>(
                    OnTrackedChanged));
            _subscriptions.Add(
                events.Subscribe<QuestRewardClaimFailedEvent>(
                    OnClaimFailed));
            _subscriptions.Add(
                events.Subscribe<QuestRewardsClaimedEvent>(
                    OnRewardsClaimed));

            SelectInitialQuest();
            Render();
        }

        /// <summary>断开视图与事件订阅。</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _view.EntrySelected -= OnEntrySelected;
            _view.TrackRequested -= OnTrackRequested;
            _view.ClaimRequested -= OnClaimRequested;
            _view.CloseRequested -= OnCloseRequested;
            for (var i = _subscriptions.Count - 1; i >= 0; i--)
            {
                _subscriptions[i].Dispose();
            }

            _subscriptions.Clear();
            _disposed = true;
        }

        /// <summary>选择任务列表中的第一项。</summary>
        private void SelectInitialQuest()
        {
            _selectedQuestId = null;
            var snapshots = _quests.Snapshots;
            if (snapshots.Count > 0)
            {
                _selectedQuestId = snapshots[0].QuestId;
            }
        }

        private void OnEntrySelected(int index)
        {
            var snapshots = _quests.Snapshots;
            if (index < 0 || index >= snapshots.Count)
            {
                return;
            }

            _selectedQuestId = snapshots[index].QuestId;
            _feedbackText = string.Empty;
            Render();
        }

        private void OnTrackRequested()
        {
            if (string.IsNullOrWhiteSpace(_selectedQuestId))
            {
                return;
            }

            var tracked = _quests.TrackQuest(_selectedQuestId);
            _feedbackText = tracked
                ? "已设为当前追踪任务。"
                : "该任务当前无法追踪。";
            Render();
        }

        private void OnClaimRequested()
        {
            if (string.IsNullOrWhiteSpace(_selectedQuestId))
            {
                return;
            }

            var result = _quests.ClaimRewards(_selectedQuestId);
            _feedbackText = result.Succeeded
                ? "奖励已发放到仓库。"
                : "领取失败，请检查背包空间后重试。";
            Render();
        }

        private void OnCloseRequested()
        {
            _closeRequested();
        }

        private void OnQuestChanged(QuestChangedEvent message)
        {
            Render();
        }

        private void OnTrackedChanged(TrackedQuestChangedEvent message)
        {
            Render();
        }

        private void OnClaimFailed(QuestRewardClaimFailedEvent message)
        {
            _feedbackText = "奖励领取失败：背包空间不足或物品未登记。";
            Render();
        }

        private void OnRewardsClaimed(QuestRewardsClaimedEvent message)
        {
            _feedbackText = "奖励已发放到仓库。";
            Render();
        }

        /// <summary>从权威快照构建任务页面视图模型。</summary>
        private void Render()
        {
            if (_disposed)
            {
                return;
            }

            var snapshots = _quests.Snapshots;
            var entries = new List<QuestEntryViewModel>(snapshots.Count);
            foreach (var snapshot in snapshots)
            {
                entries.Add(
                    new QuestEntryViewModel(
                        snapshot.QuestId,
                        snapshot.Title,
                        FormatStatus(snapshot.Status),
                        FormatProgress(snapshot),
                        string.Equals(
                            snapshot.QuestId,
                            _quests.TrackedQuestId,
                            StringComparison.Ordinal),
                        string.Equals(
                            snapshot.QuestId,
                            _selectedQuestId,
                            StringComparison.Ordinal)));
            }

            var selected = FindSelectedSnapshot(snapshots);
            var selectedDefinition = selected != null
                ? FindSelectedDefinition(selected.QuestId)
                : null;
            _view.Render(
                new QuestScreenViewModel(
                    entries,
                    _selectedQuestId ?? string.Empty,
                    selected != null ? selected.Title : "未选择任务",
                    selectedDefinition != null
                        ? selectedDefinition.Description
                        : string.Empty,
                    selected != null
                        ? FormatObjectives(selectedDefinition, selected)
                        : string.Empty,
                    selectedDefinition != null
                        ? FormatRewards(selectedDefinition)
                        : string.Empty,
                    selected != null
                        ? FormatStatus(selected.Status)
                        : string.Empty,
                    selected != null &&
                    selected.Status != QuestStatus.Claimed &&
                    selected.Status != QuestStatus.Inactive,
                    selected != null && selected.Status == QuestStatus.Completed,
                    _feedbackText));
        }

        private QuestProgressSnapshot FindSelectedSnapshot(
            IReadOnlyList<QuestProgressSnapshot> snapshots)
        {
            if (string.IsNullOrWhiteSpace(_selectedQuestId))
            {
                return null;
            }

            for (var i = 0; i < snapshots.Count; i++)
            {
                if (string.Equals(
                        snapshots[i].QuestId,
                        _selectedQuestId,
                        StringComparison.Ordinal))
                {
                    return snapshots[i];
                }
            }

            return null;
        }

        private QuestDefinition FindSelectedDefinition(string questId)
        {
            return _quests.TryGetDefinition(questId, out var definition)
                ? definition
                : null;
        }

        private static string FormatStatus(QuestStatus status)
        {
            return status switch
            {
                QuestStatus.Active => "进行中",
                QuestStatus.Completed => "已完成",
                QuestStatus.Claimed => "已领取",
                _ => "未接取"
            };
        }

        private static string FormatProgress(QuestProgressSnapshot snapshot)
        {
            var completed = 0;
            for (var i = 0; i < snapshot.Objectives.Count; i++)
            {
                var objective = snapshot.Objectives[i];
                if (objective.CurrentAmount >= objective.RequiredAmount)
                {
                    completed++;
                }
            }

            return $"{completed}/{snapshot.Objectives.Count}";
        }

        private static string FormatObjectives(
            QuestDefinition definition,
            QuestProgressSnapshot snapshot)
        {
            var builder = new StringBuilder();
            foreach (var objective in snapshot.Objectives)
            {
                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append("• ");
                builder.Append(FindObjectiveDisplayText(
                    definition,
                    objective));
                builder.Append("  ");
                builder.Append(objective.CurrentAmount);
                builder.Append('/');
                builder.Append(objective.RequiredAmount);
            }

            return builder.ToString();
        }

        /// <summary>
        /// 优先使用策划配置的目标说明，没有配置时回退到目标标识。
        /// </summary>
        private static string FindObjectiveDisplayText(
            QuestDefinition definition,
            QuestObjectiveProgressSnapshot objective)
        {
            if (definition != null)
            {
                foreach (var candidate in definition.Objectives)
                {
                    if (string.Equals(
                            candidate.ObjectiveId,
                            objective.ObjectiveId,
                            StringComparison.Ordinal) &&
                        !string.IsNullOrWhiteSpace(candidate.DisplayText))
                    {
                        return candidate.DisplayText;
                    }
                }
            }

            return objective.TargetId;
        }

        private static string FormatRewards(QuestDefinition definition)
        {
            if (definition.Rewards.Count == 0)
            {
                return "无奖励";
            }

            var builder = new StringBuilder();
            foreach (var reward in definition.Rewards)
            {
                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append("• ");
                builder.Append(reward.ItemId);
                builder.Append(" ×");
                builder.Append(reward.Amount);
            }

            return builder.ToString();
        }
    }
}
