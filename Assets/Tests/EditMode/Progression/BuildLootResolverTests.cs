using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Train.Composition.Progression;
using Train.Equipment.Data;
using Train.Inventory.Data;
using UnityEngine;

namespace Train.Tests.EditMode.Progression
{
    /// <summary>验证四套构筑配置和确定性掉落规则。</summary>
    public sealed class BuildLootResolverTests
    {
        private readonly List<UnityEngine.Object> _createdObjects =
            new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
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
        public void LubanData_RegistersFourBuildsWithFiveMembersAndThreeTiers()
        {
            var sets = ReadLubanFile("equipment_sets.csv");
            var members = ReadLubanFile("equipment_set_members.csv");
            var bonuses = ReadLubanFile("equipment_set_bonuses.csv");
            var buildIds = new[]
            {
                BuildLootResolver.EmberSetId,
                BuildLootResolver.TideSetId,
                BuildLootResolver.GaleSetId,
                BuildLootResolver.SeismicSetId
            };

            foreach (var buildId in buildIds)
            {
                Assert.That(
                    sets.Any(line => line.Contains("," + buildId + ",")),
                    Is.True,
                    buildId + " should be registered");
                Assert.That(
                    members.Count(line => line.EndsWith("," + buildId)),
                    Is.EqualTo(5),
                    buildId + " should have five members");
                Assert.That(
                    bonuses.Any(line => line.Contains("," + buildId + ",2,")),
                    Is.True,
                    buildId + " should have a 2-piece bonus");
                Assert.That(
                    bonuses.Any(line => line.Contains("," + buildId + ",4,")),
                    Is.True,
                    buildId + " should have a 4-piece bonus");
                Assert.That(
                    bonuses.Any(line => line.Contains("," + buildId + ",5,")),
                    Is.True,
                    buildId + " should have a 5-piece bonus");
            }
        }

        [Test]
        public void Resolve_IsStableAndReturnsAnItemFromInventoryCatalog()
        {
            var resolver = CreateResolver();
            var first = resolver.Resolve("human_duelist", "duelist_001");
            var second = resolver.Resolve("human_duelist", "duelist_001");

            Assert.That(first.IsValid, Is.True);
            Assert.That(first.ItemId, Is.EqualTo(second.ItemId));
            Assert.That(first.Quantity, Is.EqualTo(second.Quantity));
            Assert.That(first.BuildSetId, Is.EqualTo(BuildLootResolver.GaleSetId));
            Assert.That(
                new[]
                {
                    "gale_fang", "skyward_frame", "zephyr_steps",
                    "aero_visor", "cyclone_charm", "gale_grenade",
                    "neon_capacitor", "training_chip"
                },
                Does.Contain(first.ItemId));
        }

        [Test]
        public void ResolveBuildSetId_CoversCanonicalEnemyArchetypes()
        {
            var resolver = CreateResolver();
            var knownBuilds = new[]
            {
                BuildLootResolver.EmberSetId,
                BuildLootResolver.TideSetId,
                BuildLootResolver.GaleSetId,
                BuildLootResolver.SeismicSetId
            };
            var archetypes = ReadLubanFile("enemy_archetypes.csv");

            foreach (var line in archetypes)
            {
                var columns = line.Split(',');
                if (columns.Length < 2 || string.IsNullOrWhiteSpace(columns[1]))
                {
                    continue;
                }

                var setId = resolver.ResolveBuildSetId(columns[1]);
                Assert.That(
                    knownBuilds,
                    Does.Contain(setId),
                    columns[1] + " should map to a build");
            }
        }

        [Test]
        public void ResolveBuildSetId_UnknownArchetypeUsesStableRotation()
        {
            var resolver = CreateResolver();

            var first = resolver.ResolveBuildSetId("future_enemy_alpha");
            var second = resolver.ResolveBuildSetId("future_enemy_alpha");

            Assert.That(first, Is.EqualTo(second));
            Assert.That(
                new[]
                {
                    BuildLootResolver.EmberSetId,
                    BuildLootResolver.TideSetId,
                    BuildLootResolver.GaleSetId,
                    BuildLootResolver.SeismicSetId
                },
                Does.Contain(first));
        }

        [Test]
        public void Resolve_BossGuaranteesHighQualityMappedBuildEquipment()
        {
            var resolver = CreateResolver();
            var result = resolver.Resolve("boss_rusk", "boss_001");

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsEquipment, Is.True);
            Assert.That(result.BuildSetId, Is.EqualTo(BuildLootResolver.SeismicSetId));
            Assert.That(result.Quality, Is.GreaterThanOrEqualTo((int)EquipmentRarity.Epic));
            Assert.That(
                new[]
                {
                    "seismic_maul", "granite_bastion", "faultline_greaves",
                    "bedrock_helm", "geode_seal"
                },
                Does.Contain(result.ItemId));
        }

        [Test]
        public void Resolve_CommonFallbacksAreNotAlwaysTrainingChip()
        {
            var resolver = CreateResolver(includeEquipment: false);
            var itemIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var archetype in new[]
                     { "kaykit_knight", "human_caster", "human_duelist", "human_brute" })
            {
                itemIds.Add(resolver.Resolve(archetype, archetype + "_001").ItemId);
            }

            Assert.That(itemIds.Count, Is.GreaterThan(1));
            Assert.That(itemIds, Does.Not.Contain(string.Empty));
        }

        [Test]
        public void Resolve_RejectsEquipmentMissingFromInventoryCatalog()
        {
            var equipment = CreateEquipment(
                "geode_seal",
                BuildLootResolver.SeismicSetId,
                EquipmentRarity.Legendary);
            var inventory = new List<ItemDefinition>
            {
                CreateResource("upgrade_module", ItemCategory.Material)
            };
            var resolver = new BuildLootResolver(
                new[] { equipment },
                inventory);

            var result = resolver.Resolve("boss_rusk", "boss_missing_inventory");

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsEquipment, Is.False);
            Assert.That(result.ItemId, Is.EqualTo("upgrade_module"));
        }

        private BuildLootResolver CreateResolver(bool includeEquipment = true)
        {
            var equipment = new List<EquipmentItemDefinition>();
            var inventory = new List<ItemDefinition>();
            var builds = new[]
            {
                new BuildInput(
                    BuildLootResolver.EmberSetId,
                    new[] { "ember_edge", "cinder_guard", "flare_steps", "ash_visor", "phoenix_core" }),
                new BuildInput(
                    BuildLootResolver.TideSetId,
                    new[] { "tide_blade", "abyssal_shell", "ripple_boots", "mist_visor", "aqua_orbit" }),
                new BuildInput(
                    BuildLootResolver.GaleSetId,
                    new[] { "gale_fang", "skyward_frame", "zephyr_steps", "aero_visor", "cyclone_charm" }),
                new BuildInput(
                    BuildLootResolver.SeismicSetId,
                    new[] { "seismic_maul", "granite_bastion", "faultline_greaves", "bedrock_helm", "geode_seal" })
            };

            foreach (var build in builds)
            {
                for (var i = 0; i < build.ItemIds.Length; i++)
                {
                    var quality = i == 4
                        ? EquipmentRarity.Legendary
                        : EquipmentRarity.Epic;
                    var definition = CreateEquipment(
                        build.ItemIds[i],
                        build.SetId,
                        quality);
                    if (includeEquipment)
                    {
                        equipment.Add(definition);
                        inventory.Add(CreateItem(definition.ItemId, ItemCategory.Equipment));
                    }
                }
            }

            inventory.Add(CreateResource("training_chip", ItemCategory.Material));
            inventory.Add(CreateResource("upgrade_module", ItemCategory.Material));
            inventory.Add(CreateResource("neon_capacitor", ItemCategory.Material));
            inventory.Add(CreateResource("healing_canister", ItemCategory.Consumable));
            inventory.Add(CreateResource("flame_grenade", ItemCategory.Consumable));
            inventory.Add(CreateResource("tide_grenade", ItemCategory.Consumable));
            inventory.Add(CreateResource("gale_grenade", ItemCategory.Consumable));
            inventory.Add(CreateResource("seismic_grenade", ItemCategory.Consumable));
            inventory.Add(CreateResource("earth_guard_kit", ItemCategory.Consumable));

            return new BuildLootResolver(equipment, inventory);
        }

        private EquipmentItemDefinition CreateEquipment(
            string itemId,
            string setId,
            EquipmentRarity rarity)
        {
            var definition = EquipmentItemDefinition.CreateRuntime(
                itemId,
                itemId,
                itemId,
                rarity,
                EquipmentItemCategory.Accessory,
                Array.Empty<EquipmentStatModifierDefinition>(),
                setId);
            _createdObjects.Add(definition);
            return definition;
        }

        private ItemDefinition CreateItem(string itemId, ItemCategory category)
        {
            var definition = ItemDefinition.CreateRuntime(
                itemId,
                itemId,
                itemId,
                category,
                ItemRarity.Epic,
                1,
                string.Empty);
            _createdObjects.Add(definition);
            return definition;
        }

        private ItemDefinition CreateResource(string itemId, ItemCategory category)
        {
            var definition = ItemDefinition.CreateRuntime(
                itemId,
                itemId,
                itemId,
                category,
                ItemRarity.Rare,
                99,
                string.Empty);
            _createdObjects.Add(definition);
            return definition;
        }

        private static List<string> ReadLubanFile(string fileName)
        {
            var path = Path.Combine(
                Application.dataPath,
                "Config",
                "Luban",
                "Data",
                fileName);
            return File.ReadAllLines(path)
                .Where(line => !line.StartsWith("##", StringComparison.Ordinal))
                .ToList();
        }

        private readonly struct BuildInput
        {
            public BuildInput(string setId, string[] itemIds)
            {
                SetId = setId;
                ItemIds = itemIds;
            }

            public string SetId { get; }
            public string[] ItemIds { get; }
        }
    }
}
