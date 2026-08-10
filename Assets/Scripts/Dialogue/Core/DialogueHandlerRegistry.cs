using System;
using System.Collections.Generic;

namespace Train.Dialogue.Core
{
    /// <summary>
    /// 按稳定类型标识管理条件和命令处理器，并集中执行配置。
    /// </summary>
    public sealed class DialogueHandlerRegistry
    {
        private readonly Dictionary<string, IDialogueCondition> _conditions =
            new(StringComparer.Ordinal);
        private readonly Dictionary<string, IDialogueCommand> _commands =
            new(StringComparer.Ordinal);

        /// <summary>
        /// 创建处理器注册表；默认安装变量相等、变量存在和变量写入处理器。
        /// </summary>
        public DialogueHandlerRegistry(
            IEnumerable<IDialogueCondition> conditions = null,
            IEnumerable<IDialogueCommand> commands = null)
        {
            RegisterCondition(new VariableEqualsDialogueCondition());
            RegisterCondition(new VariableExistsDialogueCondition());
            RegisterCommand(new SetVariableDialogueCommand());

            if (conditions != null)
            {
                foreach (var condition in conditions)
                {
                    RegisterCondition(condition);
                }
            }

            if (commands != null)
            {
                foreach (var command in commands)
                {
                    RegisterCommand(command);
                }
            }
        }

        /// <summary>判断一组条件是否全部满足。</summary>
        public bool AreSatisfied(
            IReadOnlyList<DialogueConditionSpec> conditions,
            IDialogueVariableStore variables)
        {
            if (conditions == null)
            {
                throw new ArgumentNullException(nameof(conditions));
            }

            if (variables == null)
            {
                throw new ArgumentNullException(nameof(variables));
            }

            for (var i = 0; i < conditions.Count; i++)
            {
                var condition = conditions[i];
                if (!_conditions.TryGetValue(
                        condition.TypeId,
                        out var handler))
                {
                    throw new InvalidOperationException(
                        $"未注册对话条件处理器 '{condition.TypeId}'。");
                }

                var result = handler.Evaluate(condition, variables);
                if (condition.Negate ? result : !result)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>按配置顺序执行一组命令。</summary>
        public void Execute(
            IReadOnlyList<DialogueCommandSpec> commands,
            IDialogueVariableStore variables)
        {
            if (commands == null)
            {
                throw new ArgumentNullException(nameof(commands));
            }

            if (variables == null)
            {
                throw new ArgumentNullException(nameof(variables));
            }

            for (var i = 0; i < commands.Count; i++)
            {
                var command = commands[i];
                if (!_commands.TryGetValue(
                        command.TypeId,
                        out var handler))
                {
                    throw new InvalidOperationException(
                        $"未注册对话命令处理器 '{command.TypeId}'。");
                }

                handler.Execute(command, variables);
            }
        }

        private void RegisterCondition(IDialogueCondition condition)
        {
            if (condition == null)
            {
                throw new ArgumentException(
                    "对话条件处理器不能为 null。",
                    nameof(condition));
            }

            if (string.IsNullOrWhiteSpace(condition.TypeId))
            {
                throw new ArgumentException(
                    "对话条件处理器的 TypeId 不能为空。",
                    nameof(condition));
            }

            if (!_conditions.TryAdd(condition.TypeId, condition))
            {
                throw new ArgumentException(
                    $"重复注册对话条件处理器 '{condition.TypeId}'。",
                    nameof(condition));
            }
        }

        private void RegisterCommand(IDialogueCommand command)
        {
            if (command == null)
            {
                throw new ArgumentException(
                    "对话命令处理器不能为 null。",
                    nameof(command));
            }

            if (string.IsNullOrWhiteSpace(command.TypeId))
            {
                throw new ArgumentException(
                    "对话命令处理器的 TypeId 不能为空。",
                    nameof(command));
            }

            if (!_commands.TryAdd(command.TypeId, command))
            {
                throw new ArgumentException(
                    $"重复注册对话命令处理器 '{command.TypeId}'。",
                    nameof(command));
            }
        }
    }
}
