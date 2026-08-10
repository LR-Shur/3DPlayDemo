using System;
using System.Collections.Generic;
using NUnit.Framework;
using Train.Dialogue.Core;

namespace Train.Tests.EditMode.Dialogue
{
    /// <summary>
    /// 验证纯对话会话状态机、条件分支、命令和不可变快照契约。
    /// </summary>
    public sealed class DialogueSessionTests
    {
        [Test]
        public void ValidFlow_LineChoiceCommandAndCompletionAdvanceOnceEach()
        {
            var variables = new DialogueVariableStore();
            var session = new DialogueSession(
                "session-1",
                CreateBranchingGraph(),
                variables);

            session.Start();

            Assert.That(
                session.Status,
                Is.EqualTo(DialogueSessionStatus.AwaitingContinue));
            Assert.That(session.Snapshot.CurrentNodeId, Is.EqualTo("intro"));
            Assert.That(session.Revision, Is.EqualTo(1));

            session.Continue();

            Assert.That(
                session.Status,
                Is.EqualTo(DialogueSessionStatus.AwaitingChoice));
            Assert.That(session.Snapshot.Choices.Count, Is.EqualTo(1));
            Assert.That(
                session.Snapshot.Choices[0].ChoiceId,
                Is.EqualTo("normal"));

            var selected = session.Choose("normal");

            Assert.That(selected.Text, Is.EqualTo("普通回答"));
            Assert.That(session.Snapshot.CurrentNodeId, Is.EqualTo("result"));
            Assert.That(
                variables.TryGetValue("story.answer", out var answer),
                Is.True);
            Assert.That(answer, Is.EqualTo("normal"));

            session.Continue();

            Assert.That(
                session.Status,
                Is.EqualTo(DialogueSessionStatus.Completed));
            Assert.That(session.Revision, Is.EqualTo(4));
            Assert.That(session.Snapshot.CurrentNodeId, Is.Null);
        }

        [Test]
        public void ChoiceConditions_ExposeSpecialBranchWhenVariableMatches()
        {
            var variables = new DialogueVariableStore();
            variables.SetValue("story.trusted", "true");
            var session = new DialogueSession(
                "session-2",
                CreateBranchingGraph(),
                variables);

            session.Start();
            session.Continue();

            Assert.That(session.Snapshot.Choices.Count, Is.EqualTo(2));
            Assert.That(
                session.Snapshot.Choices[0].ChoiceId,
                Is.EqualTo("special"));
            Assert.That(
                session.Snapshot.Choices[1].ChoiceId,
                Is.EqualTo("normal"));
        }

        [Test]
        public void NodeCondition_WhenFalseSkipsToConfiguredFallback()
        {
            var gated = new DialogueNodeSpec(
                "gated",
                DialogueNodeKind.Line,
                "character.guard",
                "只有授权后才会看到。",
                "fallback",
                conditions: new[]
                {
                    new DialogueConditionSpec(
                        "variable_equals",
                        "story.authorized",
                        "true")
                });
            var fallback = new DialogueNodeSpec(
                "fallback",
                DialogueNodeKind.Line,
                "character.guard",
                "权限不足，已切换到普通说明。",
                "end");
            var end = new DialogueNodeSpec(
                "end",
                DialogueNodeKind.End,
                null,
                null);
            var graph = new DialogueGraphSpec(
                "conditional_node",
                "条件节点",
                "gated",
                new[] { gated, fallback, end });
            var session = new DialogueSession("session-3", graph);

            session.Start();

            Assert.That(
                session.Snapshot.CurrentNodeId,
                Is.EqualTo("fallback"));
            Assert.That(session.Revision, Is.EqualTo(1));
        }

        [Test]
        public void NegatedCondition_InvertsBuiltInHandlerResult()
        {
            var variables = new DialogueVariableStore();
            variables.SetValue("flag", "yes");
            var registry = new DialogueHandlerRegistry();
            var conditions = new[]
            {
                new DialogueConditionSpec(
                    "variable_equals",
                    "flag",
                    "yes",
                    negate: true)
            };

            Assert.That(
                registry.AreSatisfied(conditions, variables),
                Is.False);
        }

        [Test]
        public void InvalidCommandsForCurrentState_ThrowWithoutRevisionChange()
        {
            var session = new DialogueSession(
                "session-invalid",
                CreateBranchingGraph());

            Assert.Throws<InvalidOperationException>(session.Continue);
            Assert.Throws<InvalidOperationException>(
                () => session.Choose("normal"));
            Assert.Throws<InvalidOperationException>(session.Cancel);
            Assert.That(session.Revision, Is.Zero);

            session.Start();
            Assert.Throws<InvalidOperationException>(
                () => session.Choose("normal"));
            Assert.That(session.Revision, Is.EqualTo(1));
        }

        [Test]
        public void UnknownOrHiddenChoice_IsRejectedAtomically()
        {
            var session = new DialogueSession(
                "session-choice-invalid",
                CreateBranchingGraph());
            session.Start();
            session.Continue();
            var before = session.Snapshot;

            Assert.Throws<InvalidOperationException>(
                () => session.Choose("special"));

            Assert.That(session.Revision, Is.EqualTo(before.Revision));
            Assert.That(
                session.Snapshot.CurrentNodeId,
                Is.EqualTo(before.CurrentNodeId));
            Assert.That(
                session.Snapshot.Choices.Count,
                Is.EqualTo(before.Choices.Count));
        }

        [Test]
        public void Cancel_ProducesTerminalSnapshotAndRejectsSecondCancel()
        {
            var session = new DialogueSession(
                "session-cancel",
                CreateBranchingGraph());
            session.Start();

            session.Cancel();

            Assert.That(
                session.Status,
                Is.EqualTo(DialogueSessionStatus.Cancelled));
            Assert.That(session.Snapshot.IsTerminal, Is.True);
            Assert.That(session.Snapshot.CurrentNodeId, Is.Null);
            Assert.Throws<InvalidOperationException>(session.Cancel);
        }

        [Test]
        public void MissingConditionHandler_FailsWithActionableMessage()
        {
            var choice = new DialogueChoiceSpec(
                "locked",
                "未知条件选项",
                "end",
                new[]
                {
                    new DialogueConditionSpec(
                        "feature_not_installed",
                        "flag")
                });
            var graph = new DialogueGraphSpec(
                "missing_handler",
                "缺少处理器",
                "choice",
                new[]
                {
                    new DialogueNodeSpec(
                        "choice",
                        DialogueNodeKind.Choice,
                        null,
                        "请选择",
                        choices: new[] { choice }),
                    new DialogueNodeSpec(
                        "end",
                        DialogueNodeKind.End,
                        null,
                        null)
                });
            var session = new DialogueSession("session-missing", graph);

            var exception = Assert.Throws<InvalidOperationException>(
                session.Start);

            Assert.That(
                exception.Message,
                Does.Contain("feature_not_installed"));
            Assert.That(session.Revision, Is.Zero);
        }

        [Test]
        public void Graph_RejectsDuplicateIdsAndMissingTargets()
        {
            var duplicate = new DialogueNodeSpec(
                "same",
                DialogueNodeKind.Line,
                null,
                "重复",
                "same");
            Assert.Throws<ArgumentException>(
                () => new DialogueGraphSpec(
                    "duplicate",
                    "重复节点",
                    "same",
                    new[] { duplicate, duplicate }));

            var broken = new DialogueNodeSpec(
                "start",
                DialogueNodeKind.Line,
                null,
                "错误引用",
                "missing");
            Assert.Throws<ArgumentException>(
                () => new DialogueGraphSpec(
                    "broken",
                    "错误引用",
                    "start",
                    new[] { broken }));
        }

        [Test]
        public void GraphAndSnapshots_AreDefensiveReadOnlyCopies()
        {
            var sourceChoices = new List<DialogueChoiceSpec>
            {
                new(
                    "first",
                    "第一个",
                    "end")
            };
            var choiceNode = new DialogueNodeSpec(
                "choice",
                DialogueNodeKind.Choice,
                null,
                "选择",
                choices: sourceChoices);
            sourceChoices[0] = new DialogueChoiceSpec(
                "replacement",
                "替换",
                "end");
            var sourceNodes = new[]
            {
                choiceNode,
                new DialogueNodeSpec(
                    "end",
                    DialogueNodeKind.End,
                    null,
                    null)
            };
            var graph = new DialogueGraphSpec(
                "immutable",
                "不可变测试",
                "choice",
                sourceNodes);
            sourceNodes[0] = sourceNodes[1];
            var session = new DialogueSession("session-copy", graph);
            session.Start();
            var choiceSnapshot = session.Snapshot;

            session.Choose("first");

            Assert.That(graph.Nodes[0].NodeId, Is.EqualTo("choice"));
            Assert.That(graph.Nodes[0].Choices[0].ChoiceId, Is.EqualTo("first"));
            Assert.That(
                choiceSnapshot.Choices,
                Is.Not.AssignableTo<DialogueChoiceSnapshot[]>());
            Assert.That(choiceSnapshot.Choices.Count, Is.EqualTo(1));
            Assert.That(
                choiceSnapshot.Status,
                Is.EqualTo(DialogueSessionStatus.AwaitingChoice));
            Assert.That(
                session.Status,
                Is.EqualTo(DialogueSessionStatus.Completed));
        }

        private static DialogueGraphSpec CreateBranchingGraph()
        {
            var special = new DialogueChoiceSpec(
                "special",
                "特殊回答",
                "result",
                new[]
                {
                    new DialogueConditionSpec(
                        "variable_equals",
                        "story.trusted",
                        "true")
                },
                new[]
                {
                    new DialogueCommandSpec(
                        "set_variable",
                        "story.answer",
                        "special")
                });
            var normal = new DialogueChoiceSpec(
                "normal",
                "普通回答",
                "result",
                commands: new[]
                {
                    new DialogueCommandSpec(
                        "set_variable",
                        "story.answer",
                        "normal")
                });
            return new DialogueGraphSpec(
                "branching",
                "分支测试",
                "intro",
                new[]
                {
                    new DialogueNodeSpec(
                        "intro",
                        DialogueNodeKind.Line,
                        "character.rusk",
                        "先说一句。",
                        "decision"),
                    new DialogueNodeSpec(
                        "decision",
                        DialogueNodeKind.Choice,
                        "character.player",
                        "请选择。",
                        choices: new[] { special, normal }),
                    new DialogueNodeSpec(
                        "result",
                        DialogueNodeKind.Line,
                        "character.rusk",
                        "收到回答。",
                        "end"),
                    new DialogueNodeSpec(
                        "end",
                        DialogueNodeKind.End,
                        null,
                        null)
                });
        }
    }
}
