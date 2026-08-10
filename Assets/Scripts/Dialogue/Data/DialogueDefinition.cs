using System;
using System.Collections.Generic;
using Train.Dialogue.Core;
using UnityEngine;

namespace Train.Dialogue.Data
{
    /// <summary>
    /// 以 ScriptableObject 保存一张对话图，并负责转换为纯领域定义。
    /// </summary>
    [CreateAssetMenu(
        menuName = "Train/Dialogue/Dialogue Definition",
        fileName = "DialogueDefinition")]
    public sealed class DialogueDefinition : ScriptableObject
    {
        [SerializeField] private string _dialogueId;
        [SerializeField] private string _title;
        [SerializeField] private string _entryNodeId;
        [SerializeField] private List<DialogueNodeDefinition> _nodes = new();

        /// <summary>获取对话稳定标识。</summary>
        public string DialogueId => _dialogueId;

        /// <summary>获取对话展示标题。</summary>
        public string Title => _title;

        /// <summary>获取入口节点稳定标识。</summary>
        public string EntryNodeId => _entryNodeId;

        /// <summary>获取只读节点配置列表。</summary>
        public IReadOnlyList<DialogueNodeDefinition> Nodes => _nodes;

        /// <summary>将 Unity 资产转换并校验为不可变对话图。</summary>
        public DialogueGraphSpec ToCoreSpec()
        {
            var nodes = new DialogueNodeSpec[_nodes.Count];
            for (var i = 0; i < _nodes.Count; i++)
            {
                nodes[i] = (_nodes[i] ??
                    throw new InvalidOperationException(
                        $"对话资产 '{name}' 的节点包含 null。"))
                    .ToCoreSpec();
            }

            return new DialogueGraphSpec(
                _dialogueId,
                _title,
                _entryNodeId,
                nodes);
        }
    }
}
