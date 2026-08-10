using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Train.Dialogue.Core
{
    /// <summary>
    /// 描述对话图中的一个不可变节点及其进入条件、进入命令和后继关系。
    /// </summary>
    public sealed class DialogueNodeSpec
    {
        private readonly ReadOnlyCollection<DialogueChoiceSpec> _choices;
        private readonly ReadOnlyCollection<DialogueConditionSpec> _conditions;
        private readonly ReadOnlyCollection<DialogueCommandSpec> _commands;

        /// <summary>创建一个对话节点定义。</summary>
        public DialogueNodeSpec(
            string nodeId,
            DialogueNodeKind kind,
            string speakerId,
            string text,
            string nextNodeId = null,
            IEnumerable<DialogueChoiceSpec> choices = null,
            IEnumerable<DialogueConditionSpec> conditions = null,
            IEnumerable<DialogueCommandSpec> commands = null)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                throw new ArgumentException(
                    "对话节点标识不能为空。",
                    nameof(nodeId));
            }

            if (!Enum.IsDefined(typeof(DialogueNodeKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            NodeId = nodeId;
            Kind = kind;
            SpeakerId = speakerId ?? string.Empty;
            Text = text ?? string.Empty;
            NextNodeId = string.IsNullOrWhiteSpace(nextNodeId)
                ? null
                : nextNodeId;
            _choices = CopyChoices(choices);
            _conditions = CopyConditions(conditions);
            _commands = CopyCommands(commands);

            ValidateShape();
        }

        /// <summary>获取节点在图内的稳定标识。</summary>
        public string NodeId { get; }

        /// <summary>获取节点职责类型。</summary>
        public DialogueNodeKind Kind { get; }

        /// <summary>获取说话角色的稳定标识；旁白可为空。</summary>
        public string SpeakerId { get; }

        /// <summary>获取台词或选项提示文本。</summary>
        public string Text { get; }

        /// <summary>获取线性后继节点标识；没有后继时为空。</summary>
        public string NextNodeId { get; }

        /// <summary>获取选择节点的只读选项集合。</summary>
        public IReadOnlyList<DialogueChoiceSpec> Choices => _choices;

        /// <summary>获取决定节点是否进入的只读条件集合。</summary>
        public IReadOnlyList<DialogueConditionSpec> Conditions => _conditions;

        /// <summary>获取节点进入时执行的只读命令集合。</summary>
        public IReadOnlyList<DialogueCommandSpec> Commands => _commands;

        private void ValidateShape()
        {
            if (Kind == DialogueNodeKind.Line)
            {
                if (string.IsNullOrWhiteSpace(Text))
                {
                    throw new ArgumentException(
                        $"台词节点 '{NodeId}' 的文本不能为空。");
                }

                if (_choices.Count != 0)
                {
                    throw new ArgumentException(
                        $"台词节点 '{NodeId}' 不能包含选项。");
                }
            }
            else if (Kind == DialogueNodeKind.Choice)
            {
                if (_choices.Count == 0)
                {
                    throw new ArgumentException(
                        $"选择节点 '{NodeId}' 至少需要一个选项。");
                }

                if (NextNodeId != null)
                {
                    throw new ArgumentException(
                        $"选择节点 '{NodeId}' 不能配置线性后继节点。");
                }

                var choiceIds = new HashSet<string>(StringComparer.Ordinal);
                for (var i = 0; i < _choices.Count; i++)
                {
                    if (!choiceIds.Add(_choices[i].ChoiceId))
                    {
                        throw new ArgumentException(
                            $"选择节点 '{NodeId}' 包含重复选项标识 " +
                            $"'{_choices[i].ChoiceId}'。");
                    }
                }
            }
            else
            {
                if (_choices.Count != 0 || NextNodeId != null)
                {
                    throw new ArgumentException(
                        $"结束节点 '{NodeId}' 不能包含选项或后继节点。");
                }
            }
        }

        private static ReadOnlyCollection<DialogueChoiceSpec>
            CopyChoices(IEnumerable<DialogueChoiceSpec> source)
        {
            var copy = new List<DialogueChoiceSpec>();
            if (source != null)
            {
                foreach (var item in source)
                {
                    copy.Add(
                        item ?? throw new ArgumentException(
                            "对话节点选项不能包含 null。",
                            nameof(source)));
                }
            }

            return copy.AsReadOnly();
        }

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
                            "对话节点条件不能包含 null。",
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
                            "对话节点命令不能包含 null。",
                            nameof(source)));
                }
            }

            return copy.AsReadOnly();
        }
    }
}
