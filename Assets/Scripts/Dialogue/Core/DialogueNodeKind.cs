namespace Train.Dialogue.Core
{
    /// <summary>
    /// 定义对话图节点的职责类型。
    /// </summary>
    public enum DialogueNodeKind
    {
        /// <summary>展示一句台词，并等待玩家继续。</summary>
        Line = 0,

        /// <summary>展示可用选项，并等待玩家选择。</summary>
        Choice = 1,

        /// <summary>结束当前对话会话。</summary>
        End = 2
    }
}
