namespace Train.Presentation.UI.ViewModels
{
    /// <summary>
    /// 角色列表中一名角色的不可变展示数据。
    /// </summary>
    public sealed class CharacterEntryViewModel
    {
        /// <summary>
        /// 创建一名角色卡片数据。
        /// </summary>
        public CharacterEntryViewModel(
            string characterId,
            string displayName,
            string combatRole,
            bool isUnlocked,
            bool isSelected)
        {
            CharacterId = characterId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            CombatRole = combatRole ?? string.Empty;
            IsUnlocked = isUnlocked;
            IsSelected = isSelected;
        }

        /// <summary>获取角色稳定标识。</summary>
        public string CharacterId { get; }

        /// <summary>获取角色展示名。</summary>
        public string DisplayName { get; }

        /// <summary>获取战斗定位。</summary>
        public string CombatRole { get; }

        /// <summary>获取是否已解锁。</summary>
        public bool IsUnlocked { get; }

        /// <summary>获取是否为当前选择。</summary>
        public bool IsSelected { get; }
    }
}
