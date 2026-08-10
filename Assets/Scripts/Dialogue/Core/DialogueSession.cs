using System;
using System.Collections.Generic;

namespace Train.Dialogue.Core
{
    /// <summary>
    /// 驱动单场对话的纯 C# 状态机，负责条件过滤、命令执行与不可变快照。
    /// </summary>
    public sealed class DialogueSession
    {
        private readonly DialogueGraphSpec _graph;
        private readonly IDialogueVariableStore _variables;
        private readonly DialogueHandlerRegistry _handlers;
        private readonly string _sessionId;
        private DialogueNodeSpec _currentNode;
        private IReadOnlyList<DialogueChoiceSpec> _availableChoices =
            Array.Empty<DialogueChoiceSpec>();

        /// <summary>创建一场尚未开始的对话会话。</summary>
        public DialogueSession(
            string sessionId,
            DialogueGraphSpec graph,
            IDialogueVariableStore variables = null,
            DialogueHandlerRegistry handlers = null)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                throw new ArgumentException(
                    "对话会话标识不能为空。",
                    nameof(sessionId));
            }

            _sessionId = sessionId;
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _variables = variables ?? new DialogueVariableStore();
            _handlers = handlers ?? new DialogueHandlerRegistry();
            Status = DialogueSessionStatus.NotStarted;
        }

        /// <summary>获取会话当前状态。</summary>
        public DialogueSessionStatus Status { get; private set; }

        /// <summary>获取当前修订号，每次有效外部指令只增加一次。</summary>
        public long Revision { get; private set; }

        /// <summary>获取当前不可变会话快照。</summary>
        public DialogueSessionSnapshot Snapshot => CreateSnapshot();

        /// <summary>从图的入口节点开始会话。</summary>
        public void Start()
        {
            EnsureStatus(DialogueSessionStatus.NotStarted, "开始");
            var nextRevision = checked(Revision + 1);
            MoveTo(_graph.EntryNodeId);
            Revision = nextRevision;
        }

        /// <summary>从当前台词节点推进到后继节点或正常结束。</summary>
        public void Continue()
        {
            EnsureStatus(DialogueSessionStatus.AwaitingContinue, "继续");
            var nextRevision = checked(Revision + 1);
            if (_currentNode.NextNodeId == null)
            {
                Complete();
            }
            else
            {
                MoveTo(_currentNode.NextNodeId);
            }

            Revision = nextRevision;
        }

        /// <summary>选择当前可见选项并返回被选择选项的不可变快照。</summary>
        public DialogueChoiceSnapshot Choose(string choiceId)
        {
            EnsureStatus(DialogueSessionStatus.AwaitingChoice, "选择");
            if (string.IsNullOrWhiteSpace(choiceId))
            {
                throw new ArgumentException(
                    "对话选项标识不能为空。",
                    nameof(choiceId));
            }

            DialogueChoiceSpec selected = null;
            for (var i = 0; i < _availableChoices.Count; i++)
            {
                if (string.Equals(
                        _availableChoices[i].ChoiceId,
                        choiceId,
                        StringComparison.Ordinal))
                {
                    selected = _availableChoices[i];
                    break;
                }
            }

            if (selected == null)
            {
                throw new InvalidOperationException(
                    $"选项 '{choiceId}' 在当前节点不可用。");
            }

            var nextRevision = checked(Revision + 1);
            var selectedSnapshot = new DialogueChoiceSnapshot(
                selected.ChoiceId,
                selected.Text);
            _handlers.Execute(selected.Commands, _variables);
            MoveTo(selected.NextNodeId);
            Revision = nextRevision;
            return selectedSnapshot;
        }

        /// <summary>主动取消一场正在进行的会话。</summary>
        public void Cancel()
        {
            if (Status != DialogueSessionStatus.AwaitingContinue &&
                Status != DialogueSessionStatus.AwaitingChoice)
            {
                throw new InvalidOperationException(
                    $"会话处于 {Status} 时不能取消。");
            }

            var nextRevision = checked(Revision + 1);
            _currentNode = null;
            _availableChoices = Array.Empty<DialogueChoiceSpec>();
            Status = DialogueSessionStatus.Cancelled;
            Revision = nextRevision;
        }

        /// <summary>创建当前状态的不可变防御性快照。</summary>
        public DialogueSessionSnapshot CreateSnapshot()
        {
            var choices =
                new DialogueChoiceSnapshot[_availableChoices.Count];
            for (var i = 0; i < _availableChoices.Count; i++)
            {
                choices[i] = new DialogueChoiceSnapshot(
                    _availableChoices[i].ChoiceId,
                    _availableChoices[i].Text);
            }

            return new DialogueSessionSnapshot(
                _sessionId,
                _graph.DialogueId,
                _graph.Title,
                Status,
                Revision,
                _currentNode?.NodeId,
                _currentNode?.Kind,
                _currentNode?.SpeakerId,
                _currentNode?.Text,
                choices);
        }

        private void MoveTo(string nodeId)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            while (true)
            {
                if (!visited.Add(nodeId))
                {
                    throw new InvalidOperationException(
                        $"对话 '{_graph.DialogueId}' 在条件跳过过程中形成循环，" +
                        $"节点为 '{nodeId}'。");
                }

                var node = _graph.GetNode(nodeId);
                if (!_handlers.AreSatisfied(node.Conditions, _variables))
                {
                    if (node.NextNodeId == null)
                    {
                        Complete();
                        return;
                    }

                    nodeId = node.NextNodeId;
                    continue;
                }

                _handlers.Execute(node.Commands, _variables);
                if (node.Kind == DialogueNodeKind.End)
                {
                    Complete();
                    return;
                }

                _currentNode = node;
                if (node.Kind == DialogueNodeKind.Line)
                {
                    _availableChoices = Array.Empty<DialogueChoiceSpec>();
                    Status = DialogueSessionStatus.AwaitingContinue;
                    return;
                }

                var available = new List<DialogueChoiceSpec>();
                for (var i = 0; i < node.Choices.Count; i++)
                {
                    var choice = node.Choices[i];
                    if (_handlers.AreSatisfied(
                            choice.Conditions,
                            _variables))
                    {
                        available.Add(choice);
                    }
                }

                if (available.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"选择节点 '{node.NodeId}' 没有任何可用选项。");
                }

                _availableChoices = available.AsReadOnly();
                Status = DialogueSessionStatus.AwaitingChoice;
                return;
            }
        }

        private void Complete()
        {
            _currentNode = null;
            _availableChoices = Array.Empty<DialogueChoiceSpec>();
            Status = DialogueSessionStatus.Completed;
        }

        private void EnsureStatus(
            DialogueSessionStatus expected,
            string operation)
        {
            if (Status != expected)
            {
                throw new InvalidOperationException(
                    $"会话处于 {Status} 时不能执行“{operation}”，" +
                    $"要求状态为 {expected}。");
            }
        }
    }
}
