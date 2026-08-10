using System;

namespace Train.Presentation.UI.ViewModels
{
    /// <summary>
    /// 任务列表中一行任务的不可变展示数据。
    /// </summary>
    public sealed class QuestEntryViewModel
    {
        /// <summary>
        /// 创建一行任务展示数据。
        /// </summary>
        public QuestEntryViewModel(
            string questId,
            string title,
            string statusText,
            string progressText,
            bool isTracked,
            bool isSelected)
        {
            QuestId = questId ?? string.Empty;
            Title = title ?? string.Empty;
            StatusText = statusText ?? string.Empty;
            ProgressText = progressText ?? string.Empty;
            IsTracked = isTracked;
            IsSelected = isSelected;
        }

        /// <summary>获取任务稳定标识。</summary>
        public string QuestId { get; }

        /// <summary>获取任务标题。</summary>
        public string Title { get; }

        /// <summary>获取状态文本。</summary>
        public string StatusText { get; }

        /// <summary>获取目标进度摘要。</summary>
        public string ProgressText { get; }

        /// <summary>获取是否为当前追踪任务。</summary>
        public bool IsTracked { get; }

        /// <summary>获取是否在列表中选中。</summary>
        public bool IsSelected { get; }
    }
}
