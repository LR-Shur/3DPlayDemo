using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Train.Dialogue.Core
{
    /// <summary>
    /// 表示一张经过引用校验的不可变对话图。
    /// </summary>
    public sealed class DialogueGraphSpec
    {
        private readonly ReadOnlyCollection<DialogueNodeSpec> _nodes;
        private readonly Dictionary<string, DialogueNodeSpec> _nodeById;

        /// <summary>创建并校验一张对话图。</summary>
        public DialogueGraphSpec(
            string dialogueId,
            string title,
            string entryNodeId,
            IEnumerable<DialogueNodeSpec> nodes)
        {
            if (string.IsNullOrWhiteSpace(dialogueId))
            {
                throw new ArgumentException(
                    "对话标识不能为空。",
                    nameof(dialogueId));
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException(
                    "对话标题不能为空。",
                    nameof(title));
            }

            if (string.IsNullOrWhiteSpace(entryNodeId))
            {
                throw new ArgumentException(
                    "对话入口节点标识不能为空。",
                    nameof(entryNodeId));
            }

            DialogueId = dialogueId;
            Title = title;
            EntryNodeId = entryNodeId;
            _nodeById = new Dictionary<string, DialogueNodeSpec>(
                StringComparer.Ordinal);
            var copy = new List<DialogueNodeSpec>();
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }

            foreach (var node in nodes)
            {
                if (node == null)
                {
                    throw new ArgumentException(
                        "对话图节点不能包含 null。",
                        nameof(nodes));
                }

                if (!_nodeById.TryAdd(node.NodeId, node))
                {
                    throw new ArgumentException(
                        $"对话图包含重复节点标识 '{node.NodeId}'。",
                        nameof(nodes));
                }

                copy.Add(node);
            }

            if (copy.Count == 0)
            {
                throw new ArgumentException(
                    "对话图至少需要一个节点。",
                    nameof(nodes));
            }

            _nodes = copy.AsReadOnly();
            ValidateReferences();
        }

        /// <summary>获取对话图稳定标识。</summary>
        public string DialogueId { get; }

        /// <summary>获取对话标题。</summary>
        public string Title { get; }

        /// <summary>获取入口节点稳定标识。</summary>
        public string EntryNodeId { get; }

        /// <summary>获取按配置顺序保存的只读节点集合。</summary>
        public IReadOnlyList<DialogueNodeSpec> Nodes => _nodes;

        /// <summary>按稳定标识获取节点，找不到时抛出明确异常。</summary>
        public DialogueNodeSpec GetNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId) ||
                !_nodeById.TryGetValue(nodeId, out var node))
            {
                throw new KeyNotFoundException(
                    $"对话 '{DialogueId}' 中不存在节点 '{nodeId}'。");
            }

            return node;
        }

        private void ValidateReferences()
        {
            if (!_nodeById.ContainsKey(EntryNodeId))
            {
                throw new ArgumentException(
                    $"对话入口节点 '{EntryNodeId}' 不存在。");
            }

            for (var i = 0; i < _nodes.Count; i++)
            {
                var node = _nodes[i];
                if (node.NextNodeId != null &&
                    !_nodeById.ContainsKey(node.NextNodeId))
                {
                    throw new ArgumentException(
                        $"节点 '{node.NodeId}' 引用了不存在的后继节点 " +
                        $"'{node.NextNodeId}'。");
                }

                for (var choiceIndex = 0;
                     choiceIndex < node.Choices.Count;
                     choiceIndex++)
                {
                    var choice = node.Choices[choiceIndex];
                    if (!_nodeById.ContainsKey(choice.NextNodeId))
                    {
                        throw new ArgumentException(
                            $"选项 '{choice.ChoiceId}' 引用了不存在的节点 " +
                            $"'{choice.NextNodeId}'。");
                    }
                }
            }
        }
    }
}
