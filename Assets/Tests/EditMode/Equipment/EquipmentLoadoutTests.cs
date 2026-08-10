using System;
using System.Collections.Generic;
using NUnit.Framework;
using Train.Equipment.Core;

namespace Train.Tests.EditMode.Equipment
{
    /// <summary>
    /// 验证装备领域模型的换装原子性、属性公式、套装门槛和快照只读语义。
    /// </summary>
    public sealed class EquipmentLoadoutTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void Equip_PutsItemIntoItsDeclaredSlotAndUpdatesRevision()
        {
            var loadout = CreateLoadout(attack: 100f);
            var weapon = CreateItem(
                "weapon_a",
                EquipmentSlot.Weapon,
                modifiers: FlatAttack(20f));

            var changed = loadout.Equip(weapon);

            Assert.That(changed, Is.True);
            Assert.That(loadout.Revision, Is.EqualTo(1));
            Assert.That(
                loadout.Snapshot.GetEquippedItem(EquipmentSlot.Weapon),
                Is.SameAs(weapon));
            Assert.That(
                loadout.Snapshot.GetFinalStat(StatType.Attack),
                Is.EqualTo(120f).Within(Tolerance));
        }

        [Test]
        public void Equip_ItemsInDifferentSlotsCoexist()
        {
            var loadout = CreateLoadout(attack: 100f);
            var weapon = CreateItem(
                "weapon_a",
                EquipmentSlot.Weapon,
                modifiers: FlatAttack(10f));
            var helmet = CreateItem(
                "helmet_a",
                EquipmentSlot.Helmet,
                modifiers: FlatAttack(15f));

            Assert.That(loadout.Equip(weapon), Is.True);
            Assert.That(loadout.Equip(helmet), Is.True);

            Assert.That(
                loadout.Snapshot.GetEquippedItem(EquipmentSlot.Weapon),
                Is.SameAs(weapon));
            Assert.That(
                loadout.Snapshot.GetEquippedItem(EquipmentSlot.Helmet),
                Is.SameAs(helmet));
            Assert.That(
                loadout.Snapshot.GetFinalStat(StatType.Attack),
                Is.EqualTo(125f).Within(Tolerance));
        }

        [Test]
        public void Equip_NewItemInSameSlotReplacesPreviousItem()
        {
            var loadout = CreateLoadout(attack: 100f);
            var oldWeapon = CreateItem(
                "weapon_old",
                EquipmentSlot.Weapon,
                modifiers: FlatAttack(10f));
            var newWeapon = CreateItem(
                "weapon_new",
                EquipmentSlot.Weapon,
                modifiers: FlatAttack(40f));
            Assert.That(loadout.Equip(oldWeapon), Is.True);

            Assert.That(loadout.Equip(newWeapon), Is.True);

            Assert.That(loadout.Revision, Is.EqualTo(2));
            Assert.That(
                loadout.Snapshot.GetEquippedItem(EquipmentSlot.Weapon),
                Is.SameAs(newWeapon));
            Assert.That(
                loadout.Snapshot.GetFinalStat(StatType.Attack),
                Is.EqualTo(140f).Within(Tolerance));
        }

        [Test]
        public void Unequip_RemovesItemAndItsModifiers()
        {
            var loadout = CreateLoadout(attack: 100f);
            var weapon = CreateItem(
                "weapon_a",
                EquipmentSlot.Weapon,
                modifiers: FlatAttack(20f));
            Assert.That(loadout.Equip(weapon), Is.True);

            var changed = loadout.Unequip(EquipmentSlot.Weapon);

            Assert.That(changed, Is.True);
            Assert.That(loadout.Revision, Is.EqualTo(2));
            Assert.That(
                loadout.Snapshot.GetEquippedItem(EquipmentSlot.Weapon),
                Is.Null);
            Assert.That(
                loadout.Snapshot.GetFinalStat(StatType.Attack),
                Is.EqualTo(100f).Within(Tolerance));
        }

        [Test]
        public void Unequip_EmptySlotIsAtomicNoOp()
        {
            var loadout = CreateLoadout(attack: 100f);
            var eventCount = 0;
            loadout.Changed += (_, __) => eventCount++;
            var before = loadout.Snapshot;

            var changed = loadout.Unequip(EquipmentSlot.Accessory1);

            Assert.That(changed, Is.False);
            Assert.That(loadout.Revision, Is.Zero);
            Assert.That(eventCount, Is.Zero);
            AssertSnapshotsEqual(before, loadout.Snapshot);
        }

        [Test]
        public void Equip_SameStableIdTwiceIsAtomicNoOp()
        {
            var loadout = CreateLoadout(attack: 100f);
            var weapon = CreateItem(
                "unique_weapon",
                EquipmentSlot.Weapon,
                modifiers: FlatAttack(20f));
            Assert.That(loadout.Equip(weapon), Is.True);
            var before = loadout.Snapshot;
            var eventCount = 0;
            loadout.Changed += (_, __) => eventCount++;

            var changed = loadout.Equip(weapon);

            Assert.That(changed, Is.False);
            Assert.That(loadout.Revision, Is.EqualTo(1));
            Assert.That(eventCount, Is.Zero);
            AssertSnapshotsEqual(before, loadout.Snapshot);
        }

        [Test]
        public void Equip_DuplicateStableIdInAnotherSlotIsRejectedAtomically()
        {
            var loadout = CreateLoadout(attack: 100f);
            var weapon = CreateItem(
                "duplicate_id",
                EquipmentSlot.Weapon,
                modifiers: FlatAttack(20f));
            var conflictingHelmet = CreateItem(
                "duplicate_id",
                EquipmentSlot.Helmet,
                modifiers: FlatAttack(999f));
            Assert.That(loadout.Equip(weapon), Is.True);
            var before = loadout.Snapshot;

            var changed = loadout.Equip(conflictingHelmet);

            Assert.That(changed, Is.False);
            Assert.That(loadout.Revision, Is.EqualTo(1));
            AssertSnapshotsEqual(before, loadout.Snapshot);
        }

        [Test]
        public void Snapshot_ContainsAllTenSlotsInStableOrder()
        {
            var snapshot = CreateLoadout(attack: 100f).Snapshot;

            Assert.That(snapshot.Slots.Count, Is.EqualTo(10));
            Assert.That(
                GetSnapshotSlotOrder(snapshot),
                Is.EqualTo(
                    new[]
                    {
                        EquipmentSlot.Weapon,
                        EquipmentSlot.Helmet,
                        EquipmentSlot.Armor,
                        EquipmentSlot.Gloves,
                        EquipmentSlot.Shoes,
                        EquipmentSlot.Accessory1,
                        EquipmentSlot.Accessory2,
                        EquipmentSlot.Accessory3,
                        EquipmentSlot.Accessory4,
                        EquipmentSlot.Accessory5
                    }));
        }

        [Test]
        public void Equip_DifferentAccessoriesCanOccupyDifferentAccessorySlots()
        {
            var loadout = CreateLoadout(attack: 100f);
            var firstAccessory = CreateItem(
                "accessory_a",
                EquipmentSlot.Accessory1,
                modifiers: FlatAttack(10f));
            var secondAccessory = CreateItem(
                "accessory_b",
                EquipmentSlot.Accessory1,
                modifiers: FlatAttack(20f));

            Assert.That(loadout.Equip(firstAccessory), Is.True);
            Assert.That(
                loadout.Equip(
                    secondAccessory,
                    EquipmentSlot.Accessory2),
                Is.True);

            Assert.That(
                loadout.Snapshot.GetEquippedItem(
                    EquipmentSlot.Accessory1),
                Is.SameAs(firstAccessory));
            Assert.That(
                loadout.Snapshot.GetEquippedItem(
                    EquipmentSlot.Accessory2),
                Is.SameAs(secondAccessory));
            Assert.That(
                loadout.Snapshot.GetFinalStat(StatType.Attack),
                Is.EqualTo(130f).Within(Tolerance));
        }

        [Test]
        public void Equip_AccessoryCanTargetAnyOfFiveAccessorySlots()
        {
            var loadout = CreateLoadout(attack: 100f);
            var accessory = CreateItem(
                "flexible_accessory",
                EquipmentSlot.Accessory1);

            Assert.That(
                loadout.Equip(accessory, EquipmentSlot.Accessory5),
                Is.True);

            Assert.That(
                loadout.Snapshot.GetEquippedItem(
                    EquipmentSlot.Accessory1),
                Is.Null);
            Assert.That(
                loadout.Snapshot.GetEquippedItem(
                    EquipmentSlot.Accessory5),
                Is.SameAs(accessory));
        }

        [Test]
        public void Equip_SameAccessoryToAnotherSlotMovesWithoutDuplicating()
        {
            var loadout = CreateLoadout(attack: 100f);
            var accessory = CreateItem(
                "moving_accessory",
                EquipmentSlot.Accessory1,
                modifiers: FlatAttack(15f));
            var events = new List<EquipmentChangedEventArgs>();
            loadout.Changed += (_, args) => events.Add(args);
            Assert.That(loadout.Equip(accessory), Is.True);

            Assert.That(
                loadout.Equip(accessory, EquipmentSlot.Accessory3),
                Is.True);

            var snapshot = loadout.Snapshot;
            Assert.That(loadout.Revision, Is.EqualTo(2));
            Assert.That(
                snapshot.GetEquippedItem(EquipmentSlot.Accessory1),
                Is.Null);
            Assert.That(
                snapshot.GetEquippedItem(EquipmentSlot.Accessory3),
                Is.SameAs(accessory));
            Assert.That(
                CountItem(snapshot, accessory.ItemId),
                Is.EqualTo(1));
            Assert.That(
                snapshot.GetFinalStat(StatType.Attack),
                Is.EqualTo(115f).Within(Tolerance));
            Assert.That(
                events[1].Kind,
                Is.EqualTo(EquipmentChangeKind.Moved));
            Assert.That(
                events[1].SourceSlot,
                Is.EqualTo(EquipmentSlot.Accessory1));
            Assert.That(
                events[1].Slot,
                Is.EqualTo(EquipmentSlot.Accessory3));
        }

        [Test]
        public void Equip_MoveIntoOccupiedAccessorySlotReplacesTargetItem()
        {
            var loadout = CreateLoadout(attack: 100f);
            var movingAccessory = CreateItem(
                "moving_accessory",
                EquipmentSlot.Accessory1,
                modifiers: FlatAttack(10f));
            var displacedAccessory = CreateItem(
                "displaced_accessory",
                EquipmentSlot.Accessory1,
                modifiers: FlatAttack(50f));
            var events = new List<EquipmentChangedEventArgs>();
            loadout.Changed += (_, args) => events.Add(args);
            Assert.That(loadout.Equip(movingAccessory), Is.True);
            Assert.That(
                loadout.Equip(
                    displacedAccessory,
                    EquipmentSlot.Accessory2),
                Is.True);

            Assert.That(
                loadout.Equip(
                    movingAccessory,
                    EquipmentSlot.Accessory2),
                Is.True);

            var snapshot = loadout.Snapshot;
            Assert.That(
                snapshot.GetEquippedItem(EquipmentSlot.Accessory1),
                Is.Null);
            Assert.That(
                snapshot.GetEquippedItem(EquipmentSlot.Accessory2),
                Is.SameAs(movingAccessory));
            Assert.That(
                CountItem(snapshot, displacedAccessory.ItemId),
                Is.Zero);
            Assert.That(
                snapshot.GetFinalStat(StatType.Attack),
                Is.EqualTo(110f).Within(Tolerance));
            Assert.That(events[2].PreviousItem, Is.SameAs(displacedAccessory));
            Assert.That(events[2].CurrentItem, Is.SameAs(movingAccessory));
            Assert.That(
                events[2].SourceSlot,
                Is.EqualTo(EquipmentSlot.Accessory1));
        }

        [Test]
        public void Equip_DifferentAccessoryWithSameItemIdIsRejected()
        {
            var loadout = CreateLoadout(attack: 100f);
            var original = CreateItem(
                "same_accessory_id",
                EquipmentSlot.Accessory1);
            var conflictingDefinition = CreateItem(
                "same_accessory_id",
                EquipmentSlot.Accessory1);
            Assert.That(loadout.Equip(original), Is.True);
            var before = loadout.Snapshot;

            var changed = loadout.Equip(
                conflictingDefinition,
                EquipmentSlot.Accessory2);

            Assert.That(changed, Is.False);
            Assert.That(loadout.Revision, Is.EqualTo(1));
            AssertSnapshotsEqual(before, loadout.Snapshot);
            Assert.That(
                CountItem(loadout.Snapshot, original.ItemId),
                Is.EqualTo(1));
        }

        [Test]
        public void Equip_IncompatibleTargetSlotIsRejectedAtomically()
        {
            var loadout = CreateLoadout(attack: 100f);
            var helmet = CreateItem(
                "helmet_a",
                EquipmentSlot.Helmet);
            var accessory = CreateItem(
                "accessory_a",
                EquipmentSlot.Accessory1);
            var eventCount = 0;
            loadout.Changed += (_, __) => eventCount++;

            Assert.Throws<InvalidOperationException>(
                () => loadout.Equip(helmet, EquipmentSlot.Armor));
            Assert.Throws<InvalidOperationException>(
                () => loadout.Equip(
                    accessory,
                    EquipmentSlot.Gloves));

            Assert.That(loadout.Revision, Is.Zero);
            Assert.That(eventCount, Is.Zero);
            var snapshot = loadout.Snapshot;
            for (var i = 0; i < snapshot.Slots.Count; i++)
            {
                Assert.That(snapshot.Slots[i].IsEmpty, Is.True);
            }
        }

        [Test]
        public void StatFormula_AppliesFlatThenAdditiveThenMultiplicative()
        {
            var loadout = CreateLoadout(attack: 100f);
            var item = CreateItem(
                "formula_item",
                EquipmentSlot.Weapon,
                modifiers: new[]
                {
                    FlatAttack(20f),
                    FlatAttack(30f),
                    AdditiveAttack(0.2f),
                    AdditiveAttack(0.1f),
                    MultiplicativeAttack(0.5f),
                    MultiplicativeAttack(0.1f)
                });

            Assert.That(loadout.Equip(item), Is.True);

            Assert.That(
                loadout.Snapshot.GetFinalStat(StatType.Attack),
                Is.EqualTo(321.75f).Within(Tolerance));
        }

        [Test]
        public void StatFormula_IsIndependentFromModifierInputOrder()
        {
            var forwardModifiers = new[]
            {
                FlatAttack(20f),
                AdditiveAttack(0.2f),
                MultiplicativeAttack(0.5f),
                FlatAttack(30f),
                AdditiveAttack(0.1f),
                MultiplicativeAttack(0.1f)
            };
            var reverseModifiers = new[]
            {
                MultiplicativeAttack(0.1f),
                AdditiveAttack(0.1f),
                FlatAttack(30f),
                MultiplicativeAttack(0.5f),
                AdditiveAttack(0.2f),
                FlatAttack(20f)
            };
            var first = CreateLoadout(attack: 100f);
            var second = CreateLoadout(attack: 100f);

            Assert.That(
                first.Equip(
                    CreateItem(
                        "forward",
                        EquipmentSlot.Weapon,
                        modifiers: forwardModifiers)),
                Is.True);
            Assert.That(
                second.Equip(
                    CreateItem(
                        "reverse",
                        EquipmentSlot.Weapon,
                        modifiers: reverseModifiers)),
                Is.True);

            Assert.That(
                first.Snapshot.GetFinalStat(StatType.Attack),
                Is.EqualTo(
                    second.Snapshot.GetFinalStat(StatType.Attack))
                    .Within(Tolerance));
        }

        [Test]
        public void RatioStats_UseDecimalRepresentation()
        {
            var baseStats = DefaultBaseStats(attack: 100f);
            baseStats[StatType.CritRate] = 0.05f;
            var loadout = new EquipmentLoadout(
                baseStats,
                Array.Empty<EquipmentSetSpec>());
            var accessory = CreateItem(
                "crit_accessory",
                EquipmentSlot.Accessory1,
                modifiers: new[]
                {
                    new StatModifier(
                        StatType.CritRate,
                        StatModifierOperation.Flat,
                        0.1f)
                });

            Assert.That(loadout.Equip(accessory), Is.True);

            Assert.That(
                loadout.Snapshot.GetFinalStat(StatType.CritRate),
                Is.EqualTo(0.15f).Within(Tolerance));
        }

        [Test]
        public void TwoPieceSet_ActivatesItsFirstBonus()
        {
            var set = CreateElectricSet();
            var loadout = CreateLoadout(attack: 100f, set);

            Assert.That(
                loadout.Equip(
                    CreateItem(
                        "electric_weapon",
                        EquipmentSlot.Weapon,
                        setId: set.SetId)),
                Is.True);
            Assert.That(
                loadout.Equip(
                    CreateItem(
                        "electric_helmet",
                        EquipmentSlot.Helmet,
                        setId: set.SetId)),
                Is.True);

            var snapshot = loadout.Snapshot;
            Assert.That(
                snapshot.GetFinalStat(StatType.Attack),
                Is.EqualTo(120f).Within(Tolerance));
            Assert.That(snapshot.ActivatedSets.Count, Is.EqualTo(1));
            Assert.That(
                snapshot.ActivatedSets[0].EquippedPieceCount,
                Is.EqualTo(2));
            Assert.That(
                snapshot.ActivatedSets[0].ActiveBonusPieceCounts,
                Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void ThreePieceSet_StacksTwoAndThreePieceBonuses()
        {
            var set = CreateElectricSet();
            var loadout = CreateLoadout(attack: 100f, set);
            Assert.That(
                loadout.Equip(
                    CreateItem(
                        "electric_weapon",
                        EquipmentSlot.Weapon,
                        setId: set.SetId)),
                Is.True);
            Assert.That(
                loadout.Equip(
                    CreateItem(
                        "electric_helmet",
                        EquipmentSlot.Helmet,
                        setId: set.SetId)),
                Is.True);

            Assert.That(
                loadout.Equip(
                    CreateItem(
                        "electric_armor",
                        EquipmentSlot.Armor,
                        setId: set.SetId)),
                Is.True);

            var snapshot = loadout.Snapshot;
            Assert.That(
                snapshot.GetFinalStat(StatType.Attack),
                Is.EqualTo(132f).Within(Tolerance));
            Assert.That(
                snapshot.GetFinalStat(StatType.ElectricDamageBonus),
                Is.EqualTo(0.3f).Within(Tolerance));
            Assert.That(
                snapshot.ActivatedSets[0].ActiveBonusPieceCounts,
                Is.EqualTo(new[] { 2, 3 }));
        }

        [Test]
        public void ReplacingSetPiece_DeactivatesSetBonus()
        {
            var set = CreateElectricSet();
            var loadout = CreateLoadout(attack: 100f, set);
            Assert.That(
                loadout.Equip(
                    CreateItem(
                        "electric_weapon",
                        EquipmentSlot.Weapon,
                        setId: set.SetId)),
                Is.True);
            Assert.That(
                loadout.Equip(
                    CreateItem(
                        "electric_helmet",
                        EquipmentSlot.Helmet,
                        setId: set.SetId)),
                Is.True);
            Assert.That(
                loadout.Snapshot.ActivatedSets.Count,
                Is.EqualTo(1));

            Assert.That(
                loadout.Equip(
                    CreateItem(
                        "plain_helmet",
                        EquipmentSlot.Helmet)),
                Is.True);

            Assert.That(
                loadout.Snapshot.GetFinalStat(StatType.Attack),
                Is.EqualTo(100f).Within(Tolerance));
            Assert.That(loadout.Snapshot.ActivatedSets, Is.Empty);
        }

        [Test]
        public void UnknownSetId_DoesNotActivateAnUnregisteredBonus()
        {
            var loadout = CreateLoadout(attack: 100f);
            Assert.That(
                loadout.Equip(
                    CreateItem(
                        "unknown_weapon",
                        EquipmentSlot.Weapon,
                        setId: "unknown_set")),
                Is.True);
            Assert.That(
                loadout.Equip(
                    CreateItem(
                        "unknown_helmet",
                        EquipmentSlot.Helmet,
                        setId: "unknown_set")),
                Is.True);

            Assert.That(loadout.Snapshot.ActivatedSets, Is.Empty);
            Assert.That(
                loadout.Snapshot.GetFinalStat(StatType.Attack),
                Is.EqualTo(100f).Within(Tolerance));
        }

        [Test]
        public void ChangedEvent_FiresOncePerCommittedOperation()
        {
            var loadout = CreateLoadout(attack: 100f);
            var first = CreateItem(
                "first_weapon",
                EquipmentSlot.Weapon);
            var second = CreateItem(
                "second_weapon",
                EquipmentSlot.Weapon);
            var events = new List<EquipmentChangedEventArgs>();
            loadout.Changed += (_, args) => events.Add(args);

            Assert.That(loadout.Equip(first), Is.True);
            Assert.That(loadout.Equip(second), Is.True);
            Assert.That(loadout.Unequip(EquipmentSlot.Weapon), Is.True);

            Assert.That(events.Count, Is.EqualTo(3));
            Assert.That(
                events[0].Kind,
                Is.EqualTo(EquipmentChangeKind.Equipped));
            Assert.That(events[0].PreviousItem, Is.Null);
            Assert.That(events[0].CurrentItem, Is.SameAs(first));
            Assert.That(events[0].SourceSlot, Is.Null);
            Assert.That(events[0].Snapshot.Revision, Is.EqualTo(1));
            Assert.That(events[1].PreviousItem, Is.SameAs(first));
            Assert.That(events[1].CurrentItem, Is.SameAs(second));
            Assert.That(events[1].SourceSlot, Is.Null);
            Assert.That(
                events[2].Kind,
                Is.EqualTo(EquipmentChangeKind.Unequipped));
            Assert.That(events[2].PreviousItem, Is.SameAs(second));
            Assert.That(events[2].CurrentItem, Is.Null);
            Assert.That(
                events[2].SourceSlot,
                Is.EqualTo(EquipmentSlot.Weapon));
            Assert.That(events[2].Snapshot.Revision, Is.EqualTo(3));
        }

        [Test]
        public void Equip_WhenStatCalculationOverflows_RollsBackAtomically()
        {
            var baseStats = DefaultBaseStats(attack: float.MaxValue);
            var loadout = new EquipmentLoadout(
                baseStats,
                Array.Empty<EquipmentSetSpec>());
            var overflowingItem = CreateItem(
                "overflowing_weapon",
                EquipmentSlot.Weapon,
                modifiers: MultiplicativeAttack(1f));
            var eventCount = 0;
            loadout.Changed += (_, __) => eventCount++;

            Assert.Throws<OverflowException>(
                () => loadout.Equip(overflowingItem));

            Assert.That(loadout.Revision, Is.Zero);
            Assert.That(eventCount, Is.Zero);
            Assert.That(
                loadout.Snapshot.GetEquippedItem(EquipmentSlot.Weapon),
                Is.Null);
            Assert.That(
                loadout.Snapshot.GetFinalStat(StatType.Attack),
                Is.EqualTo(float.MaxValue));
        }

        [Test]
        public void Snapshot_RemainsPointInTimeAfterLaterEquipmentChanges()
        {
            var loadout = CreateLoadout(attack: 100f);
            var weapon = CreateItem(
                "weapon_a",
                EquipmentSlot.Weapon,
                modifiers: FlatAttack(20f));
            Assert.That(loadout.Equip(weapon), Is.True);
            var oldSnapshot = loadout.Snapshot;

            Assert.That(
                loadout.Equip(
                    CreateItem(
                        "helmet_a",
                        EquipmentSlot.Helmet,
                        modifiers: FlatAttack(30f))),
                Is.True);

            Assert.That(oldSnapshot.Revision, Is.EqualTo(1));
            Assert.That(
                oldSnapshot.GetEquippedItem(EquipmentSlot.Helmet),
                Is.Null);
            Assert.That(
                oldSnapshot.GetFinalStat(StatType.Attack),
                Is.EqualTo(120f).Within(Tolerance));
            Assert.That(
                loadout.Snapshot.GetFinalStat(StatType.Attack),
                Is.EqualTo(150f).Within(Tolerance));
        }

        [Test]
        public void SnapshotCollections_AreReadOnly()
        {
            var loadout = CreateLoadout(attack: 100f);
            var snapshot = loadout.Snapshot;
            var slotList =
                (IList<EquipmentSlotSnapshot>)snapshot.Slots;
            var statDictionary =
                (IDictionary<StatType, float>)snapshot.FinalStats;

            Assert.Throws<NotSupportedException>(
                () => slotList.Add(
                    new EquipmentSlotSnapshot(
                        EquipmentSlot.Weapon,
                        null)));
            Assert.Throws<NotSupportedException>(
                () => statDictionary[StatType.Attack] = 999f);
        }

        [Test]
        public void Constructor_CopiesBaseStatsAndRejectsDuplicateSetIds()
        {
            var baseStats = DefaultBaseStats(attack: 100f);
            var loadout = new EquipmentLoadout(
                baseStats,
                Array.Empty<EquipmentSetSpec>());
            baseStats[StatType.Attack] = 999f;
            var set = CreateElectricSet();

            Assert.That(
                loadout.Snapshot.GetBaseStat(StatType.Attack),
                Is.EqualTo(100f));
            Assert.Throws<ArgumentException>(
                () => new EquipmentLoadout(
                    DefaultBaseStats(attack: 100f),
                    new[] { set, set }));
        }

        private static EquipmentLoadout CreateLoadout(
            float attack,
            params EquipmentSetSpec[] sets)
        {
            return new EquipmentLoadout(
                DefaultBaseStats(attack),
                sets ?? Array.Empty<EquipmentSetSpec>());
        }

        private static Dictionary<StatType, float> DefaultBaseStats(
            float attack)
        {
            return new Dictionary<StatType, float>
            {
                [StatType.MaxHealth] = 1000f,
                [StatType.Attack] = attack,
                [StatType.Defense] = 50f,
                [StatType.CritRate] = 0.05f,
                [StatType.CritDamage] = 0.5f,
                [StatType.ElectricDamageBonus] = 0f
            };
        }

        private static EquipmentItemSpec CreateItem(
            string itemId,
            EquipmentSlot slot,
            string setId = null,
            params StatModifier[] modifiers)
        {
            return new EquipmentItemSpec(
                itemId,
                $"显示名_{itemId}",
                slot,
                setId,
                modifiers ?? Array.Empty<StatModifier>());
        }

        private static EquipmentSetSpec CreateElectricSet()
        {
            return new EquipmentSetSpec(
                "electric_set",
                "雷鸣套装",
                new[]
                {
                    new EquipmentSetBonusSpec(
                        2,
                        new[]
                        {
                            AdditiveAttack(0.2f)
                        }),
                    new EquipmentSetBonusSpec(
                        3,
                        new[]
                        {
                            MultiplicativeAttack(0.1f),
                            new StatModifier(
                                StatType.ElectricDamageBonus,
                                StatModifierOperation.Flat,
                                0.3f)
                        })
                });
        }

        private static StatModifier FlatAttack(float value)
        {
            return new StatModifier(
                StatType.Attack,
                StatModifierOperation.Flat,
                value);
        }

        private static StatModifier AdditiveAttack(float value)
        {
            return new StatModifier(
                StatType.Attack,
                StatModifierOperation.AdditivePercent,
                value);
        }

        private static StatModifier MultiplicativeAttack(float value)
        {
            return new StatModifier(
                StatType.Attack,
                StatModifierOperation.MultiplicativePercent,
                value);
        }

        private static EquipmentSlot[] GetSnapshotSlotOrder(
            EquipmentSnapshot snapshot)
        {
            var result = new EquipmentSlot[snapshot.Slots.Count];
            for (var i = 0; i < snapshot.Slots.Count; i++)
            {
                result[i] = snapshot.Slots[i].Slot;
            }

            return result;
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
            Assert.That(actual.Slots.Count, Is.EqualTo(expected.Slots.Count));
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
    }
}
