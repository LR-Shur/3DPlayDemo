namespace Train.Characters.Events
{
    /// <summary>
    /// 表示一名角色刚刚在名册中完成解锁。
    /// </summary>
    public readonly struct CharacterUnlockedEvent
    {
        /// <summary>创建角色解锁消息。</summary>
        public CharacterUnlockedEvent(
            string characterId,
            long revision)
        {
            CharacterId = characterId;
            Revision = revision;
        }

        /// <summary>获取被解锁角色的稳定标识。</summary>
        public string CharacterId { get; }

        /// <summary>获取解锁后的名册修订号。</summary>
        public long Revision { get; }
    }
}
