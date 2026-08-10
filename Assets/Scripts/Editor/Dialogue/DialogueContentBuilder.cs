#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Train.Dialogue.Core;
using Train.Dialogue.Data;
using UnityEditor;
using UnityEngine;

namespace Train.EditorTools.Dialogue
{
    /// <summary>
    /// 以固定路径和稳定 ID 幂等构建中文示例对话与默认对话设置。
    /// </summary>
    public static class DialogueContentBuilder
    {
        private const string RootFolder = "Assets/Data/Dialogue";
        private const string DefinitionFolder =
            RootFolder + "/Definitions";
        private const string SettingsPath =
            RootFolder + "/DefaultDialogueSettings.asset";

        /// <summary>构建三组可用于后续角色与 UI 接入的中文示例对话。</summary>
        [MenuItem("Tools/Train/Content/Build Dialogue Content")]
        public static void Build()
        {
            EnsureFolder(RootFolder);
            EnsureFolder(DefinitionFolder);

            var seeds = CreateSeeds();
            var definitions = new DialogueDefinition[seeds.Length];
            for (var i = 0; i < seeds.Length; i++)
            {
                definitions[i] = CreateOrUpdateDefinition(seeds[i]);
            }

            var settings =
                AssetDatabase.LoadAssetAtPath<DialogueSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<DialogueSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            var serialized = new SerializedObject(settings);
            var dialogues = serialized.FindProperty("_dialogues");
            dialogues.arraySize = definitions.Length;
            for (var i = 0; i < definitions.Length; i++)
            {
                dialogues.GetArrayElementAtIndex(i).objectReferenceValue =
                    definitions[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"对话内容构建完成：{definitions.Length} 组中文对话，" +
                $"设置资产 '{SettingsPath}'。");
        }

        private static DialogueSeed[] CreateSeeds()
        {
            return new[]
            {
                new DialogueSeed(
                    "dialogue_rusk_first_meet",
                    "与 Rusk 的初次会面",
                    "wake_up",
                    new[]
                    {
                        Line(
                            "wake_up",
                            "character.rusk",
                            "醒了？这里并不安全。能站起来的话，就先把剑握稳。",
                            "first_decision"),
                        Choice(
                            "first_decision",
                            "你准备怎么回答？",
                            new[]
                            {
                                new ChoiceSeed(
                                    "accept_help",
                                    "我来帮忙。",
                                    "accepted",
                                    commands: new[]
                                    {
                                        SetVariable(
                                            "story.rusk_help_accepted",
                                            "true")
                                    }),
                                new ChoiceSeed(
                                    "ask_situation",
                                    "先告诉我这里发生了什么。",
                                    "explanation")
                            }),
                        Line(
                            "explanation",
                            "character.rusk",
                            "失控的以太装置引来了敌人。关掉它之前，街区不会安静。",
                            "second_decision"),
                        Choice(
                            "second_decision",
                            "听完说明后，你决定……",
                            new[]
                            {
                                new ChoiceSeed(
                                    "accept_after_explanation",
                                    "明白了，我来处理。",
                                    "accepted",
                                    commands: new[]
                                    {
                                        SetVariable(
                                            "story.rusk_help_accepted",
                                            "true")
                                    }),
                                new ChoiceSeed(
                                    "leave_for_now",
                                    "我还需要准备一下。",
                                    "leave")
                            }),
                        Line(
                            "accepted",
                            "character.rusk",
                            "很好。别逞强，我会在后方盯着能量读数。",
                            "end"),
                        Line(
                            "leave",
                            "character.rusk",
                            "可以，但别拖太久。敌人可不会等我们准备好。",
                            "end"),
                        End("end")
                    }),
                new DialogueSeed(
                    "dialogue_patrol_report",
                    "巡逻队汇报",
                    "report_start",
                    new[]
                    {
                        Line(
                            "report_start",
                            "character.captain_lyra",
                            "新面孔，报上你在旧街区的行动情况。",
                            "report_choice"),
                        Choice(
                            "report_choice",
                            "选择汇报内容。",
                            new[]
                            {
                                new ChoiceSeed(
                                    "report_rusk_request",
                                    "我接受了 Rusk 的委托。",
                                    "trusted_report",
                                    conditions: new[]
                                    {
                                        VariableEquals(
                                            "story.rusk_help_accepted",
                                            "true")
                                    }),
                                new ChoiceSeed(
                                    "report_patrol",
                                    "我只是路过并检查了周边。",
                                    "normal_report")
                            }),
                        Line(
                            "trusted_report",
                            "character.captain_lyra",
                            "既然 Rusk 信任你，巡逻队也会开放补给点。保持联络。",
                            "end"),
                        Line(
                            "normal_report",
                            "character.captain_lyra",
                            "知道了。未经许可，不要靠近封锁区核心。",
                            "end"),
                        End("end")
                    }),
                new DialogueSeed(
                    "dialogue_training_operator",
                    "训练终端引导",
                    "training_intro",
                    new[]
                    {
                        Line(
                            "training_intro",
                            "character.training_operator",
                            "战斗模拟已经加载。攻击、闪避和拾取模块均可测试。",
                            "ready_choice"),
                        Choice(
                            "ready_choice",
                            "是否立即开始训练？",
                            new[]
                            {
                                new ChoiceSeed(
                                    "ready",
                                    "开始训练。",
                                    "ready_line",
                                    commands: new[]
                                    {
                                        SetVariable(
                                            "tutorial.combat_ready",
                                            "true")
                                    }),
                                new ChoiceSeed(
                                    "not_ready",
                                    "我想先看看装备。",
                                    "not_ready_line")
                            }),
                        Line(
                            "ready_line",
                            "character.training_operator",
                            "确认。模拟敌人将在倒计时结束后生成。",
                            "end"),
                        Line(
                            "not_ready_line",
                            "character.training_operator",
                            "装备终端位于右侧，准备好后可以重新启动本对话。",
                            "end"),
                        End("end")
                    })
            };
        }

        private static DialogueDefinition CreateOrUpdateDefinition(
            DialogueSeed seed)
        {
            var path =
                $"{DefinitionFolder}/{seed.DialogueId}.asset";
            var definition =
                AssetDatabase.LoadAssetAtPath<DialogueDefinition>(path);
            if (definition == null)
            {
                definition =
                    ScriptableObject.CreateInstance<DialogueDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("_dialogueId").stringValue =
                seed.DialogueId;
            serialized.FindProperty("_title").stringValue = seed.Title;
            serialized.FindProperty("_entryNodeId").stringValue =
                seed.EntryNodeId;

            var nodes = serialized.FindProperty("_nodes");
            nodes.arraySize = seed.Nodes.Count;
            for (var i = 0; i < seed.Nodes.Count; i++)
            {
                WriteNode(
                    nodes.GetArrayElementAtIndex(i),
                    seed.Nodes[i]);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void WriteNode(
            SerializedProperty property,
            NodeSeed seed)
        {
            property.FindPropertyRelative("_nodeId").stringValue =
                seed.NodeId;
            property.FindPropertyRelative("_kind").enumValueIndex =
                (int)seed.Kind;
            property.FindPropertyRelative("_speakerId").stringValue =
                seed.SpeakerId ?? string.Empty;
            property.FindPropertyRelative("_text").stringValue =
                seed.Text ?? string.Empty;
            property.FindPropertyRelative("_nextNodeId").stringValue =
                seed.NextNodeId ?? string.Empty;
            WriteConditions(
                property.FindPropertyRelative("_conditions"),
                seed.Conditions);
            WriteCommands(
                property.FindPropertyRelative("_commands"),
                seed.Commands);

            var choices = property.FindPropertyRelative("_choices");
            choices.arraySize = seed.Choices.Count;
            for (var i = 0; i < seed.Choices.Count; i++)
            {
                var choice = choices.GetArrayElementAtIndex(i);
                var choiceSeed = seed.Choices[i];
                choice.FindPropertyRelative("_choiceId").stringValue =
                    choiceSeed.ChoiceId;
                choice.FindPropertyRelative("_text").stringValue =
                    choiceSeed.Text;
                choice.FindPropertyRelative("_nextNodeId").stringValue =
                    choiceSeed.NextNodeId;
                WriteConditions(
                    choice.FindPropertyRelative("_conditions"),
                    choiceSeed.Conditions);
                WriteCommands(
                    choice.FindPropertyRelative("_commands"),
                    choiceSeed.Commands);
            }
        }

        private static void WriteConditions(
            SerializedProperty property,
            IReadOnlyList<ConditionSeed> conditions)
        {
            property.arraySize = conditions.Count;
            for (var i = 0; i < conditions.Count; i++)
            {
                var element = property.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_typeId").stringValue =
                    conditions[i].TypeId;
                element.FindPropertyRelative("_key").stringValue =
                    conditions[i].Key;
                element.FindPropertyRelative("_value").stringValue =
                    conditions[i].Value ?? string.Empty;
                element.FindPropertyRelative("_negate").boolValue =
                    conditions[i].Negate;
            }
        }

        private static void WriteCommands(
            SerializedProperty property,
            IReadOnlyList<CommandSeed> commands)
        {
            property.arraySize = commands.Count;
            for (var i = 0; i < commands.Count; i++)
            {
                var element = property.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_typeId").stringValue =
                    commands[i].TypeId;
                element.FindPropertyRelative("_key").stringValue =
                    commands[i].Key;
                element.FindPropertyRelative("_value").stringValue =
                    commands[i].Value ?? string.Empty;
            }
        }

        private static NodeSeed Line(
            string nodeId,
            string speakerId,
            string text,
            string nextNodeId)
        {
            return new NodeSeed(
                nodeId,
                DialogueNodeKind.Line,
                speakerId,
                text,
                nextNodeId);
        }

        private static NodeSeed Choice(
            string nodeId,
            string text,
            IReadOnlyList<ChoiceSeed> choices)
        {
            return new NodeSeed(
                nodeId,
                DialogueNodeKind.Choice,
                string.Empty,
                text,
                choices: choices);
        }

        private static NodeSeed End(string nodeId)
        {
            return new NodeSeed(
                nodeId,
                DialogueNodeKind.End,
                string.Empty,
                string.Empty);
        }

        private static ConditionSeed VariableEquals(
            string key,
            string value)
        {
            return new ConditionSeed(
                "variable_equals",
                key,
                value,
                false);
        }

        private static CommandSeed SetVariable(
            string key,
            string value)
        {
            return new CommandSeed("set_variable", key, value);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var slash = path.LastIndexOf('/');
            var parent = path.Substring(0, slash);
            var name = path.Substring(slash + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        /// <summary>保存一组待构建对话内容。</summary>
        private readonly struct DialogueSeed
        {
            /// <summary>创建一组对话内容记录。</summary>
            public DialogueSeed(
                string dialogueId,
                string title,
                string entryNodeId,
                IReadOnlyList<NodeSeed> nodes)
            {
                DialogueId = dialogueId;
                Title = title;
                EntryNodeId = entryNodeId;
                Nodes = nodes;
            }

            /// <summary>获取对话稳定标识。</summary>
            public string DialogueId { get; }

            /// <summary>获取对话标题。</summary>
            public string Title { get; }

            /// <summary>获取入口节点标识。</summary>
            public string EntryNodeId { get; }

            /// <summary>获取节点内容。</summary>
            public IReadOnlyList<NodeSeed> Nodes { get; }
        }

        /// <summary>保存一个待写入资产的节点内容。</summary>
        private readonly struct NodeSeed
        {
            /// <summary>创建一个节点内容记录。</summary>
            public NodeSeed(
                string nodeId,
                DialogueNodeKind kind,
                string speakerId,
                string text,
                string nextNodeId = null,
                IReadOnlyList<ChoiceSeed> choices = null,
                IReadOnlyList<ConditionSeed> conditions = null,
                IReadOnlyList<CommandSeed> commands = null)
            {
                NodeId = nodeId;
                Kind = kind;
                SpeakerId = speakerId;
                Text = text;
                NextNodeId = nextNodeId;
                Choices = choices ?? Array.Empty<ChoiceSeed>();
                Conditions = conditions ?? Array.Empty<ConditionSeed>();
                Commands = commands ?? Array.Empty<CommandSeed>();
            }

            /// <summary>获取节点标识。</summary>
            public string NodeId { get; }

            /// <summary>获取节点类型。</summary>
            public DialogueNodeKind Kind { get; }

            /// <summary>获取角色标识。</summary>
            public string SpeakerId { get; }

            /// <summary>获取展示文本。</summary>
            public string Text { get; }

            /// <summary>获取后继节点标识。</summary>
            public string NextNodeId { get; }

            /// <summary>获取选项内容。</summary>
            public IReadOnlyList<ChoiceSeed> Choices { get; }

            /// <summary>获取进入条件。</summary>
            public IReadOnlyList<ConditionSeed> Conditions { get; }

            /// <summary>获取进入命令。</summary>
            public IReadOnlyList<CommandSeed> Commands { get; }
        }

        /// <summary>保存一个待写入资产的选项内容。</summary>
        private readonly struct ChoiceSeed
        {
            /// <summary>创建一个选项内容记录。</summary>
            public ChoiceSeed(
                string choiceId,
                string text,
                string nextNodeId,
                IReadOnlyList<ConditionSeed> conditions = null,
                IReadOnlyList<CommandSeed> commands = null)
            {
                ChoiceId = choiceId;
                Text = text;
                NextNodeId = nextNodeId;
                Conditions =
                    conditions ?? Array.Empty<ConditionSeed>();
                Commands = commands ?? Array.Empty<CommandSeed>();
            }

            /// <summary>获取选项标识。</summary>
            public string ChoiceId { get; }

            /// <summary>获取选项文本。</summary>
            public string Text { get; }

            /// <summary>获取目标节点标识。</summary>
            public string NextNodeId { get; }

            /// <summary>获取可见条件。</summary>
            public IReadOnlyList<ConditionSeed> Conditions { get; }

            /// <summary>获取选中命令。</summary>
            public IReadOnlyList<CommandSeed> Commands { get; }
        }

        /// <summary>保存一条待写入资产的条件内容。</summary>
        private readonly struct ConditionSeed
        {
            /// <summary>创建一条条件内容记录。</summary>
            public ConditionSeed(
                string typeId,
                string key,
                string value,
                bool negate)
            {
                TypeId = typeId;
                Key = key;
                Value = value;
                Negate = negate;
            }

            /// <summary>获取条件类型标识。</summary>
            public string TypeId { get; }

            /// <summary>获取条件参数键。</summary>
            public string Key { get; }

            /// <summary>获取条件参数值。</summary>
            public string Value { get; }

            /// <summary>获取是否反转条件结果。</summary>
            public bool Negate { get; }
        }

        /// <summary>保存一条待写入资产的命令内容。</summary>
        private readonly struct CommandSeed
        {
            /// <summary>创建一条命令内容记录。</summary>
            public CommandSeed(string typeId, string key, string value)
            {
                TypeId = typeId;
                Key = key;
                Value = value;
            }

            /// <summary>获取命令类型标识。</summary>
            public string TypeId { get; }

            /// <summary>获取命令参数键。</summary>
            public string Key { get; }

            /// <summary>获取命令参数值。</summary>
            public string Value { get; }
        }
    }
}
#endif
