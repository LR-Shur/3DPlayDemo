using System;

namespace Train.Dialogue.Core
{
    /// <summary>
    /// 判断变量是否存在且与配置值按序号规则完全相等。
    /// </summary>
    public sealed class VariableEqualsDialogueCondition : IDialogueCondition
    {
        /// <summary>获取内置条件稳定标识。</summary>
        public string TypeId => "variable_equals";

        /// <inheritdoc />
        public bool Evaluate(
            DialogueConditionSpec condition,
            IDialogueVariableStore variables)
        {
            if (condition == null)
            {
                throw new ArgumentNullException(nameof(condition));
            }

            if (variables == null)
            {
                throw new ArgumentNullException(nameof(variables));
            }

            return variables.TryGetValue(condition.Key, out var value) &&
                   string.Equals(
                       value,
                       condition.Value,
                       StringComparison.Ordinal);
        }
    }
}
