using System.Collections.Generic;
using NUnit.Framework;
using Train.Equipment.Application;
using Train.Equipment.Core;
using Train.Equipment.Data;
using UnityEngine;

namespace Train.Tests.EditMode.Equipment.Application
{
    public sealed class EquipmentDescriptionFormatterTests
    {
        private readonly List<Object> _createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (var i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    Object.DestroyImmediate(_createdObjects[i]);
                }
            }

            _createdObjects.Clear();
        }

        [Test]
        public void Format_UsesRealDescriptionAndShowsSetProgress()
        {
            var item = CreateItem(
                "storm_blade",
                "雷鸣刀",
                "附带雷属性伤害的近战武器",
                EquipmentItemCategory.Weapon,
                "storm_protocol");
            var secondItem = CreateItem(
                "storm_helm",
                "风暴目镜",
                "提高雷属性侦测能力",
                EquipmentItemCategory.Helmet,
                "storm_protocol");
            var set = CreateSet("storm_protocol", "风暴协议", 2);
            var loadout = new EquipmentLoadout(
                new Dictionary<StatType, float>(),
                new[] { set.ToCoreSpec() });
            loadout.Equip(item.ToCoreSpec(), EquipmentSlot.Weapon);
            var service = new FakeEquipmentService(
                loadout.Snapshot,
                new[] { item, secondItem },
                set);

            var description = EquipmentDescriptionFormatter.Format(item, service);

            Assert.That(description, Does.Contain("附带雷属性伤害的近战武器"));
            Assert.That(description, Does.Not.Contain("由 Luban 配置"));
            Assert.That(description, Does.Contain("所属套装：风暴协议"));
            Assert.That(description, Does.Contain("当前已装备：1件"));
            Assert.That(
                description,
                Does.Contain("2件奖励：未激活（攻击力 +10）"));
        }

        [Test]
        public void Format_MarksReachedSetBonusAsActive()
        {
            var item = CreateItem(
                "storm_blade",
                "雷鸣刀",
                "真实说明",
                EquipmentItemCategory.Weapon,
                "storm_protocol");
            var secondItem = CreateItem(
                "storm_helm",
                "风暴目镜",
                "真实说明",
                EquipmentItemCategory.Helmet,
                "storm_protocol");
            var set = CreateSet("storm_protocol", "风暴协议", 2);
            var loadout = new EquipmentLoadout(
                new Dictionary<StatType, float>(),
                new[] { set.ToCoreSpec() });
            loadout.Equip(item.ToCoreSpec(), EquipmentSlot.Weapon);
            loadout.Equip(secondItem.ToCoreSpec(), EquipmentSlot.Helmet);
            var service = new FakeEquipmentService(
                loadout.Snapshot,
                new[] { item, secondItem },
                set);

            var description = EquipmentDescriptionFormatter.Format(item, service);

            Assert.That(description, Does.Contain("当前已装备：2件"));
            Assert.That(
                description,
                Does.Contain("2件奖励：已激活（攻击力 +10）"));
        }

        [Test]
        public void Format_ShowsEverySetTierAndDoesNotAddSetInfoWithoutSet()
        {
            var item = CreateItem(
                "storm_blade",
                "雷鸣刀",
                "真实说明",
                EquipmentItemCategory.Weapon,
                "storm_protocol");
            var secondItem = CreateItem(
                "storm_helm",
                "风暴目镜",
                "真实说明",
                EquipmentItemCategory.Helmet,
                "storm_protocol");
            var standaloneItem = CreateItem(
                "plain_blade",
                "普通刀",
                "独立装备说明",
                EquipmentItemCategory.Weapon,
                string.Empty);
            var set = CreateSet("storm_protocol", "风暴协议", 2, 4);
            var loadout = new EquipmentLoadout(
                new Dictionary<StatType, float>(),
                new[] { set.ToCoreSpec() });
            loadout.Equip(item.ToCoreSpec(), EquipmentSlot.Weapon);
            loadout.Equip(secondItem.ToCoreSpec(), EquipmentSlot.Helmet);
            var service = new FakeEquipmentService(
                loadout.Snapshot,
                new[] { item, secondItem, standaloneItem },
                set);

            var setDescription = EquipmentDescriptionFormatter.Format(
                item,
                service);
            var standaloneDescription = EquipmentDescriptionFormatter.Format(
                standaloneItem,
                service);

            Assert.That(
                setDescription,
                Does.Contain("2件奖励：已激活（攻击力 +10）"));
            Assert.That(
                setDescription,
                Does.Contain("4件奖励：未激活（攻击力 +10）"));
            Assert.That(standaloneDescription, Is.EqualTo("独立装备说明"));
            Assert.That(standaloneDescription, Does.Not.Contain("套装"));
        }

        private EquipmentItemDefinition CreateItem(
            string itemId,
            string displayName,
            string description,
            EquipmentItemCategory category,
            string setId)
        {
            var item = EquipmentItemDefinition.CreateRuntime(
                itemId,
                displayName,
                description,
                EquipmentRarity.Common,
                category,
                null,
                setId);
            _createdObjects.Add(item);
            return item;
        }

        private EquipmentSetDefinition CreateSet(
            string setId,
            string displayName,
            params int[] requiredPieceCounts)
        {
            var bonuses = new List<EquipmentSetBonusDefinition>();
            foreach (var requiredPieceCount in requiredPieceCounts)
            {
                bonuses.Add(
                    EquipmentSetBonusDefinition.CreateRuntime(
                        requiredPieceCount,
                        new[]
                        {
                            EquipmentStatModifierDefinition.CreateRuntime(
                                StatType.Attack,
                                StatModifierOperation.Flat,
                                10f)
                        }));
            }

            var set = EquipmentSetDefinition.CreateRuntime(
                setId,
                displayName,
                bonuses);
            _createdObjects.Add(set);
            return set;
        }

        private sealed class FakeEquipmentService : IEquipmentService
        {
            private readonly EquipmentSetDefinition _set;

            public FakeEquipmentService(
                EquipmentSnapshot snapshot,
                IReadOnlyList<EquipmentItemDefinition> catalog,
                EquipmentSetDefinition set)
            {
                Snapshot = snapshot;
                Catalog = catalog;
                _set = set;
            }

            public EquipmentSnapshot Snapshot { get; }

            public IReadOnlyList<EquipmentItemDefinition> Catalog { get; }

            public bool TryGetDefinition(
                string itemId,
                out EquipmentItemDefinition definition)
            {
                for (var i = 0; i < Catalog.Count; i++)
                {
                    if (Catalog[i] != null && Catalog[i].ItemId == itemId)
                    {
                        definition = Catalog[i];
                        return true;
                    }
                }

                definition = null;
                return false;
            }

            public bool TryGetSetDefinition(
                string setId,
                out EquipmentSetDefinition definition)
            {
                if (_set != null && _set.SetId == setId)
                {
                    definition = _set;
                    return true;
                }

                definition = null;
                return false;
            }

            public bool Equip(string itemId, EquipmentSlot targetSlot) => false;

            public bool Unequip(EquipmentSlot slot) => false;
        }
    }
}
