namespace Train.Dialogue.Events
{
    /// <summary>
    /// 表示对话目录和对话服务已经可以使用。
    /// </summary>
    public readonly struct DialogueSystemReadyEvent
    {
        /// <summary>创建对话系统就绪消息。</summary>
        public DialogueSystemReadyEvent(int dialogueCount)
        {
            DialogueCount = dialogueCount;
        }

        /// <summary>获取可启动对话资产数量。</summary>
        public int DialogueCount { get; }
    }
}
