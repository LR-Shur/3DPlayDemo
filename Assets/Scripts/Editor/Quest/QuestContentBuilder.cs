#if UNITY_EDITOR
using System;
using Train.Quest.Core;
using Train.Quest.Data;
using UnityEditor;
using UnityEngine;

namespace Train.EditorTools.Quest
{
    /// <summary>
    /// 以稳定路径幂等生成训练关卡使用的中文任务定义和任务系统设置。
    /// </summary>
    public static class QuestContentBuilder
    {
        private const string RootFolder = "Assets/Data/Quests";
        private const string DefinitionFolder = RootFolder + "/Definitions";
        private const string SettingsPath =
            RootFolder + "/DefaultQuestSettings.asset";

        private static readonly QuestSeed[] Seeds =
        {
            new(
                "quest.first_patrol",
                "初次巡防",
                "清理训练城区中的模拟敌人，熟悉基础攻击、闪避与追击节奏。",
                true,
                10,
                new[]
                {
                    new ObjectiveSeed(
                        "defeat_training_knights",
                        QuestFactType.EnemyDefeated,
                        "kaykit_knight",
                        "击败训练骑士",
                        3)
                },
                new[]
                {
                    new RewardSeed("city_token", 30)
                }),
            new(
                "quest.field_salvage",
                "场地回收",
                "留意城区中的发光拾取标记，回收散落的训练数据。",
                true,
                20,
                new[]
                {
                    new ObjectiveSeed(
                        "collect_training_chips",
                        QuestFactType.ItemAcquired,
                        "training_chip",
                        "拾取训练芯片",
                        5)
                },
                new[]
                {
                    new RewardSeed("healing_canister", 2)
                }),
            new(
                "quest.combat_clearance",
                "战区许可",
                "完成一轮完整战斗流程，证明你能够独立处理训练区域。",
                true,
                30,
                new[]
                {
                    new ObjectiveSeed(
                        "clear_combat_level",
                        QuestFactType.LevelCompleted,
                        "level.combat.001",
                        "完成「霓虹训练场」",
                        1)
                },
                new[]
                {
                    new RewardSeed("upgrade_module", 1)
                }),
            new(
                "quest.storm_resonance",
                "雷鸣共振",
                "找到带有雷电响应的装备，让战斗模块完成一次元素校准。",
                true,
                40,
                new[]
                {
                    new ObjectiveSeed(
                        "acquire_thunder_ring",
                        QuestFactType.ItemAcquired,
                        "thunder_ring",
                        "获得「雷鸣指环」",
                        1)
                },
                new[]
                {
                    new RewardSeed("city_token", 80)
                }),
            new(
                "quest.full_drill",
                "进阶综合演练",
                "同时完成歼敌与通关要求，检验一整套战斗流程。",
                false,
                50,
                new[]
                {
                    new ObjectiveSeed(
                        "defeat_squad",
                        QuestFactType.EnemyDefeated,
                        "kaykit_knight",
                        "击败训练骑士",
                        3),
                    new ObjectiveSeed(
                        "complete_drill",
                        QuestFactType.LevelCompleted,
                        "level.combat.001",
                        "完成「霓虹训练场」",
                        1)
                },
                new[]
                {
                    new RewardSeed("training_chip", 6),
                    new RewardSeed("city_token", 120)
                })
        };

        /// <summary>生成或更新全部任务内容资产。</summary>
        [MenuItem("Tools/Train/Content/Build Quest Content")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("请先停止运行游戏，再构建任务内容。");
                return;
            }

            EnsureFolder(RootFolder);
            EnsureFolder(DefinitionFolder);

            var definitions = new QuestDefinition[Seeds.Length];
            for (var i = 0; i < Seeds.Length; i++)
            {
                definitions[i] = CreateOrUpdateQuest(Seeds[i]);
            }

            var settings =
                AssetDatabase.LoadAssetAtPath<QuestSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<QuestSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            var settingsObject = new SerializedObject(settings);
            var questList = settingsObject.FindProperty("_quests");
            questList.arraySize = definitions.Length;
            for (var i = 0; i < definitions.Length; i++)
            {
                questList.GetArrayElementAtIndex(i).objectReferenceValue =
                    definitions[i];
            }

            settingsObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"任务内容构建完成：{definitions.Length} 项中文任务，" +
                $"设置资源 '{SettingsPath}'。");
        }

        private static QuestDefinition CreateOrUpdateQuest(QuestSeed seed)
        {
            var path = $"{DefinitionFolder}/{seed.QuestId}.asset";
            var definition =
                AssetDatabase.LoadAssetAtPath<QuestDefinition>(path);
            if (definition == null)
            {
                definition =
                    ScriptableObject.CreateInstance<QuestDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("_questId").stringValue = seed.QuestId;
            serialized.FindProperty("_title").stringValue = seed.Title;
            serialized.FindProperty("_description").stringValue =
                seed.Description;
            serialized.FindProperty("_autoAccept").boolValue =
                seed.AutoAccept;
            serialized.FindProperty("_sortOrder").intValue = seed.SortOrder;

            var objectives = serialized.FindProperty("_objectives");
            objectives.arraySize = seed.Objectives.Length;
            for (var i = 0; i < seed.Objectives.Length; i++)
            {
                var source = seed.Objectives[i];
                var target = objectives.GetArrayElementAtIndex(i);
                target.FindPropertyRelative("_objectiveId").stringValue =
                    source.ObjectiveId;
                target.FindPropertyRelative("_factType").enumValueIndex =
                    (int)source.FactType;
                target.FindPropertyRelative("_targetId").stringValue =
                    source.TargetId;
                target.FindPropertyRelative("_displayText").stringValue =
                    source.DisplayText;
                target.FindPropertyRelative("_requiredAmount").intValue =
                    source.RequiredAmount;
            }

            var rewards = serialized.FindProperty("_rewards");
            rewards.arraySize = seed.Rewards.Length;
            for (var i = 0; i < seed.Rewards.Length; i++)
            {
                var source = seed.Rewards[i];
                var target = rewards.GetArrayElementAtIndex(i);
                target.FindPropertyRelative("_itemId").stringValue =
                    source.ItemId;
                target.FindPropertyRelative("_amount").intValue =
                    source.Amount;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
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

        /// <summary>保存构建器写入一项任务资产的固定内容。</summary>
        private sealed class QuestSeed
        {
            /// <summary>创建一项任务资产种子。</summary>
            public QuestSeed(
                string questId,
                string title,
                string description,
                bool autoAccept,
                int sortOrder,
                ObjectiveSeed[] objectives,
                RewardSeed[] rewards)
            {
                QuestId = questId;
                Title = title;
                Description = description;
                AutoAccept = autoAccept;
                SortOrder = sortOrder;
                Objectives = objectives;
                Rewards = rewards;
            }

            /// <summary>获取任务稳定标识。</summary>
            public string QuestId { get; }

            /// <summary>获取任务标题。</summary>
            public string Title { get; }

            /// <summary>获取任务说明。</summary>
            public string Description { get; }

            /// <summary>获取是否自动接取。</summary>
            public bool AutoAccept { get; }

            /// <summary>获取任务排序值。</summary>
            public int SortOrder { get; }

            /// <summary>获取目标种子。</summary>
            public ObjectiveSeed[] Objectives { get; }

            /// <summary>获取奖励种子。</summary>
            public RewardSeed[] Rewards { get; }
        }

        /// <summary>保存一条任务目标的固定构建数据。</summary>
        private readonly struct ObjectiveSeed
        {
            /// <summary>创建一条目标资产种子。</summary>
            public ObjectiveSeed(
                string objectiveId,
                QuestFactType factType,
                string targetId,
                string displayText,
                int requiredAmount)
            {
                ObjectiveId = objectiveId;
                FactType = factType;
                TargetId = targetId;
                DisplayText = displayText;
                RequiredAmount = requiredAmount;
            }

            /// <summary>获取目标稳定标识。</summary>
            public string ObjectiveId { get; }

            /// <summary>获取事实类型。</summary>
            public QuestFactType FactType { get; }

            /// <summary>获取事实目标标识。</summary>
            public string TargetId { get; }

            /// <summary>获取目标展示文本。</summary>
            public string DisplayText { get; }

            /// <summary>获取目标所需数量。</summary>
            public int RequiredAmount { get; }
        }

        /// <summary>保存一条任务奖励的固定构建数据。</summary>
        private readonly struct RewardSeed
        {
            /// <summary>创建一条奖励资产种子。</summary>
            public RewardSeed(string itemId, int amount)
            {
                ItemId = itemId;
                Amount = amount;
            }

            /// <summary>获取奖励物品标识。</summary>
            public string ItemId { get; }

            /// <summary>获取奖励数量。</summary>
            public int Amount { get; }
        }
    }
}
#endif
