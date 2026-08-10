namespace Train.Characters.Events
{
    /// <summary>
    /// 表示当前角色选择已改变；该消息不等同于玩家控制器已经完成切换。
    /// </summary>
    public readonly struct CharacterSelectedEvent
    {
        /// <summary>创建角色选择变更消息。</summary>
        public CharacterSelectedEvent(
            string previousCharacterId,
            string currentCharacterId,
            long revision)
        {
            PreviousCharacterId = previousCharacterId;
            CurrentCharacterId = currentCharacterId;
            Revision = revision;
        }

        /// <summary>获取变更前的角色标识。</summary>
        public string PreviousCharacterId { get; }

        /// <summary>获取变更后的角色标识。</summary>
        public string CurrentCharacterId { get; }

        /// <summary>获取选择后的名册修订号。</summary>
        public long Revision { get; }
    }
}
