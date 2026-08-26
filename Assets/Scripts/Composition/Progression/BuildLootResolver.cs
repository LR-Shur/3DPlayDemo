using System;
using System.Collections.Generic;
using Train.Equipment.Data;
using Train.Inventory.Data;

namespace Train.Composition.Progression
{
    /// <summary>区分构筑装备掉落和构筑相关资源掉落。</summary>
    public enum BuildLootKind
    {
        Resource = 0,
        Equipment = 1
    }

    /// <summary>
    /// 一次确定性掉落结果。结果中的 ItemId 必须来自背包目录。
    /// </summary>
    public readonly struct BuildLootResult
    {
        public static BuildLootResult Empty =>
            new BuildLootResult(string.Empty, 0, BuildLootKind.Resource, string.Empty, 0);

        public BuildLootResult(
            string itemId,
            int quantity,
            BuildLootKind kind,
            string buildSetId,
            int quality)
        {
            ItemId = itemId ?? string.Empty;
            Quantity = Math.Max(0, quantity);
            Kind = kind;
            BuildSetId = buildSetId ?? string.Empty;
            Quality = quality;
        }

        public string ItemId { get; }
        public int Quantity { get; }
        public BuildLootKind Kind { get; }
        public string BuildSetId { get; }
        public int Quality { get; }
        public bool IsEquipment => Kind == BuildLootKind.Equipment;
        public bool IsValid => !string.IsNullOrWhiteSpace(ItemId) && Quantity > 0;
    }

    /// <summary>
    /// 根据敌人原型和稳定实例 ID 解析构筑掉落。
    /// 该类不访问 Unity 场景、全局随机数或服务定位器，因此可直接做 EditMode 测试。
    /// </summary>
    public sealed class BuildLootResolver
    {
        public const string EmberSetId = "ember_combo";
        public const string TideSetId = "tide_sustain";
        public const string GaleSetId = "gale_mobility";
        public const string SeismicSetId = "seismic_breaker";

        private const float CommonEquipmentChance = 0.18f;
        private const float EliteEquipmentChance = 0.42f;

        private static readonly string[] BuildSetIds =
        {
            EmberSetId,
            TideSetId,
            GaleSetId,
            SeismicSetId
        };

        private static readonly Dictionary<string, string> BuildByArchetype =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "kaykit_knight", EmberSetId },
                { "training_dummy", EmberSetId },
                { "elite_knight", EmberSetId },
                { "human_caster", TideSetId },
                { "arc_drone", TideSetId },
                { "lobber_bot", TideSetId },
                { "scout_knight", GaleSetId },
                { "human_duelist", GaleSetId },
                { "roller_bot", GaleSetId },
                { "human_brute", SeismicSetId },
                { "crawler_bot", SeismicSetId },
                { "overload_sentinel", SeismicSetId },
                { "boss_rusk", SeismicSetId }
            };

        private static readonly Dictionary<string, string[]> PreferredFallbackIds =
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                {
                    EmberSetId,
                    new[] { "flame_grenade", "upgrade_module", "training_chip" }
                },
                {
                    TideSetId,
                    new[] { "tide_grenade", "healing_canister", "upgrade_module" }
                },
                {
                    GaleSetId,
                    new[] { "gale_grenade", "neon_capacitor", "training_chip" }
                },
                {
                    SeismicSetId,
                    new[] { "seismic_grenade", "earth_guard_kit", "upgrade_module" }
                }
            };

        private readonly Dictionary<string, List<EquipmentItemDefinition>>
            _equipmentBySet = new Dictionary<string, List<EquipmentItemDefinition>>(
                StringComparer.Ordinal);
        private readonly List<ItemDefinition> _resources = new List<ItemDefinition>();
        private readonly Dictionary<string, ItemDefinition> _resourcesById =
            new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);

        public BuildLootResolver(
            IReadOnlyList<EquipmentItemDefinition> equipmentCatalog,
            IReadOnlyList<ItemDefinition> inventoryCatalog)
        {
            if (equipmentCatalog == null)
            {
                throw new ArgumentNullException(nameof(equipmentCatalog));
            }

            if (inventoryCatalog == null)
            {
                throw new ArgumentNullException(nameof(inventoryCatalog));
            }

            var legalInventoryIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < inventoryCatalog.Count; i++)
            {
                var item = inventoryCatalog[i];
                if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
                {
                    continue;
                }

                legalInventoryIds.Add(item.ItemId);
                if (item.Category == ItemCategory.Material ||
                    item.Category == ItemCategory.Consumable)
                {
                    if (_resourcesById.TryAdd(item.ItemId, item))
                    {
                        _resources.Add(item);
                    }
                }
            }

            for (var i = 0; i < equipmentCatalog.Count; i++)
            {
                var equipment = equipmentCatalog[i];
                if (equipment == null ||
                    !IsBuildSet(equipment.SetId) ||
                    !legalInventoryIds.Contains(equipment.ItemId))
                {
                    continue;
                }

                if (!_equipmentBySet.TryGetValue(equipment.SetId, out var pool))
                {
                    pool = new List<EquipmentItemDefinition>();
                    _equipmentBySet.Add(equipment.SetId, pool);
                }

                if (!ContainsEquipment(pool, equipment.ItemId))
                {
                    pool.Add(equipment);
                }
            }
        }

        /// <summary>
        /// 获取敌人原型对应的构筑；未知原型按稳定哈希在四套构筑间轮转。
        /// </summary>
        public string ResolveBuildSetId(string enemyArchetypeId)
        {
            if (!string.IsNullOrWhiteSpace(enemyArchetypeId) &&
                BuildByArchetype.TryGetValue(enemyArchetypeId, out var setId))
            {
                return setId;
            }

            var stableId = string.IsNullOrWhiteSpace(enemyArchetypeId)
                ? "unknown"
                : enemyArchetypeId;
            return BuildSetIds[
                (int)(StableHash(stableId) % (uint)BuildSetIds.Length)];
        }

        /// <summary>
        /// 以敌人原型和稳定实例 ID 解析一次掉落；相同输入永远得到相同结果。
        /// </summary>
        public BuildLootResult Resolve(string enemyArchetypeId, string instanceId)
        {
            var archetype = string.IsNullOrWhiteSpace(enemyArchetypeId)
                ? "unknown"
                : enemyArchetypeId;
            var stableInstanceId = string.IsNullOrWhiteSpace(instanceId)
                ? archetype
                : instanceId;
            var buildSetId = ResolveBuildSetId(archetype);

            _equipmentBySet.TryGetValue(buildSetId, out var equipmentPool);
            var tier = ResolveTier(archetype);
            if (equipmentPool != null && equipmentPool.Count > 0 &&
                ShouldDropEquipment(tier, archetype, stableInstanceId))
            {
                var candidatePool = SelectQualityPool(equipmentPool, tier);
                if (candidatePool.Count > 0)
                {
                    var index = (int)(StableHash(
                        archetype + "|" + stableInstanceId + "|equipment|" + buildSetId) %
                        (uint)candidatePool.Count);
                    var equipment = candidatePool[index];
                    return new BuildLootResult(
                        equipment.ItemId,
                        1,
                        BuildLootKind.Equipment,
                        buildSetId,
                        (int)equipment.Rarity);
                }
            }

            return ResolveResource(archetype, stableInstanceId, buildSetId, tier);
        }

        private BuildLootResult ResolveResource(
            string archetype,
            string instanceId,
            string buildSetId,
            EnemyLootTier tier)
        {
            var pool = new List<ItemDefinition>();
            if (PreferredFallbackIds.TryGetValue(buildSetId, out var preferredIds))
            {
                for (var i = 0; i < preferredIds.Length; i++)
                {
                    if (_resourcesById.TryGetValue(preferredIds[i], out var preferred))
                    {
                        pool.Add(preferred);
                    }
                }
            }

            if (pool.Count == 0)
            {
                pool.AddRange(_resources);
            }

            if (pool.Count == 0)
            {
                return BuildLootResult.Empty;
            }

            var index = (int)(StableHash(
                archetype + "|" + instanceId + "|resource|" + buildSetId) %
                (uint)pool.Count);
            var item = pool[index];
            var baseQuantity = tier == EnemyLootTier.Boss
                ? 3
                : tier == EnemyLootTier.Elite
                    ? 2
                    : 1 + (int)(StableHash(instanceId + "|quantity") % 2u);
            return new BuildLootResult(
                item.ItemId,
                Math.Min(item.MaxStack, baseQuantity),
                BuildLootKind.Resource,
                buildSetId,
                (int)item.Rarity);
        }

        private static List<EquipmentItemDefinition> SelectQualityPool(
            List<EquipmentItemDefinition> equipmentPool,
            EnemyLootTier tier)
        {
            if (tier != EnemyLootTier.Boss)
            {
                return equipmentPool;
            }

            var highQuality = new List<EquipmentItemDefinition>();
            var highestQuality = 0;
            for (var i = 0; i < equipmentPool.Count; i++)
            {
                var quality = (int)equipmentPool[i].Rarity;
                highestQuality = Math.Max(highestQuality, quality);
                if (quality >= (int)EquipmentRarity.Epic)
                {
                    highQuality.Add(equipmentPool[i]);
                }
            }

            if (highQuality.Count > 0)
            {
                return highQuality;
            }

            var bestAvailable = new List<EquipmentItemDefinition>();
            for (var i = 0; i < equipmentPool.Count; i++)
            {
                if ((int)equipmentPool[i].Rarity == highestQuality)
                {
                    bestAvailable.Add(equipmentPool[i]);
                }
            }

            return bestAvailable;
        }

        private static bool ShouldDropEquipment(
            EnemyLootTier tier,
            string archetype,
            string instanceId)
        {
            if (tier == EnemyLootTier.Boss)
            {
                return true;
            }

            var chance = tier == EnemyLootTier.Elite
                ? EliteEquipmentChance
                : CommonEquipmentChance;
            var roll = StableHash(archetype + "|" + instanceId + "|equipment-roll") % 10000u;
            return roll < chance * 10000f;
        }

        private static EnemyLootTier ResolveTier(string archetype)
        {
            if (archetype.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return EnemyLootTier.Boss;
            }

            return archetype.IndexOf("elite", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   string.Equals(
                       archetype,
                       "overload_sentinel",
                       StringComparison.OrdinalIgnoreCase)
                ? EnemyLootTier.Elite
                : EnemyLootTier.Common;
        }

        private static bool IsBuildSet(string setId)
        {
            return string.Equals(setId, EmberSetId, StringComparison.Ordinal) ||
                   string.Equals(setId, TideSetId, StringComparison.Ordinal) ||
                   string.Equals(setId, GaleSetId, StringComparison.Ordinal) ||
                   string.Equals(setId, SeismicSetId, StringComparison.Ordinal);
        }

        private static bool ContainsEquipment(
            List<EquipmentItemDefinition> pool,
            string itemId)
        {
            for (var i = 0; i < pool.Count; i++)
            {
                if (string.Equals(pool[i].ItemId, itemId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static uint StableHash(string value)
        {
            unchecked
            {
                var hash = 2166136261u;
                for (var i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= 16777619u;
                }

                return hash;
            }
        }

        private enum EnemyLootTier
        {
            Common = 0,
            Elite = 1,
            Boss = 2
        }
    }
}
