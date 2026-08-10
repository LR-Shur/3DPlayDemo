namespace Train.Dialogue.Core
{
    /// <summary>
    /// 定义一种可插拔的对话条件处理器，不依赖场景对象或表现层。
    /// </summary>
    public interface IDialogueCondition
    {
        /// <summary>获取与配置中的条件类型对应的稳定标识。</summary>
        string TypeId { get; }

        /// <summary>根据条件参数和变量表判断内容是否可用。</summary>
        bool Evaluate(
            DialogueConditionSpec condition,
            IDialogueVariableStore variables);
    }
}
