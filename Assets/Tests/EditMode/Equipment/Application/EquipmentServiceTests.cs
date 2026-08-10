using System;
using System.Collections.Generic;
using NUnit.Framework;
using Train.Architecture.Assets;
using Train.Architecture.Events;
using Train.Equipment.Application;
using Train.Equipment.Core;
using Train.Equipment.Data;
using Train.Equipment.Events;
using UnityEditor;
using UnityEngine;

namespace Train.Tests.EditMode.Equipment.Application
{
    /// <summary>
    /// 验证装备应用服务的数据转换、换装用例、应用事件和资源租约生命周期。
    /// </summary>
    public sealed class EquipmentServiceTests
    {
        private const float Tolerance = 0.0001f;

        private readonly List<UnityEngine.Object> _createdObjects = new();
        private EventBus _events;
        private EquipmentService _service;

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
        public void Constructor_BuildsCatalogAndSeedsWithoutPublishingEvent()
        {
            var weapon = CreateItem(
                "starter_weapon",
                EquipmentItemCategory.Weapon,
                flatAttack: 20f);
            var settings = CreateSettings(
                new[] { weapon },
                starting: new[]
                {
                    new StartingEntry(
                        weapon,
                        EquipmentSlot.Weapon)
                });
            _events = new EventBus();
            var eventCount = 0;
            using var subscription =
                _events.Subscribe<EquipmentChangedEvent>(_ => eventCount++);

            _service = new EquipmentService(settings, _events);

            Assert.That(_service.Catalog.Count, Is.EqualTo(1));
            Assert.That(
                _service.Snapshot.GetEquippedItem(EquipmentSlot.Weapon)
                    .ItemId,
                Is.EqualTo("starter_weapon"));
            Assert.That(
                _service.Snapshot.GetFinalStat(StatType.Attack),
                Is.EqualTo(120f).Within(Tolerance));
            Assert.That(_service.Snapshot.Revision, Is.EqualTo(1));
            Assert.That(eventCount, Is.Zero);
        }

        [Test]
        public void TryGetDefinition_ReturnsRegisteredCatalogEntry()
        {
            var helmet = CreateItem(
                "helmet_a",
                EquipmentItemCategory.Helmet);
            CreateService(CreateSettings(new[] { helmet }));

            var found = _service.TryGetDefinition(
                "helmet_a",
                out var result);

            Assert.That(found, Is.True);
            Assert.That(result, Is.SameAs(helmet));
            Assert.That(
                _service.TryGetDefinition("missing", out _),
                Is.False);
            Assert.That(
                _service.TryGetDefinition(" ", out _),
                Is.False);
        }

        [Test]
        public void Equip_RegisteredFixedEquipmentPublishesApplicationEvent()
        {
            var weapon = CreateItem(
                "weapon_a",
                EquipmentItemCategory.Weapon,
                flatAttack: 25f);
            CreateService(CreateSettings(new[] { weapon }));
            EquipmentChangedEvent published = default;
            using var subscription =
                _events.Subscribe<EquipmentChangedEvent>(
                    value => published = value);

            var changed = _service.Equip(
                "weapon_a",
                EquipmentSlot.Weapon);

            Assert.That(changed, Is.True);
            Assert.That(
                _service.Snapshot.GetFinalStat(StatType.Attack),
                Is.EqualTo(125f).Within(Tolerance));
            Assert.That(
                published.Kind,
                Is.EqualTo(EquipmentChangeKind.Equipped));
            Assert.That(
                published.TargetSlot,
                Is.EqualTo(EquipmentSlot.Weapon));
            Assert.That(published.SourceSlot, Is.Null);
            Assert.That(published.PreviousItemId, Is.Null);
            Assert.That(published.CurrentItemId, Is.EqualTo("weapon_a"));
            Assert.That(
                published.Revision,
                Is.EqualTo(_service.Snapshot.Revision));
        }

        [Test]
        public void Equip_UnknownItemIsAtomicAndSilent()
        {
            var weapon = CreateItem(
                "weapon_a",
                EquipmentItemCategory.Weapon);
            CreateService(CreateSettings(new[] { weapon }));
            var eventCount = 0;
            using var subscription =
                _events.Subscribe<EquipmentChangedEvent>(_ => eventCount++);
            var before = _service.Snapshot;

            var changed = _service.Equip(
                "missing_item",
                EquipmentSlot.Weapon);

            Assert.That(changed, Is.False);
            Assert.That(eventCount, Is.Zero);
            AssertSnapshotsEqual(before, _service.Snapshot);
        }

        [Test]
        public void Equip_FixedEquipmentToWrongSlotIsAtomicAndSilent()
        {
            var helmet = CreateItem(
                "helmet_a",
                EquipmentItemCategory.Helmet);
            CreateService(CreateSettings(new[] { helmet }));
            var eventCount = 0;
            using var subscription =
                _events.Subscribe<EquipmentChangedEvent>(_ => eventCount++);
            var before = _service.Snapshot;

            var changed = _service.Equip(
                "helmet_a",
                EquipmentSlot.Armor);

            Assert.That(changed, Is.False);
            Assert.That(eventCount, Is.Zero);
            AssertSnapshotsEqual(before, _service.Snapshot);
        }

        [Test]
        public void Equip_DifferentAccessoriesCanUseAnyIndependentSlots()
        {
            var first = CreateItem(
                "accessory_a",
                EquipmentItemCategory.Accessory,
                flatAttack: 10f);
            var second = CreateItem(
                "accessory_b",
                EquipmentItemCategory.Accessory,
                flatAttack: 15f);
            CreateService(CreateSettings(new[] { first, second }));

            Assert.That(
                _service.Equip(
                    "accessory_a",
                    EquipmentSlot.Accessory2),
                Is.True);
            Assert.That(
                _service.Equip(
                    "accessory_b",
                    EquipmentSlot.Accessory5),
                Is.True);

            Assert.That(
                _service.Snapshot.GetEquippedItem(
                    EquipmentSlot.Accessory2).ItemId,
                Is.EqualTo("accessory_a"));
            Assert.That(
                _service.Snapshot.GetEquippedItem(
                    EquipmentSlot.Accessory5).ItemId,
                Is.EqualTo("accessory_b"));
            Assert.That(
                _service.Snapshot.GetFinalStat(StatType.Attack),
                Is.EqualTo(125f).Within(Tolerance));
        }

        [Test]
        public void Equip_SameAccessoryToAnotherSlotPublishesMovedOnce()
        {
            var accessory = CreateItem(
                "moving_accessory",
                EquipmentItemCategory.Accessory);
            CreateService(CreateSettings(new[] { accessory }));
            Assert.That(
                _service.Equip(
                    accessory.ItemId,
                    EquipmentSlot.Accessory1),
                Is.True);
            var events = new List<EquipmentChangedEvent>();
            using var subscription =
                _events.Subscribe<EquipmentChangedEvent>(
                    value => events.Add(value));

            var changed = _service.Equip(
                accessory.ItemId,
                EquipmentSlot.Accessory4);

            Assert.That(changed, Is.True);
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(
                events[0].Kind,
                Is.EqualTo(EquipmentChangeKind.Moved));
            Assert.That(
                events[0].SourceSlot,
                Is.EqualTo(EquipmentSlot.Accessory1));
            Assert.That(
                events[0].TargetSlot,
                Is.EqualTo(EquipmentSlot.Accessory4));
            Assert.That(
                CountItem(_service.Snapshot, accessory.ItemId),
                Is.EqualTo(1));
        }

        [Test]
        public void Unequip_OccupiedSlotPublishesRemovedItem()
        {
            var shoes = CreateItem(
                "shoes_a",
                EquipmentItemCategory.Shoes);
            CreateService(CreateSettings(new[] { shoes }));
            Assert.That(
                _service.Equip(shoes.ItemId, EquipmentSlot.Shoes),
                Is.True);
            EquipmentChangedEvent published = default;
            using var subscription =
                _events.Subscribe<EquipmentChangedEvent>(
                    value => published = value);

            var changed = _service.Unequip(EquipmentSlot.Shoes);

            Assert.That(changed, Is.True);
            Assert.That(
                published.Kind,
                Is.EqualTo(EquipmentChangeKind.Unequipped));
            Assert.That(
                published.SourceSlot,
                Is.EqualTo(EquipmentSlot.Shoes));
            Assert.That(
                published.PreviousItemId,
                Is.EqualTo(shoes.ItemId));
            Assert.That(published.CurrentItemId, Is.Null);
            Assert.That(
                _service.Snapshot.GetEquippedItem(EquipmentSlot.Shoes),
                Is.Null);
        }

        [Test]
        public void Unequip_EmptyOrInvalidSlotIsAtomicAndSilent()
        {
            var weapon = CreateItem(
                "weapon_a",
                EquipmentItemCategory.Weapon);
            CreateService(CreateSettings(new[] { weapon }));
            var eventCount = 0;
            using var subscription =
                _events.Subscribe<EquipmentChangedEvent>(_ => eventCount++);
            var before = _service.Snapshot;

            Assert.That(
                _service.Unequip(EquipmentSlot.Weapon),
                Is.False);
            Assert.That(
                _service.Unequip((EquipmentSlot)999),
                Is.False);

            Assert.That(eventCount, Is.Zero);
            AssertSnapshotsEqual(before, _service.Snapshot);
        }

        [Test]
        public void SetDefinitions_ConvertAndActivateThroughService()
        {
            var set = CreateSet(
                "thunder_set",
                requiredPieces: 2,
                additiveAttack: 0.2f);
            var weapon = CreateItem(
                "thunder_weapon",
                EquipmentItemCategory.Weapon,
                setId: set.SetId);
            var gloves = CreateItem(
                "thunder_gloves",
                EquipmentItemCategory.Gloves,
                setId: set.SetId);
            CreateService(
                CreateSettings(
                    new[] { weapon, gloves },
                    sets: new[] { set }));

            Assert.That(
                _service.Equip(
                    weapon.ItemId,
                    EquipmentSlot.Weapon),
                Is.True);
            Assert.That(
                _service.Equip(
                    gloves.ItemId,
                    EquipmentSlot.Gloves),
                Is.True);

            Assert.That(
                _service.Snapshot.GetFinalStat(StatType.Attack),
                Is.EqualTo(120f).Within(Tolerance));
            Assert.That(_service.Snapshot.ActivatedSets.Count, Is.EqualTo(1));
            Assert.That(
                _service.Snapshot.ActivatedSets[0].SetId,
                Is.EqualTo(set.SetId));
        }

        [Test]
        public void InitialAccessory_UsesConfiguredTargetSlot()
        {
            var accessory = CreateItem(
                "starter_accessory",
                EquipmentItemCategory.Accessory);
            var settings = CreateSettings(
                new[] { accessory },
                starting: new[]
                {
                    new StartingEntry(
                        accessory,
                        EquipmentSlot.Accessory5)
                });

            CreateService(settings);

            Assert.That(
                _service.Snapshot.GetEquippedItem(
                    EquipmentSlot.Accessory1),
                Is.Null);
            Assert.That(
                _service.Snapshot.GetEquippedItem(
                    EquipmentSlot.Accessory5).ItemId,
                Is.EqualTo(accessory.ItemId));
        }

        [Test]
        public void Constructor_DuplicateItemIdThrowsAndDisposesLease()
        {
            var first = CreateItem(
                "duplicate",
                EquipmentItemCategory.Weapon);
            var second = CreateItem(
                "duplicate",
                EquipmentItemCategory.Helmet);
            var settings = CreateSettings(new[] { first, second });
            _events = new EventBus();
            var lease = new FakeSettingsLease(settings);

            Assert.Throws<InvalidOperationException>(
                () => new EquipmentService(settings, _events, lease));

            Assert.That(lease.IsDisposed, Is.True);
        }

        [Test]
        public void Constructor_UnknownSetReferenceThrows()
        {
            var weapon = CreateItem(
                "weapon_a",
                EquipmentItemCategory.Weapon,
                setId: "missing_set");
            var settings = CreateSettings(new[] { weapon });
            _events = new EventBus();

            Assert.Throws<InvalidOperationException>(
                () => new EquipmentService(settings, _events));
        }

        [Test]
        public void Constructor_InvalidStartingTargetThrowsAndDisposesLease()
        {
            var helmet = CreateItem(
                "helmet_a",
                EquipmentItemCategory.Helmet);
            var settings = CreateSettings(
                new[] { helmet },
                starting: new[]
                {
                    new StartingEntry(
                        helmet,
                        EquipmentSlot.Weapon)
                });
            _events = new EventBus();
            var lease = new FakeSettingsLease(settings);

            Assert.Throws<InvalidOperationException>(
                () => new EquipmentService(settings, _events, lease));

            Assert.That(lease.IsDisposed, Is.True);
        }

        [Test]
        public void Dispose_ReleasesLeaseAndRejectsFurtherUse()
        {
            var weapon = CreateItem(
                "weapon_a",
                EquipmentItemCategory.Weapon);
            var settings = CreateSettings(new[] { weapon });
            _events = new EventBus();
            var lease = new FakeSettingsLease(settings);
            _service = new EquipmentService(settings, _events, lease);

            _service.Dispose();

            Assert.That(lease.IsDisposed, Is.True);
            Assert.Throws<ObjectDisposedException>(
                () => _ = _service.Snapshot);
            Assert.Throws<ObjectDisposedException>(
                () => _service.Equip(
                    weapon.ItemId,
                    EquipmentSlot.Weapon));
        }

        [Test]
        public void ItemDefinition_ExposesMetadataAndSlotCompatibility()
        {
            var accessory = CreateItem(
                "accessory_a",
                EquipmentItemCategory.Accessory,
                flatAttack: 12f,
                description: "测试说明",
                rarity: EquipmentRarity.Epic);

            Assert.That(accessory.DisplayName, Is.EqualTo("显示_accessory_a"));
            Assert.That(accessory.Description, Is.EqualTo("测试说明"));
            Assert.That(accessory.Rarity, Is.EqualTo(EquipmentRarity.Epic));
            Assert.That(
                accessory.CanEquipIn(EquipmentSlot.Accessory3),
                Is.True);
            Assert.That(
                accessory.CanEquipIn(EquipmentSlot.Helmet),
                Is.False);
            Assert.That(accessory.ToCoreSpec().Modifiers.Count, Is.EqualTo(1));
        }

        private void CreateService(EquipmentSettings settings)
        {
            _events = new EventBus();
            _service = new EquipmentService(settings, _events);
        }

        private EquipmentItemDefinition CreateItem(
            string itemId,
            EquipmentItemCategory category,
            float flatAttack = 0f,
            string setId = null,
            string description = null,
            EquipmentRarity rarity = EquipmentRarity.Common)
        {
            var item =
                ScriptableObject.CreateInstance<EquipmentItemDefinition>();
            _createdObjects.Add(item);
            var serialized = new SerializedObject(item);
            serialized.FindProperty("_itemId").stringValue = itemId;
            serialized.FindProperty("_displayName").stringValue =
                $"显示_{itemId}";
            serialized.FindProperty("_description").stringValue =
                description ?? string.Empty;
            serialized.FindProperty("_rarity").enumValueIndex = (int)rarity - 1;
            serialized.FindProperty("_category").enumValueIndex =
                (int)category;
            serialized.FindProperty("_setId").stringValue =
                setId ?? string.Empty;

            var modifiers = serialized.FindProperty("_modifiers");
            modifiers.arraySize = flatAttack == 0f ? 0 : 1;
            if (flatAttack != 0f)
            {
                ConfigureModifier(
                    modifiers.GetArrayElementAtIndex(0),
                    StatType.Attack,
                    StatModifierOperation.Flat,
                    flatAttack);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        private EquipmentSetDefinition CreateSet(
            string setId,
            int requiredPieces,
            float additiveAttack)
        {
            var set =
                ScriptableObject.CreateInstance<EquipmentSetDefinition>();
            _createdObjects.Add(set);
            var serialized = new SerializedObject(set);
            serialized.FindProperty("_setId").stringValue = setId;
            serialized.FindProperty("_displayName").stringValue =
                $"显示_{setId}";

            var bonuses = serialized.FindProperty("_bonuses");
            bonuses.arraySize = 1;
            var bonus = bonuses.GetArrayElementAtIndex(0);
            bonus.FindPropertyRelative("_requiredPieceCount").intValue =
                requiredPieces;
            var modifiers = bonus.FindPropertyRelative("_modifiers");
            modifiers.arraySize = 1;
            ConfigureModifier(
                modifiers.GetArrayElementAtIndex(0),
                StatType.Attack,
                StatModifierOperation.AdditivePercent,
                additiveAttack);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return set;
        }

        private EquipmentSettings CreateSettings(
            IReadOnlyList<EquipmentItemDefinition> items,
            IReadOnlyList<EquipmentSetDefinition> sets = null,
            IReadOnlyList<StartingEntry> starting = null)
        {
            var settings =
                ScriptableObject.CreateInstance<EquipmentSettings>();
            _createdObjects.Add(settings);
            var serialized = new SerializedObject(settings);

            var baseStats = serialized.FindProperty("_baseStats");
            baseStats.arraySize = 2;
            ConfigureBaseStat(
                baseStats.GetArrayElementAtIndex(0),
                StatType.Attack,
                100f);
            ConfigureBaseStat(
                baseStats.GetArrayElementAtIndex(1),
                StatType.MaxHealth,
                1000f);

            var itemArray = serialized.FindProperty("_items");
            itemArray.arraySize = items.Count;
            for (var i = 0; i < items.Count; i++)
            {
                itemArray.GetArrayElementAtIndex(i).objectReferenceValue =
                    items[i];
            }

            var setArray = serialized.FindProperty("_sets");
            setArray.arraySize = sets?.Count ?? 0;
            if (sets != null)
            {
                for (var i = 0; i < sets.Count; i++)
                {
                    setArray.GetArrayElementAtIndex(i).objectReferenceValue =
                        sets[i];
                }
            }

            var startingArray =
                serialized.FindProperty("_startingEquipment");
            startingArray.arraySize = starting?.Count ?? 0;
            if (starting != null)
            {
                for (var i = 0; i < starting.Count; i++)
                {
                    var element = startingArray.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("_item")
                        .objectReferenceValue = starting[i].Item;
                    element.FindPropertyRelative("_targetSlot")
                        .enumValueIndex = (int)starting[i].TargetSlot;
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return settings;
        }

        private static void ConfigureModifier(
            SerializedProperty property,
            StatType statType,
            StatModifierOperation operation,
            float value)
        {
            property.FindPropertyRelative("_statType").enumValueIndex =
                (int)statType;
            property.FindPropertyRelative("_operation").enumValueIndex =
                (int)operation;
            property.FindPropertyRelative("_value").floatValue = value;
        }

        private static void ConfigureBaseStat(
            SerializedProperty property,
            StatType statType,
            float value)
        {
            property.FindPropertyRelative("_statType").enumValueIndex =
                (int)statType;
            property.FindPropertyRelative("_value").floatValue = value;
        }

        private static int CountItem(
            EquipmentSnapshot snapshot,
            string itemId)
        {
            var count = 0;
            for (var i = 0; i < snapshot.Slots.Count; i++)
            {
                var item = snapshot.Slots[i].Item;
                if (item != null &&
                    string.Equals(
                        item.ItemId,
                        itemId,
                        StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static void AssertSnapshotsEqual(
            EquipmentSnapshot expected,
            EquipmentSnapshot actual)
        {
            Assert.That(actual.Revision, Is.EqualTo(expected.Revision));
            for (var i = 0; i < expected.Slots.Count; i++)
            {
                Assert.That(
                    actual.Slots[i].Slot,
                    Is.EqualTo(expected.Slots[i].Slot));
                Assert.That(
                    actual.Slots[i].Item,
                    Is.SameAs(expected.Slots[i].Item));
            }

            foreach (var pair in expected.FinalStats)
            {
                Assert.That(
                    actual.FinalStats[pair.Key],
                    Is.EqualTo(pair.Value).Within(Tolerance));
            }
        }

        /// <summary>
        /// 为测试提供不依赖真实 YooAsset 的设置资源租约。
        /// </summary>
        private sealed class FakeSettingsLease :
            IAssetLease<EquipmentSettings>
        {
            public FakeSettingsLease(EquipmentSettings asset)
            {
                Asset = asset;
            }

            public bool IsDisposed { get; private set; }

            public string Location => "test://equipment-settings";

            public EquipmentSettings Asset { get; }

            public bool IsValid => !IsDisposed && Asset != null;

            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        /// <summary>
        /// 保存测试构造设置资源时使用的一条初始装备参数。
        /// </summary>
        private readonly struct StartingEntry
        {
            public StartingEntry(
                EquipmentItemDefinition item,
                EquipmentSlot targetSlot)
            {
                Item = item;
                TargetSlot = targetSlot;
            }

            public EquipmentItemDefinition Item { get; }

            public EquipmentSlot TargetSlot { get; }
        }
    }
}
