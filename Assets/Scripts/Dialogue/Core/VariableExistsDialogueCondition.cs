using System;

namespace Train.Dialogue.Core
{
    /// <summary>
    /// 判断变量表中是否已经登记指定键。
    /// </summary>
    public sealed class VariableExistsDialogueCondition : IDialogueCondition
    {
        /// <summary>获取内置条件稳定标识。</summary>
        public string TypeId => "variable_exists";

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

            return variables.TryGetValue(condition.Key, out _);
        }
    }
}
