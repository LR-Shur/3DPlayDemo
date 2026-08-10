namespace Train.Dialogue.Core
{
    /// <summary>
    /// 定义一种可插拔的对话命令处理器，用于提交对话产生的领域副作用。
    /// </summary>
    public interface IDialogueCommand
    {
        /// <summary>获取与配置中的命令类型对应的稳定标识。</summary>
        string TypeId { get; }

        /// <summary>使用命令参数更新抽象变量表或其他注入的领域端口。</summary>
        void Execute(
            DialogueCommandSpec command,
            IDialogueVariableStore variables);
    }
}
