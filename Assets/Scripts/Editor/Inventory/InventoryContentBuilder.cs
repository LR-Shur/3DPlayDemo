#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Train.Equipment.Data;
using Train.Inventory.Data;
using UnityEditor;
using UnityEngine;

namespace Train.EditorTools.Inventory
{
    /// <summary>
    /// 以固定资产路径构建背包基础物品，并自动把装备目录同步为不可堆叠背包物品。
    /// 重复执行只会更新既有资产，不会创建重复物品。
    /// </summary>
    public static class InventoryContentBuilder
    {
        private const string RootFolder = "Assets/Data/Inventory";
        private const string ItemFolder = RootFolder + "/Items";
        private const string EquipmentItemFolder = "Assets/Data/Equipment/Items";
        private const string SettingsPath =
            RootFolder + "/DefaultInventorySettings.asset";

        /// <summary>构建背包基础内容并同步当前已经生成的装备目录。</summary>
        [MenuItem("Tools/Train/Content/Build Inventory Content")]
        public static void Build()
        {
            EnsureFolder(RootFolder);
            EnsureFolder(ItemFolder);

            var items = new List<ItemDefinition>
            {
                CreateOrUpdateItem(
                    "training_chip",
                    "训练芯片",
                    "用于基础能力训练的通用数据芯片。",
                    ItemCategory.Material,
                    ItemRarity.Common,
                    99,
                    string.Empty),
                CreateOrUpdateItem(
                    "healing_canister",
                    "应急治疗罐",
                    "便携式治疗补给，可在战斗间隙恢复状态。",
                    ItemCategory.Consumable,
                    ItemRarity.Rare,
                    20,
                    string.Empty),
                CreateOrUpdateItem(
                    "city_token",
                    "都市代币",
                    "训练城区内流通的通用代币。",
                    ItemCategory.Currency,
                    ItemRarity.Elite,
                    9999,
                    string.Empty),
                CreateOrUpdateItem(
                    "upgrade_module",
                    "高能升级模组",
                    "稀有的装备强化组件，蕴含高密度能量。",
                    ItemCategory.Material,
                    ItemRarity.Epic,
                    10,
                    string.Empty)
            };

            var seeds = new List<StartingItemSeed>
            {
                new("training_chip", 8),
                new("healing_canister", 3),
                new("city_token", 120),
                new("upgrade_module", 1)
            };

            foreach (var equipment in LoadEquipmentCatalog())
            {
                var iconPath = equipment.Icon == null
                    ? string.Empty
                    : AssetDatabase.GetAssetPath(equipment.Icon);
                items.Add(
                    CreateOrUpdateItem(
                        equipment.ItemId,
                        equipment.DisplayName,
                        equipment.Description,
                        ItemCategory.Equipment,
                        ConvertRarity(equipment.Rarity),
                        1,
                        iconPath));
                seeds.Add(new StartingItemSeed(equipment.ItemId, 1));
            }

            var settings =
                AssetDatabase.LoadAssetAtPath<InventorySettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<InventorySettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            var serialized = new SerializedObject(settings);
            serialized.FindProperty("_capacity").intValue = 36;

            var itemList = serialized.FindProperty("_items");
            itemList.arraySize = items.Count;
            for (var index = 0; index < items.Count; index++)
            {
                itemList.GetArrayElementAtIndex(index).objectReferenceValue =
                    items[index];
            }

            var startingItems = serialized.FindProperty("_startingItems");
            startingItems.arraySize = seeds.Count;
            for (var index = 0; index < seeds.Count; index++)
            {
                var element = startingItems.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("_itemId").stringValue =
                    seeds[index].ItemId;
                element.FindPropertyRelative("_count").intValue =
                    seeds[index].Count;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"Inventory content built: {items.Count} items, " +
                $"{seeds.Count} starting entries, capacity 36, " +
                $"settings '{SettingsPath}'.");
        }

        private static IReadOnlyList<EquipmentItemDefinition>
            LoadEquipmentCatalog()
        {
            if (!AssetDatabase.IsValidFolder(EquipmentItemFolder))
            {
                return Array.Empty<EquipmentItemDefinition>();
            }

            return AssetDatabase
                .FindAssets(
                    "t:EquipmentItemDefinition",
                    new[] { EquipmentItemFolder })
                .Select(
                    guid => AssetDatabase.LoadAssetAtPath<
                        EquipmentItemDefinition>(
                        AssetDatabase.GUIDToAssetPath(guid)))
                .Where(definition => definition != null)
                .OrderBy(
                    definition => definition.ItemId,
                    StringComparer.Ordinal)
                .ToArray();
        }

        private static ItemRarity ConvertRarity(
            EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Legendary => ItemRarity.Legendary,
                EquipmentRarity.Epic => ItemRarity.Epic,
                EquipmentRarity.Elite => ItemRarity.Elite,
                EquipmentRarity.Rare => ItemRarity.Rare,
                _ => ItemRarity.Common
            };
        }

        private static ItemDefinition CreateOrUpdateItem(
            string itemId,
            string displayName,
            string description,
            ItemCategory category,
            ItemRarity rarity,
            int maxStack,
            string iconLocation)
        {
            var path = $"{ItemFolder}/{itemId}.asset";
            var definition =
                AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<ItemDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("_itemId").stringValue = itemId;
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_description").stringValue = description;
            serialized.FindProperty("_category").enumValueIndex =
                (int)category;
            serialized.FindProperty("_rarity").enumValueIndex =
                (int)rarity - 1;
            serialized.FindProperty("_maxStack").intValue = maxStack;
            serialized.FindProperty("_iconLocation").stringValue =
                iconLocation ?? string.Empty;
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

        /// <summary>
        /// 表示构建背包设置时写入的一条初始物品记录。
        /// </summary>
        private readonly struct StartingItemSeed
        {
            /// <summary>创建一条初始物品记录。</summary>
            public StartingItemSeed(string itemId, int count)
            {
                ItemId = itemId;
                Count = count;
            }

            /// <summary>获取物品稳定标识。</summary>
            public string ItemId { get; }

            /// <summary>获取初始持有数量。</summary>
            public int Count { get; }
        }
    }
}
#endif
