using System;
using System.Collections.Generic;
using NUnit.Framework;
using Train.Architecture.Assets;
using Train.Architecture.Events;
using Train.GameFlow.Application.Events;
using Train.Inventory.Application;
using Train.Inventory.Data;
using Train.Inventory.Events;
using Train.Quest.Application;
using Train.Quest.Core;
using Train.Quest.Data;
using Train.Quest.Events;
using UnityEditor;
using UnityEngine;

namespace Train.Tests.EditMode.Quest
{
    /// <summary>
    /// 验证任务应用服务的事件适配、状态用例、奖励事务和资源生命周期。
    /// </summary>
    public sealed class QuestServiceTests
    {
        private readonly List<UnityEngine.Object> _createdObjects = new();
        private readonly List<IDisposable> _createdServices = new();
        private EventBus _events;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = _createdServices.Count - 1; i >= 0; i--)
            {
                _createdServices[i]?.Dispose();
            }

            _createdServices.Clear();
            _events?.Dispose();
            _events = null;

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
        public void Constructor_BuildsSortedCatalogAutoAcceptsAndPublishesReady()
        {
            var inventory = CreateInventory(
                4,
                new[] { new ItemInput("city_token", 99) });
            var later = CreateQuest(
                "quest.later",
                false,
                20,
                new[]
                {
                    new ObjectiveInput(
                        "later_target",
                        QuestFactType.LevelCompleted,
                        "level.later",
                        1)
                });
            var first = CreateQuest(
                "quest.first",
                true,
                10,
                new[]
                {
                    new ObjectiveInput(
                        "first_target",
                        QuestFactType.EnemyDefeated,
                        "enemy.first",
                        2)
                });
            var settings = CreateQuestSettings(later, first);
            QuestSystemReadyEvent? ready = null;
            _events.Subscribe<QuestSystemReadyEvent>(
                message => ready = message);

            var service = Register(
                new QuestService(settings, inventory, _events));

            Assert.That(service.Catalog, Has.Count.EqualTo(2));
            Assert.That(
                service.Catalog[0].QuestId,
                Is.EqualTo("quest.first"));
            Assert.That(
                service.Snapshots[0].Status,
                Is.EqualTo(QuestStatus.Active));
            Assert.That(
                service.Snapshots[1].Status,
                Is.EqualTo(QuestStatus.Inactive));
            Assert.That(
                service.TrackedQuestId,
                Is.EqualTo("quest.first"));
            Assert.That(ready.HasValue, Is.True);
            Assert.That(ready.Value.QuestCount, Is.EqualTo(2));
            Assert.That(ready.Value.AcceptedQuestCount, Is.EqualTo(1));
        }

        [Test]
        public void GameEvents_AdaptToExactFactsAndCompleteMatchingQuests()
        {
            var inventory = CreateInventory(
                4,
                new[] { new ItemInput("training_chip", 99) });
            var enemyQuest = CreateQuest(
                "quest.enemy",
                true,
                10,
                new[]
                {
                    new ObjectiveInput(
                        "defeat",
                        QuestFactType.EnemyDefeated,
                        "kaykit_knight",
                        2)
                });
            var itemQuest = CreateQuest(
                "quest.item",
                true,
                20,
                new[]
                {
                    new ObjectiveInput(
                        "collect",
                        QuestFactType.ItemAcquired,
                        "training_chip",
                        3)
                });
            var levelQuest = CreateQuest(
                "quest.level",
                true,
                30,
                new[]
                {
                    new ObjectiveInput(
                        "clear",
                        QuestFactType.LevelCompleted,
                        "level.combat.001",
                        1)
                });
            var service = Register(
                new QuestService(
                    CreateQuestSettings(
                        enemyQuest,
                        itemQuest,
                        levelQuest),
                    inventory,
                    _events));

            _events.Publish(
                new EnemyDefeatedEvent(
                    "level.combat.001",
                    "kaykit_knight_elite",
                    "wrong",
                    "player"));
            _events.Publish(
                new EnemyDefeatedEvent(
                    "level.combat.001",
                    "kaykit_knight",
                    "enemy_1",
                    "player"));
            _events.Publish(
                new EnemyDefeatedEvent(
                    "level.combat.001",
                    "kaykit_knight",
                    "enemy_2",
                    "player"));
            _events.Publish(
                new ItemAcquiredEvent(
                    "training_chip",
                    3,
                    3,
                    1));
            _events.Publish(
                new LevelCompletedEvent(
                    "level.combat.001",
                    12f));

            AssertQuestStatus(
                service,
                "quest.enemy",
                QuestStatus.Completed,
                2);
            AssertQuestStatus(
                service,
                "quest.item",
                QuestStatus.Completed,
                3);
            AssertQuestStatus(
                service,
                "quest.level",
                QuestStatus.Completed,
                1);
        }

        [Test]
        public void ManualQuest_CanBeAcceptedTrackedAndCleared()
        {
            var inventory = CreateInventory(
                2,
                new[] { new ItemInput("city_token", 99) });
            var quest = CreateQuest(
                "quest.manual",
                false,
                10,
                new[]
                {
                    new ObjectiveInput(
                        "talk",
                        QuestFactType.DialogueCompleted,
                        "npc.operator",
                        1)
                });
            var service = Register(
                new QuestService(
                    CreateQuestSettings(quest),
                    inventory,
                    _events));
            var trackingEvents = new List<TrackedQuestChangedEvent>();
            _events.Subscribe<TrackedQuestChangedEvent>(
                trackingEvents.Add);

            Assert.That(service.TrackedQuestId, Is.Null);
            Assert.That(service.AcceptQuest("missing"), Is.False);
            Assert.That(service.AcceptQuest("quest.manual"), Is.True);
            Assert.That(service.AcceptQuest("quest.manual"), Is.False);
            Assert.That(service.TrackedQuestId, Is.EqualTo("quest.manual"));

            service.ClearTrackedQuest();
            Assert.That(service.TrackedQuestId, Is.Null);
            Assert.That(service.TrackQuest("quest.manual"), Is.True);
            Assert.That(trackingEvents, Has.Count.EqualTo(3));
        }

        [Test]
        public void ApplyFact_SupportsFutureDialogueAdapterWithoutGameObject()
        {
            var inventory = CreateInventory(
                2,
                new[] { new ItemInput("city_token", 99) });
            var quest = CreateQuest(
                "quest.dialogue",
                true,
                10,
                new[]
                {
                    new ObjectiveInput(
                        "talk",
                        QuestFactType.DialogueCompleted,
                        "npc.operator",
                        1)
                });
            var service = Register(
                new QuestService(
                    CreateQuestSettings(quest),
                    inventory,
                    _events));

            Assert.That(
                service.ApplyFact(
                    new QuestFact(
                        QuestFactType.DialogueCompleted,
                        "npc.wrong")),
                Is.False);
            Assert.That(
                service.ApplyFact(
                    new QuestFact(
                        QuestFactType.DialogueCompleted,
                        "npc.operator")),
                Is.True);
            AssertQuestStatus(
                service,
                "quest.dialogue",
                QuestStatus.Completed,
                1);
        }

        [Test]
        public void ClaimRewards_GrantsAllRewardsThenClaimsQuest()
        {
            var inventory = CreateInventory(
                4,
                new[]
                {
                    new ItemInput("city_token", 99),
                    new ItemInput("training_chip", 99)
                });
            var quest = CreateQuest(
                "quest.reward",
                true,
                10,
                new[]
                {
                    new ObjectiveInput(
                        "defeat",
                        QuestFactType.EnemyDefeated,
                        "enemy",
                        1)
                },
                new[]
                {
                    new RewardInput("city_token", 30),
                    new RewardInput("training_chip", 2)
                });
            var service = Register(
                new QuestService(
                    CreateQuestSettings(quest),
                    inventory,
                    _events));
            QuestRewardsClaimedEvent claimed = null;
            _events.Subscribe<QuestRewardsClaimedEvent>(
                message => claimed = message);
            service.ApplyFact(
                new QuestFact(
                    QuestFactType.EnemyDefeated,
                    "enemy"));

            var result = service.ClaimRewards("quest.reward");

            Assert.That(result.Succeeded, Is.True);
            Assert.That(
                inventory.GetTotalQuantity("city_token"),
                Is.EqualTo(30));
            Assert.That(
                inventory.GetTotalQuantity("training_chip"),
                Is.EqualTo(2));
            AssertQuestStatus(
                service,
                "quest.reward",
                QuestStatus.Claimed,
                1);
            Assert.That(claimed, Is.Not.Null);
            Assert.That(claimed.Rewards, Has.Count.EqualTo(2));
        }

        [Test]
        public void ClaimRewards_WhenWholeBatchCannotFit_ChangesNothing()
        {
            var inventory = CreateInventory(
                1,
                new[]
                {
                    new ItemInput("reward_a", 1),
                    new ItemInput("reward_b", 1)
                });
            var quest = CreateQuest(
                "quest.atomic",
                true,
                10,
                new[]
                {
                    new ObjectiveInput(
                        "finish",
                        QuestFactType.LevelCompleted,
                        "level",
                        1)
                },
                new[]
                {
                    new RewardInput("reward_a", 1),
                    new RewardInput("reward_b", 1)
                });
            var service = Register(
                new QuestService(
                    CreateQuestSettings(quest),
                    inventory,
                    _events));
            QuestRewardClaimFailedEvent? failed = null;
            _events.Subscribe<QuestRewardClaimFailedEvent>(
                message => failed = message);
            service.ApplyFact(
                new QuestFact(
                    QuestFactType.LevelCompleted,
                    "level"));
            var revisionBefore = inventory.Snapshot.Revision;

            var result = service.ClaimRewards("quest.atomic");

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    QuestRewardClaimFailureReason.InventoryFull));
            Assert.That(inventory.Snapshot.Revision, Is.EqualTo(revisionBefore));
            Assert.That(inventory.Snapshot.OccupiedSlotCount, Is.Zero);
            AssertQuestStatus(
                service,
                "quest.atomic",
                QuestStatus.Completed,
                1);
            Assert.That(failed.HasValue, Is.True);
        }

        [Test]
        public void ClaimRewards_AggregatesDuplicateRewardItemsForPreflight()
        {
            var inventory = CreateInventory(
                2,
                new[] { new ItemInput("city_token", 5) });
            var quest = CreateQuest(
                "quest.aggregate",
                true,
                10,
                new[]
                {
                    new ObjectiveInput(
                        "finish",
                        QuestFactType.LevelCompleted,
                        "level",
                        1)
                },
                new[]
                {
                    new RewardInput("city_token", 3),
                    new RewardInput("city_token", 4)
                });
            var service = Register(
                new QuestService(
                    CreateQuestSettings(quest),
                    inventory,
                    _events));
            service.ApplyFact(
                new QuestFact(
                    QuestFactType.LevelCompleted,
                    "level"));

            var result = service.ClaimRewards("quest.aggregate");

            Assert.That(result.Succeeded, Is.True);
            Assert.That(
                inventory.GetTotalQuantity("city_token"),
                Is.EqualTo(7));
        }

        [Test]
        public void ClaimRewards_WithUnknownReward_ReportsInvalidReward()
        {
            var inventory = CreateInventory(
                2,
                new[] { new ItemInput("known", 99) });
            var quest = CreateQuest(
                "quest.invalid_reward",
                true,
                10,
                new[]
                {
                    new ObjectiveInput(
                        "finish",
                        QuestFactType.LevelCompleted,
                        "level",
                        1)
                },
                new[] { new RewardInput("missing", 1) });
            var service = Register(
                new QuestService(
                    CreateQuestSettings(quest),
                    inventory,
                    _events));
            service.ApplyFact(
                new QuestFact(
                    QuestFactType.LevelCompleted,
                    "level"));

            var result = service.ClaimRewards("quest.invalid_reward");

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    QuestRewardClaimFailureReason.InvalidReward));
            AssertQuestStatus(
                service,
                "quest.invalid_reward",
                QuestStatus.Completed,
                1);
        }

        [Test]
        public void ClaimingTrackedQuest_SelectsNextActiveQuest()
        {
            var inventory = CreateInventory(
                2,
                new[] { new ItemInput("city_token", 99) });
            var first = CreateQuest(
                "quest.first",
                true,
                10,
                new[]
                {
                    new ObjectiveInput(
                        "first",
                        QuestFactType.LevelCompleted,
                        "level.first",
                        1)
                });
            var second = CreateQuest(
                "quest.second",
                true,
                20,
                new[]
                {
                    new ObjectiveInput(
                        "second",
                        QuestFactType.LevelCompleted,
                        "level.second",
                        1)
                });
            var service = Register(
                new QuestService(
                    CreateQuestSettings(first, second),
                    inventory,
                    _events));
            service.ApplyFact(
                new QuestFact(
                    QuestFactType.LevelCompleted,
                    "level.first"));

            var result = service.ClaimRewards("quest.first");

            Assert.That(result.Succeeded, Is.True);
            Assert.That(
                service.TrackedQuestId,
                Is.EqualTo("quest.second"));
        }

        [Test]
        public void InvalidDuplicateCatalog_PublishesFailureAndReleasesLease()
        {
            var inventory = CreateInventory(
                2,
                new[] { new ItemInput("city_token", 99) });
            var duplicateA = CreateQuest(
                "quest.duplicate",
                false,
                10,
                new[]
                {
                    new ObjectiveInput(
                        "a",
                        QuestFactType.LevelCompleted,
                        "a",
                        1)
                });
            var duplicateB = CreateQuest(
                "quest.duplicate",
                false,
                20,
                new[]
                {
                    new ObjectiveInput(
                        "b",
                        QuestFactType.LevelCompleted,
                        "b",
                        1)
                });
            var settings =
                CreateQuestSettings(duplicateA, duplicateB);
            var lease = new FakeAssetLease<QuestSettings>(settings);
            QuestSystemFailedEvent? failed = null;
            _events.Subscribe<QuestSystemFailedEvent>(
                message => failed = message);

            Assert.Throws<InvalidOperationException>(
                () => new QuestService(
                    settings,
                    inventory,
                    _events,
                    lease));

            Assert.That(lease.DisposeCount, Is.EqualTo(1));
            Assert.That(failed.HasValue, Is.True);
            Assert.That(
                failed.Value.Reason,
                Does.Contain("重复任务标识"));
        }

        [Test]
        public void Dispose_UnsubscribesFactsReleasesLeaseAndGuardsApi()
        {
            var inventory = CreateInventory(
                2,
                new[] { new ItemInput("city_token", 99) });
            var quest = CreateQuest(
                "quest.dispose",
                true,
                10,
                new[]
                {
                    new ObjectiveInput(
                        "defeat",
                        QuestFactType.EnemyDefeated,
                        "enemy",
                        1)
                });
            var settings = CreateQuestSettings(quest);
            var lease = new FakeAssetLease<QuestSettings>(settings);
            var changedCount = 0;
            _events.Subscribe<QuestChangedEvent>(_ => changedCount++);
            var service = new QuestService(
                settings,
                inventory,
                _events,
                lease);
            var countBeforeDispose = changedCount;

            service.Dispose();
            _events.Publish(
                new EnemyDefeatedEvent(
                    "level",
                    "enemy",
                    "instance",
                    "player"));

            Assert.That(changedCount, Is.EqualTo(countBeforeDispose));
            Assert.That(lease.DisposeCount, Is.EqualTo(1));
            Assert.Throws<ObjectDisposedException>(
                () => _ = service.Snapshots);
        }

        private InventoryService CreateInventory(
            int capacity,
            ItemInput[] items,
            StartingInput[] startingItems = null)
        {
            var definitions = new ItemDefinition[items.Length];
            for (var i = 0; i < items.Length; i++)
            {
                var definition =
                    ScriptableObject.CreateInstance<ItemDefinition>();
                _createdObjects.Add(definition);
                var serialized = new SerializedObject(definition);
                serialized.FindProperty("_itemId").stringValue =
                    items[i].ItemId;
                serialized.FindProperty("_displayName").stringValue =
                    items[i].ItemId;
                serialized.FindProperty("_maxStack").intValue =
                    items[i].MaxStack;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                definitions[i] = definition;
            }

            var settings =
                ScriptableObject.CreateInstance<InventorySettings>();
            _createdObjects.Add(settings);
            var settingsObject = new SerializedObject(settings);
            settingsObject.FindProperty("_capacity").intValue = capacity;
            var itemList = settingsObject.FindProperty("_items");
            itemList.arraySize = definitions.Length;
            for (var i = 0; i < definitions.Length; i++)
            {
                itemList.GetArrayElementAtIndex(i).objectReferenceValue =
                    definitions[i];
            }

            var starts = startingItems ?? Array.Empty<StartingInput>();
            var startingList =
                settingsObject.FindProperty("_startingItems");
            startingList.arraySize = starts.Length;
            for (var i = 0; i < starts.Length; i++)
            {
                var entry = startingList.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_itemId").stringValue =
                    starts[i].ItemId;
                entry.FindPropertyRelative("_count").intValue =
                    starts[i].Amount;
            }

            settingsObject.ApplyModifiedPropertiesWithoutUndo();
            return Register(
                new InventoryService(settings, _events));
        }

        private QuestDefinition CreateQuest(
            string questId,
            bool autoAccept,
            int sortOrder,
            ObjectiveInput[] objectives,
            RewardInput[] rewards = null)
        {
            var definition =
                ScriptableObject.CreateInstance<QuestDefinition>();
            _createdObjects.Add(definition);
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("_questId").stringValue = questId;
            serialized.FindProperty("_title").stringValue = questId;
            serialized.FindProperty("_description").stringValue = questId;
            serialized.FindProperty("_autoAccept").boolValue = autoAccept;
            serialized.FindProperty("_sortOrder").intValue = sortOrder;

            var objectiveList = serialized.FindProperty("_objectives");
            objectiveList.arraySize = objectives.Length;
            for (var i = 0; i < objectives.Length; i++)
            {
                var entry = objectiveList.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_objectiveId").stringValue =
                    objectives[i].ObjectiveId;
                entry.FindPropertyRelative("_factType").enumValueIndex =
                    (int)objectives[i].FactType;
                entry.FindPropertyRelative("_targetId").stringValue =
                    objectives[i].TargetId;
                entry.FindPropertyRelative("_displayText").stringValue =
                    objectives[i].ObjectiveId;
                entry.FindPropertyRelative("_requiredAmount").intValue =
                    objectives[i].RequiredAmount;
            }

            var rewardValues = rewards ?? Array.Empty<RewardInput>();
            var rewardList = serialized.FindProperty("_rewards");
            rewardList.arraySize = rewardValues.Length;
            for (var i = 0; i < rewardValues.Length; i++)
            {
                var entry = rewardList.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_itemId").stringValue =
                    rewardValues[i].ItemId;
                entry.FindPropertyRelative("_amount").intValue =
                    rewardValues[i].Amount;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private QuestSettings CreateQuestSettings(
            params QuestDefinition[] quests)
        {
            var settings =
                ScriptableObject.CreateInstance<QuestSettings>();
            _createdObjects.Add(settings);
            var serialized = new SerializedObject(settings);
            var list = serialized.FindProperty("_quests");
            list.arraySize = quests.Length;
            for (var i = 0; i < quests.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue =
                    quests[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return settings;
        }

        private T Register<T>(T disposable)
            where T : IDisposable
        {
            _createdServices.Add(disposable);
            return disposable;
        }

        private static void AssertQuestStatus(
            IQuestService service,
            string questId,
            QuestStatus expectedStatus,
            int expectedProgress)
        {
            Assert.That(
                service.TryGetSnapshot(questId, out var snapshot),
                Is.True);
            Assert.That(snapshot.Status, Is.EqualTo(expectedStatus));
            Assert.That(
                snapshot.Objectives[0].CurrentAmount,
                Is.EqualTo(expectedProgress));
        }

        /// <summary>保存测试任务目标的构造参数。</summary>
        private readonly struct ObjectiveInput
        {
            /// <summary>创建测试任务目标参数。</summary>
            public ObjectiveInput(
                string objectiveId,
                QuestFactType factType,
                string targetId,
                int requiredAmount)
            {
                ObjectiveId = objectiveId;
                FactType = factType;
                TargetId = targetId;
                RequiredAmount = requiredAmount;
            }

            /// <summary>获取目标标识。</summary>
            public string ObjectiveId { get; }

            /// <summary>获取事实类型。</summary>
            public QuestFactType FactType { get; }

            /// <summary>获取事实目标标识。</summary>
            public string TargetId { get; }

            /// <summary>获取目标所需数量。</summary>
            public int RequiredAmount { get; }
        }

        /// <summary>保存测试任务奖励的构造参数。</summary>
        private readonly struct RewardInput
        {
            /// <summary>创建测试奖励参数。</summary>
            public RewardInput(string itemId, int amount)
            {
                ItemId = itemId;
                Amount = amount;
            }

            /// <summary>获取奖励物品标识。</summary>
            public string ItemId { get; }

            /// <summary>获取奖励数量。</summary>
            public int Amount { get; }
        }

        /// <summary>保存测试背包物品的构造参数。</summary>
        private readonly struct ItemInput
        {
            /// <summary>创建测试背包物品参数。</summary>
            public ItemInput(string itemId, int maxStack)
            {
                ItemId = itemId;
                MaxStack = maxStack;
            }

            /// <summary>获取物品标识。</summary>
            public string ItemId { get; }

            /// <summary>获取单槽堆叠上限。</summary>
            public int MaxStack { get; }
        }

        /// <summary>保存测试初始背包物品的构造参数。</summary>
        private readonly struct StartingInput
        {
            /// <summary>创建测试初始背包参数。</summary>
            public StartingInput(string itemId, int amount)
            {
                ItemId = itemId;
                Amount = amount;
            }

            /// <summary>获取初始物品标识。</summary>
            public string ItemId { get; }

            /// <summary>获取初始物品数量。</summary>
            public int Amount { get; }
        }

        /// <summary>记录任务服务是否正确释放测试资源租约。</summary>
        private sealed class FakeAssetLease<TAsset> : IAssetLease<TAsset>
            where TAsset : UnityEngine.Object
        {
            /// <summary>创建一个指向指定测试资源的租约。</summary>
            public FakeAssetLease(TAsset asset)
            {
                Asset = asset;
            }

            /// <inheritdoc />
            public string Location => "test";

            /// <inheritdoc />
            public TAsset Asset { get; }

            /// <inheritdoc />
            public bool IsValid => DisposeCount == 0 && Asset != null;

            /// <summary>获取租约被释放的次数。</summary>
            public int DisposeCount { get; private set; }

            /// <summary>记录一次释放，便于断言所有权语义。</summary>
            public void Dispose()
            {
                DisposeCount++;
            }
        }
    }
}
