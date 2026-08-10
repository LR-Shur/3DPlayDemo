namespace Train.Dialogue.Core
{
    /// <summary>
    /// 描述一场对话会话当前所处的状态。
    /// </summary>
    public enum DialogueSessionStatus
    {
        /// <summary>会话已经创建，但尚未开始。</summary>
        NotStarted = 0,

        /// <summary>正在展示台词，等待继续指令。</summary>
        AwaitingContinue = 1,

        /// <summary>正在展示选项，等待选择指令。</summary>
        AwaitingChoice = 2,

        /// <summary>对话已经沿图正常结束。</summary>
        Completed = 3,

        /// <summary>对话被外部主动取消。</summary>
        Cancelled = 4
    }
}
