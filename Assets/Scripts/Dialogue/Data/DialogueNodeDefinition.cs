using System;
using System.Collections.Generic;
using Train.Dialogue.Core;
using UnityEngine;

namespace Train.Dialogue.Data
{
    /// <summary>
    /// 保存一个可序列化对话节点及其条件、命令、选项和后继关系。
    /// </summary>
    [Serializable]
    public sealed class DialogueNodeDefinition
    {
        [SerializeField] private string _nodeId;
        [SerializeField] private DialogueNodeKind _kind;
        [SerializeField] private string _speakerId;
        [TextArea(2, 6)]
        [SerializeField] private string _text;
        [SerializeField] private string _nextNodeId;
        [SerializeField]
        private List<DialogueChoiceDefinition> _choices = new();
        [SerializeField]
        private List<DialogueConditionDefinition> _conditions = new();
        [SerializeField]
        private List<DialogueCommandDefinition> _commands = new();

        /// <summary>获取节点稳定标识。</summary>
        public string NodeId => _nodeId;

        /// <summary>获取节点职责类型。</summary>
        public DialogueNodeKind Kind => _kind;

        /// <summary>将 Unity 序列化数据转换为不可变节点配置。</summary>
        public DialogueNodeSpec ToCoreSpec()
        {
            var choices = new DialogueChoiceSpec[_choices.Count];
            for (var i = 0; i < _choices.Count; i++)
            {
                choices[i] = (_choices[i] ??
                    throw new InvalidOperationException(
                        $"对话节点 '{_nodeId}' 的选项包含 null。"))
                    .ToCoreSpec();
            }

            var conditions =
                new DialogueConditionSpec[_conditions.Count];
            for (var i = 0; i < _conditions.Count; i++)
            {
                conditions[i] = (_conditions[i] ??
                    throw new InvalidOperationException(
                        $"对话节点 '{_nodeId}' 的条件包含 null。"))
                    .ToCoreSpec();
            }

            var commands = new DialogueCommandSpec[_commands.Count];
            for (var i = 0; i < _commands.Count; i++)
            {
                commands[i] = (_commands[i] ??
                    throw new InvalidOperationException(
                        $"对话节点 '{_nodeId}' 的命令包含 null。"))
                    .ToCoreSpec();
            }

            return new DialogueNodeSpec(
                _nodeId,
                _kind,
                _speakerId,
                _text,
                _nextNodeId,
                choices,
                conditions,
                commands);
        }
    }
}
