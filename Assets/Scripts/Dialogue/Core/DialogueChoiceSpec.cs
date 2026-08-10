using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Train.Dialogue.Core
{
    /// <summary>
    /// 描述一个不可变的玩家选项、可见条件、选中命令和目标节点。
    /// </summary>
    public sealed class DialogueChoiceSpec
    {
        private readonly ReadOnlyCollection<DialogueConditionSpec> _conditions;
        private readonly ReadOnlyCollection<DialogueCommandSpec> _commands;

        /// <summary>创建一个对话选项定义。</summary>
        public DialogueChoiceSpec(
            string choiceId,
            string text,
            string nextNodeId,
            IEnumerable<DialogueConditionSpec> conditions = null,
            IEnumerable<DialogueCommandSpec> commands = null)
        {
            if (string.IsNullOrWhiteSpace(choiceId))
            {
                throw new ArgumentException(
                    "对话选项标识不能为空。",
                    nameof(choiceId));
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException(
                    "对话选项文本不能为空。",
                    nameof(text));
            }

            if (string.IsNullOrWhiteSpace(nextNodeId))
            {
                throw new ArgumentException(
                    "对话选项目标节点不能为空。",
                    nameof(nextNodeId));
            }

            ChoiceId = choiceId;
            Text = text;
            NextNodeId = nextNodeId;
            _conditions = CopyConditions(conditions);
            _commands = CopyCommands(commands);
        }

        /// <summary>获取选项在所属节点内的稳定标识。</summary>
        public string ChoiceId { get; }

        /// <summary>获取展示给玩家的选项文本。</summary>
        public string Text { get; }

        /// <summary>获取选中后前往的目标节点标识。</summary>
        public string NextNodeId { get; }

        /// <summary>获取决定选项是否可见的只读条件集合。</summary>
        public IReadOnlyList<DialogueConditionSpec> Conditions => _conditions;

        /// <summary>获取选中选项后执行的只读命令集合。</summary>
        public IReadOnlyList<DialogueCommandSpec> Commands => _commands;

        private static ReadOnlyCollection<DialogueConditionSpec>
            CopyConditions(IEnumerable<DialogueConditionSpec> source)
        {
            var copy = new List<DialogueConditionSpec>();
            if (source != null)
            {
                foreach (var item in source)
                {
                    copy.Add(
                        item ?? throw new ArgumentException(
                            "对话选项条件不能包含 null。",
                            nameof(source)));
                }
            }

            return copy.AsReadOnly();
        }

        private static ReadOnlyCollection<DialogueCommandSpec>
            CopyCommands(IEnumerable<DialogueCommandSpec> source)
        {
            var copy = new List<DialogueCommandSpec>();
            if (source != null)
            {
                foreach (var item in source)
                {
                    copy.Add(
                        item ?? throw new ArgumentException(
                            "对话选项命令不能包含 null。",
                            nameof(source)));
                }
            }

            return copy.AsReadOnly();
        }
    }
}
