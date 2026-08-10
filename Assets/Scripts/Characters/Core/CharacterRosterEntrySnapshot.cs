namespace Train.Characters.Core
{
    /// <summary>
    /// 表示某一修订版本中单名角色的只读状态。
    /// </summary>
    public readonly struct CharacterRosterEntrySnapshot
    {
        /// <summary>创建一条不可变角色状态。</summary>
        public CharacterRosterEntrySnapshot(
            string characterId,
            bool isUnlocked,
            bool isSelected)
        {
            CharacterId = characterId;
            IsUnlocked = isUnlocked;
            IsSelected = isSelected;
        }

        /// <summary>获取角色稳定标识。</summary>
        public string CharacterId { get; }

        /// <summary>获取角色是否已解锁。</summary>
        public bool IsUnlocked { get; }

        /// <summary>获取角色是否为当前选择。</summary>
        public bool IsSelected { get; }
    }
}
