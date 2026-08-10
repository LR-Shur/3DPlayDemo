using System;
using System.Collections.Generic;

namespace Train.Presentation.UI.ViewModels
{
    /// <summary>
    /// 角色页面一次完整渲染所需的不可变数据。
    /// </summary>
    public sealed class CharacterScreenViewModel
    {
        /// <summary>
        /// 创建角色页面视图模型。
        /// </summary>
        public CharacterScreenViewModel(
            IReadOnlyList<CharacterEntryViewModel> entries,
            string selectedCharacterId,
            string displayName,
            string description,
            string faction,
            string combatRole,
            bool isUnlocked,
            bool isSelected,
            bool canUnlock,
            string feedbackText)
        {
            Entries = entries ?? throw new ArgumentNullException(nameof(entries));
            SelectedCharacterId = selectedCharacterId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Description = description ?? string.Empty;
            Faction = faction ?? string.Empty;
            CombatRole = combatRole ?? string.Empty;
            IsUnlocked = isUnlocked;
            IsSelected = isSelected;
            CanUnlock = canUnlock;
            FeedbackText = feedbackText ?? string.Empty;
        }

        /// <summary>获取全部角色条目。</summary>
        public IReadOnlyList<CharacterEntryViewModel> Entries { get; }

        /// <summary>获取当前选中的角色标识。</summary>
        public string SelectedCharacterId { get; }

        /// <summary>获取详情角色名。</summary>
        public string DisplayName { get; }

        /// <summary>获取详情简介。</summary>
        public string Description { get; }

        /// <summary>获取阵营。</summary>
        public string Faction { get; }

        /// <summary>获取战斗定位。</summary>
        public string CombatRole { get; }

        /// <summary>获取角色是否已解锁。</summary>
        public bool IsUnlocked { get; }

        /// <summary>获取角色是否当前选择。</summary>
        public bool IsSelected { get; }

        /// <summary>获取是否允许解锁。</summary>
        public bool CanUnlock { get; }

        /// <summary>获取操作反馈文本。</summary>
        public string FeedbackText { get; }
    }
}
