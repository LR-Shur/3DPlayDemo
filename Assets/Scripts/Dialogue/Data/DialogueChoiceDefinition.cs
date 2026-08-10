using System;
using System.Collections.Generic;
using Train.Dialogue.Core;
using UnityEngine;

namespace Train.Dialogue.Data
{
    /// <summary>
    /// 保存一个可序列化选项及其条件、命令和跳转目标。
    /// </summary>
    [Serializable]
    public sealed class DialogueChoiceDefinition
    {
        [SerializeField] private string _choiceId;
        [TextArea(1, 3)]
        [SerializeField] private string _text;
        [SerializeField] private string _nextNodeId;
        [SerializeField]
        private List<DialogueConditionDefinition> _conditions = new();
        [SerializeField]
        private List<DialogueCommandDefinition> _commands = new();

        /// <summary>获取选项稳定标识。</summary>
        public string ChoiceId => _choiceId;

        /// <summary>获取选项展示文本。</summary>
        public string Text => _text;

        /// <summary>将 Unity 序列化数据转换为不可变选项配置。</summary>
        public DialogueChoiceSpec ToCoreSpec()
        {
            var conditions =
                new DialogueConditionSpec[_conditions.Count];
            for (var i = 0; i < _conditions.Count; i++)
            {
                conditions[i] = (_conditions[i] ??
                    throw new InvalidOperationException(
                        $"对话选项 '{_choiceId}' 的条件包含 null。"))
                    .ToCoreSpec();
            }

            var commands = new DialogueCommandSpec[_commands.Count];
            for (var i = 0; i < _commands.Count; i++)
            {
                commands[i] = (_commands[i] ??
                    throw new InvalidOperationException(
                        $"对话选项 '{_choiceId}' 的命令包含 null。"))
                    .ToCoreSpec();
            }

            return new DialogueChoiceSpec(
                _choiceId,
                _text,
                _nextNodeId,
                conditions,
                commands);
        }
    }
}
