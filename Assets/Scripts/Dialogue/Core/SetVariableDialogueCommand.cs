using System;

namespace Train.Dialogue.Core
{
    /// <summary>
    /// 将配置值写入指定变量键，是对话系统提供的基础命令。
    /// </summary>
    public sealed class SetVariableDialogueCommand : IDialogueCommand
    {
        /// <summary>获取内置命令稳定标识。</summary>
        public string TypeId => "set_variable";

        /// <inheritdoc />
        public void Execute(
            DialogueCommandSpec command,
            IDialogueVariableStore variables)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (variables == null)
            {
                throw new ArgumentNullException(nameof(variables));
            }

            variables.SetValue(command.Key, command.Value);
        }
    }
}
