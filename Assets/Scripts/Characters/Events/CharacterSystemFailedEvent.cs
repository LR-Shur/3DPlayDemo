namespace Train.Characters.Events
{
    /// <summary>
    /// 表示角色名册系统初始化失败。
    /// </summary>
    public readonly struct CharacterSystemFailedEvent
    {
        /// <summary>创建角色名册失败消息。</summary>
        public CharacterSystemFailedEvent(string reason)
        {
            Reason = reason ?? string.Empty;
        }

        /// <summary>获取失败原因。</summary>
        public string Reason { get; }
    }
}
