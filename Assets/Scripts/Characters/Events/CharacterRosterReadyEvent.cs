namespace Train.Characters.Events
{
    /// <summary>
    /// 表示角色名册已完成初始化，可供其他模块读取。
    /// </summary>
    public readonly struct CharacterRosterReadyEvent
    {
        /// <summary>创建角色名册就绪消息。</summary>
        public CharacterRosterReadyEvent(
            int catalogCount,
            int unlockedCount,
            string selectedCharacterId,
            long revision)
        {
            CatalogCount = catalogCount;
            UnlockedCount = unlockedCount;
            SelectedCharacterId = selectedCharacterId;
            Revision = revision;
        }

        /// <summary>获取已登记角色总数。</summary>
        public int CatalogCount { get; }

        /// <summary>获取已解锁角色数量。</summary>
        public int UnlockedCount { get; }

        /// <summary>获取当前选择的角色标识。</summary>
        public string SelectedCharacterId { get; }

        /// <summary>获取名册修订号。</summary>
        public long Revision { get; }
    }
}
