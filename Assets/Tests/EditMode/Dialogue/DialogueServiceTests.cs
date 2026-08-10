using System;
using System.Collections.Generic;
using NUnit.Framework;
using Train.Architecture.Assets;
using Train.Architecture.Events;
using Train.Dialogue.Application;
using Train.Dialogue.Core;
using Train.Dialogue.Data;
using Train.Dialogue.Events;
using UnityEditor;
using UnityEngine;

namespace Train.Tests.EditMode.Dialogue
{
    /// <summary>
    /// 验证对话应用服务的数据转换、事件顺序、非法输入和资源生命周期。
    /// </summary>
    public sealed class DialogueServiceTests
    {
        private readonly List<UnityEngine.Object> _createdObjects = new();
        private EventBus _events;
        private DialogueService _service;

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
            _events?.Dispose();
            for (var i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_createdObjects[i]);
                }
            }

            _createdObjects.Clear();
        }

        [Test]
        public void FullFlow_PublishesStartedLineChoiceLineAndCompleted()
        {
            CreateService(CreateSettings(CreateDefinition("dialogue_flow")));
            var events = new List<string>();
            using var started =
                _events.Subscribe<DialogueStartedEvent>(
                    _ => events.Add("started"));
            using var line =
                _events.Subscribe<DialogueLineChangedEvent>(
                    _ => events.Add("line"));
            using var choice =
                _events.Subscribe<DialogueChoiceSelectedEvent>(
                    value => events.Add(
                        $"choice:{value.Choice.ChoiceId}"));
            using var completed =
                _events.Subscribe<DialogueCompletedEvent>(
                    value => events.Add(
                        value.WasCompleted ? "completed" : "cancelled"));

            Assert.That(_service.Start("dialogue_flow"), Is.True);
            Assert.That(_service.Continue(), Is.True);
            Assert.That(_service.Choose("accept"), Is.True);
            Assert.That(_service.Continue(), Is.True);

            Assert.That(
                events,
                Is.EqualTo(
                    new[]
                    {
                        "started",
                        "line",
                        "line",
                        "choice:accept",
                        "line",
                        "completed"
                    }));
            Assert.That(_service.IsActive, Is.False);
            Assert.That(
                _service.Current.Status,
                Is.EqualTo(DialogueSessionStatus.Completed));
        }

        [Test]
        public void Cancel_PublishesCompletedEventMarkedAsCancelled()
        {
            CreateService(CreateSettings(CreateDefinition("dialogue_cancel")));
            DialogueCompletedEvent published = default;
            using var subscription =
                _events.Subscribe<DialogueCompletedEvent>(
                    value => published = value);
            _service.Start("dialogue_cancel");

            var cancelled = _service.Cancel();

            Assert.That(cancelled, Is.True);
            Assert.That(published.WasCompleted, Is.False);
            Assert.That(
                published.Snapshot.Status,
                Is.EqualTo(DialogueSessionStatus.Cancelled));
            Assert.That(_service.Cancel(), Is.False);
        }

        [Test]
        public void InvalidUseCases_ReturnFalseWithoutChangingSnapshot()
        {
            CreateService(CreateSettings(CreateDefinition("dialogue_valid")));

            Assert.That(_service.Start("missing"), Is.False);
            Assert.That(_service.Continue(), Is.False);
            Assert.That(_service.Choose(" "), Is.False);
            Assert.That(_service.Cancel(), Is.False);

            Assert.That(_service.Start("dialogue_valid"), Is.True);
            var before = _service.Current;
            Assert.That(_service.Start("dialogue_valid"), Is.False);
            Assert.That(_service.Choose("accept"), Is.False);
            Assert.That(_service.Current.Revision, Is.EqualTo(before.Revision));

            Assert.That(_service.Continue(), Is.True);
            var choiceSnapshot = _service.Current;
            Assert.That(_service.Choose("unknown"), Is.False);
            Assert.That(
                _service.Current.Revision,
                Is.EqualTo(choiceSnapshot.Revision));
        }

        [Test]
        public void CatalogAndConversion_ExposeConfiguredChineseMetadata()
        {
            var definition = CreateDefinition("dialogue_metadata");
            CreateService(CreateSettings(definition));

            Assert.That(_service.Catalog.Count, Is.EqualTo(1));
            Assert.That(
                _service.TryGetDefinition(
                    "dialogue_metadata",
                    out var found),
                Is.True);
            Assert.That(found, Is.SameAs(definition));
            Assert.That(found.Title, Is.EqualTo("中文测试对话"));
            Assert.That(found.ToCoreSpec().Nodes.Count, Is.EqualTo(4));
        }

        [Test]
        public void DuplicateDialogueId_ThrowsAndReleasesLease()
        {
            var first = CreateDefinition("duplicate");
            var second = CreateDefinition("duplicate");
            var settings = CreateSettings(first, second);
            _events = new EventBus();
            var lease = new FakeSettingsLease(settings);

            Assert.Throws<InvalidOperationException>(
                () => new DialogueService(
                    settings,
                    _events,
                    settingsLease: lease));

            Assert.That(lease.IsDisposed, Is.True);
        }

        [Test]
        public void Dispose_ReleasesLeaseAndRejectsFurtherUse()
        {
            var settings = CreateSettings(CreateDefinition("dispose"));
            _events = new EventBus();
            var lease = new FakeSettingsLease(settings);
            _service = new DialogueService(
                settings,
                _events,
                settingsLease: lease);

            _service.Dispose();

            Assert.That(lease.IsDisposed, Is.True);
            Assert.Throws<ObjectDisposedException>(
                () => _ = _service.Current);
            Assert.Throws<ObjectDisposedException>(
                () => _service.Start("dispose"));
        }

        private void CreateService(DialogueSettings settings)
        {
            _events = new EventBus();
            _service = new DialogueService(settings, _events);
        }

        private DialogueDefinition CreateDefinition(string dialogueId)
        {
            var definition =
                ScriptableObject.CreateInstance<DialogueDefinition>();
            _createdObjects.Add(definition);
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("_dialogueId").stringValue = dialogueId;
            serialized.FindProperty("_title").stringValue = "中文测试对话";
            serialized.FindProperty("_entryNodeId").stringValue = "intro";
            var nodes = serialized.FindProperty("_nodes");
            nodes.arraySize = 4;

            ConfigureNode(
                nodes.GetArrayElementAtIndex(0),
                "intro",
                DialogueNodeKind.Line,
                "character.rusk",
                "测试台词。",
                "choice");
            ConfigureNode(
                nodes.GetArrayElementAtIndex(1),
                "choice",
                DialogueNodeKind.Choice,
                "character.player",
                "请选择。",
                null);
            var choices = nodes.GetArrayElementAtIndex(1)
                .FindPropertyRelative("_choices");
            choices.arraySize = 1;
            var choice = choices.GetArrayElementAtIndex(0);
            choice.FindPropertyRelative("_choiceId").stringValue = "accept";
            choice.FindPropertyRelative("_text").stringValue = "接受";
            choice.FindPropertyRelative("_nextNodeId").stringValue = "reply";

            ConfigureNode(
                nodes.GetArrayElementAtIndex(2),
                "reply",
                DialogueNodeKind.Line,
                "character.rusk",
                "已经接受。",
                "end");
            ConfigureNode(
                nodes.GetArrayElementAtIndex(3),
                "end",
                DialogueNodeKind.End,
                string.Empty,
                string.Empty,
                null);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private DialogueSettings CreateSettings(
            params DialogueDefinition[] definitions)
        {
            var settings =
                ScriptableObject.CreateInstance<DialogueSettings>();
            _createdObjects.Add(settings);
            var serialized = new SerializedObject(settings);
            var dialogues = serialized.FindProperty("_dialogues");
            dialogues.arraySize = definitions.Length;
            for (var i = 0; i < definitions.Length; i++)
            {
                dialogues.GetArrayElementAtIndex(i).objectReferenceValue =
                    definitions[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return settings;
        }

        private static void ConfigureNode(
            SerializedProperty property,
            string nodeId,
            DialogueNodeKind kind,
            string speakerId,
            string text,
            string nextNodeId)
        {
            property.FindPropertyRelative("_nodeId").stringValue = nodeId;
            property.FindPropertyRelative("_kind").enumValueIndex =
                (int)kind;
            property.FindPropertyRelative("_speakerId").stringValue =
                speakerId ?? string.Empty;
            property.FindPropertyRelative("_text").stringValue =
                text ?? string.Empty;
            property.FindPropertyRelative("_nextNodeId").stringValue =
                nextNodeId ?? string.Empty;
        }

        /// <summary>
        /// 为测试提供不依赖真实 YooAsset 的对话设置资源租约。
        /// </summary>
        private sealed class FakeSettingsLease :
            IAssetLease<DialogueSettings>
        {
            /// <summary>创建测试租约。</summary>
            public FakeSettingsLease(DialogueSettings asset)
            {
                Asset = asset;
            }

            /// <summary>获取租约是否已经释放。</summary>
            public bool IsDisposed { get; private set; }

            /// <summary>获取测试定位地址。</summary>
            public string Location => "test://dialogue-settings";

            /// <summary>获取测试设置资产。</summary>
            public DialogueSettings Asset { get; }

            /// <summary>获取测试租约是否仍有效。</summary>
            public bool IsValid => !IsDisposed && Asset != null;

            /// <summary>标记测试租约已释放。</summary>
            public void Dispose()
            {
                IsDisposed = true;
            }
        }
    }
}
