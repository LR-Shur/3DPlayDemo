using System;
using System.Collections.Generic;

namespace Train.Presentation.UI.ViewModels
{
    /// <summary>
    /// 任务页面一次完整渲染所需的不可变数据。
    /// </summary>
    public sealed class QuestScreenViewModel
    {
        /// <summary>
        /// 创建任务页面视图模型。
        /// </summary>
        public QuestScreenViewModel(
            IReadOnlyList<QuestEntryViewModel> entries,
            string selectedQuestId,
            string title,
            string description,
            string objectivesText,
            string rewardsText,
            string statusText,
            bool canTrack,
            bool canClaim,
            string feedbackText)
        {
            Entries = entries ?? throw new ArgumentNullException(nameof(entries));
            SelectedQuestId = selectedQuestId ?? string.Empty;
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            ObjectivesText = objectivesText ?? string.Empty;
            RewardsText = rewardsText ?? string.Empty;
            StatusText = statusText ?? string.Empty;
            CanTrack = canTrack;
            CanClaim = canClaim;
            FeedbackText = feedbackText ?? string.Empty;
        }

        /// <summary>获取全部任务条目。</summary>
        public IReadOnlyList<QuestEntryViewModel> Entries { get; }

        /// <summary>获取当前选中的任务标识。</summary>
        public string SelectedQuestId { get; }

        /// <summary>获取详情标题。</summary>
        public string Title { get; }

        /// <summary>获取详情说明。</summary>
        public string Description { get; }

        /// <summary>获取目标进度文本。</summary>
        public string ObjectivesText { get; }

        /// <summary>获取奖励文本。</summary>
        public string RewardsText { get; }

        /// <summary>获取状态文本。</summary>
        public string StatusText { get; }

        /// <summary>获取是否允许设置追踪。</summary>
        public bool CanTrack { get; }

        /// <summary>获取是否允许领取奖励。</summary>
        public bool CanClaim { get; }

        /// <summary>获取操作反馈文本。</summary>
        public string FeedbackText { get; }
    }
}
