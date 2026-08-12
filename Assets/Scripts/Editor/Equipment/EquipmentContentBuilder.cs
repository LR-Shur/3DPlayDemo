#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Train.EditorTools.Inventory;
using Train.Equipment.Core;
using Train.Equipment.Data;
using UnityEditor;
using UnityEngine;

namespace Train.EditorTools.Equipment
{
    /// <summary>
    /// 以固定 ID 和固定资产路径构建装备、套装、初始穿戴和配套背包内容。
    /// 该构建过程可重复执行，既有资产只会被更新而不会产生重复副本。
    /// </summary>
    public static class EquipmentContentBuilder
    {
        private const string RootFolder = "Assets/Data/Equipment";
        private const string ItemFolder = RootFolder + "/Items";
        private const string SettingsPath =
            RootFolder + "/DefaultEquipmentSettings.asset";
        private const string IconFolder =
            "Assets/Arts/UI/Icons/GameIcons";

        /// <summary>
        /// 构建十八件装备、两套套装、默认装备设置，并同步重建背包内容。
        /// </summary>
        [MenuItem("Tools/Train/Content/Build Equipment Content")]
        public static void Build()
        {
            EnsureFolder(RootFolder);
            EnsureFolder(ItemFolder);

            var seeds = CreateEquipmentSeeds();
            var items = new EquipmentItemDefinition[seeds.Length];
            var itemById =
                new Dictionary<string, EquipmentItemDefinition>(
                    StringComparer.Ordinal);
            for (var i = 0; i < seeds.Length; i++)
            {
                var item = CreateOrUpdateItem(seeds[i]);
                items[i] = item;
                itemById.Add(item.ItemId, item);
            }

            var settings =
                AssetDatabase.LoadAssetAtPath<EquipmentSettings>(SettingsPath);
            if (settings == null)
            {
                settings =
                    ScriptableObject.CreateInstance<EquipmentSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            ConfigureSettings(
                settings,
                items,
                Array.Empty<EquipmentSetDefinition>(),
                itemById);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            InventoryContentBuilder.Build();

            Debug.Log(
                $"Equipment content built: {items.Length} items, 2 sets, " +
                $"0 starting equipment entries, settings '{SettingsPath}'.");
        }

        private static EquipmentSeed[] CreateEquipmentSeeds()
        {
            return new[]
            {
                new EquipmentSeed(
                    "thunder_blade",
                    "雷鸣刃",
                    "将高压电荷压缩于刃脊的制式长剑，挥击时留下蓝白电弧。",
                    EquipmentItemCategory.Weapon,
                    EquipmentRarity.Legendary,
                    "storm_protocol",
                    "lightning-saber.png",
                    new[]
                    {
                        Modifier(
                            StatType.Attack,
                            StatModifierOperation.Flat,
                            35f),
                        Modifier(
                            StatType.ElectricDamageBonus,
                            StatModifierOperation.Flat,
                            0.08f)
                    }),
                new EquipmentSeed(
                    "streetbreaker_blade",
                    "街卫破阵刀",
                    "街区守卫常用的重型破阵刀，可靠且易于维护。",
                    EquipmentItemCategory.Weapon,
                    EquipmentRarity.Epic,
                    "street_guard",
                    "orbital.png",
                    new[]
                    {
                        Modifier(
                            StatType.Attack,
                            StatModifierOperation.Flat,
                            30f),
                        Modifier(
                            StatType.CritRate,
                            StatModifierOperation.Flat,
                            0.03f)
                    }),
                new EquipmentSeed(
                    "neon_goggles",
                    "霓虹目镜",
                    "能够标记电荷轨迹的战术目镜，适合高速追击。",
                    EquipmentItemCategory.Helmet,
                    EquipmentRarity.Epic,
                    "storm_protocol",
                    "cyber-eye.png",
                    new[]
                    {
                        Modifier(
                            StatType.CritRate,
                            StatModifierOperation.Flat,
                            0.05f),
                        Modifier(
                            StatType.ElectricDamageBonus,
                            StatModifierOperation.Flat,
                            0.04f)
                    }),
                new EquipmentSeed(
                    "street_guard_helmet",
                    "街卫战术盔",
                    "带有缓冲层和简易通信模组的街区巡逻头盔。",
                    EquipmentItemCategory.Helmet,
                    EquipmentRarity.Rare,
                    "street_guard",
                    "cyber-eye.png",
                    new[]
                    {
                        Modifier(
                            StatType.Defense,
                            StatModifierOperation.Flat,
                            20f),
                        Modifier(
                            StatType.MaxHealth,
                            StatModifierOperation.Flat,
                            80f)
                    }),
                new EquipmentSeed(
                    "volt_weave_armor",
                    "伏特织甲",
                    "以导电纤维编织的轻型战甲，可快速疏导冲击电流。",
                    EquipmentItemCategory.Armor,
                    EquipmentRarity.Epic,
                    "storm_protocol",
                    "electric.png",
                    new[]
                    {
                        Modifier(
                            StatType.MaxHealth,
                            StatModifierOperation.Flat,
                            160f),
                        Modifier(
                            StatType.Defense,
                            StatModifierOperation.Flat,
                            18f)
                    }),
                new EquipmentSeed(
                    "street_guard_armor",
                    "街卫防护衣",
                    "覆盖关键部位的模块化防护衣，强调持久作战能力。",
                    EquipmentItemCategory.Armor,
                    EquipmentRarity.Epic,
                    "street_guard",
                    "orbital.png",
                    new[]
                    {
                        Modifier(
                            StatType.MaxHealth,
                            StatModifierOperation.Flat,
                            220f),
                        Modifier(
                            StatType.Defense,
                            StatModifierOperation.Flat,
                            25f)
                    }),
                new EquipmentSeed(
                    "arc_gloves",
                    "电弧手套",
                    "掌心电极会在命中瞬间释放短促电弧。",
                    EquipmentItemCategory.Gloves,
                    EquipmentRarity.Epic,
                    "storm_protocol",
                    "electric.png",
                    new[]
                    {
                        Modifier(
                            StatType.Attack,
                            StatModifierOperation.Flat,
                            16f),
                        Modifier(
                            StatType.CritDamage,
                            StatModifierOperation.Flat,
                            0.12f)
                    }),
                new EquipmentSeed(
                    "street_guard_gloves",
                    "街卫重拳手套",
                    "加厚关节和腕部支撑让近身格斗更加稳定。",
                    EquipmentItemCategory.Gloves,
                    EquipmentRarity.Rare,
                    "street_guard",
                    "gem-chain.png",
                    new[]
                    {
                        Modifier(
                            StatType.Attack,
                            StatModifierOperation.Flat,
                            12f),
                        Modifier(
                            StatType.Defense,
                            StatModifierOperation.Flat,
                            12f)
                    }),
                new EquipmentSeed(
                    "flashstep_boots",
                    "闪步战靴",
                    "微型脉冲推进器可在起步时提供瞬时加速。",
                    EquipmentItemCategory.Shoes,
                    EquipmentRarity.Epic,
                    "storm_protocol",
                    "orbital.png",
                    new[]
                    {
                        Modifier(
                            StatType.CritRate,
                            StatModifierOperation.Flat,
                            0.04f),
                        Modifier(
                            StatType.Attack,
                            StatModifierOperation.AdditivePercent,
                            0.05f)
                    }),
                new EquipmentSeed(
                    "street_guard_boots",
                    "街卫机动靴",
                    "适合长距离巡逻的耐磨战靴，防滑且稳定。",
                    EquipmentItemCategory.Shoes,
                    EquipmentRarity.Rare,
                    "street_guard",
                    "gem-chain.png",
                    new[]
                    {
                        Modifier(
                            StatType.Defense,
                            StatModifierOperation.Flat,
                            15f),
                        Modifier(
                            StatType.MaxHealth,
                            StatModifierOperation.Flat,
                            100f)
                    }),
                new EquipmentSeed(
                    "thunder_ring",
                    "雷环",
                    "封存微型雷暴的指环，会随持有者的攻击节奏闪烁。",
                    EquipmentItemCategory.Accessory,
                    EquipmentRarity.Legendary,
                    "storm_protocol",
                    "globe-ring.png",
                    new[]
                    {
                        Modifier(
                            StatType.ElectricDamageBonus,
                            StatModifierOperation.Flat,
                            0.1f)
                    }),
                new EquipmentSeed(
                    "induction_choker",
                    "感应颈环",
                    "监测神经反应并提前释放微弱刺激的战术颈环。",
                    EquipmentItemCategory.Accessory,
                    EquipmentRarity.Epic,
                    "storm_protocol",
                    "intricate-necklace.png",
                    new[]
                    {
                        Modifier(
                            StatType.CritRate,
                            StatModifierOperation.Flat,
                            0.04f)
                    }),
                new EquipmentSeed(
                    "charged_orbit",
                    "充能轨道仪",
                    "环绕核心运转的电荷稳定器，可提高连续输出。",
                    EquipmentItemCategory.Accessory,
                    EquipmentRarity.Epic,
                    "storm_protocol",
                    "orbital.png",
                    new[]
                    {
                        Modifier(
                            StatType.Attack,
                            StatModifierOperation.AdditivePercent,
                            0.08f)
                    }),
                new EquipmentSeed(
                    "neon_capacitor",
                    "霓虹电容",
                    "经过霓虹涂装的小型高密度电容，仍带着余温。",
                    EquipmentItemCategory.Accessory,
                    EquipmentRarity.Rare,
                    "storm_protocol",
                    "electric.png",
                    new[]
                    {
                        Modifier(
                            StatType.Attack,
                            StatModifierOperation.Flat,
                            18f)
                    }),
                new EquipmentSeed(
                    "guard_badge",
                    "街卫徽章",
                    "记录巡逻功绩的金属徽章，背面刻有旧城区编号。",
                    EquipmentItemCategory.Accessory,
                    EquipmentRarity.Epic,
                    "street_guard",
                    "gem-necklace.png",
                    new[]
                    {
                        Modifier(
                            StatType.Defense,
                            StatModifierOperation.Flat,
                            18f)
                    }),
                new EquipmentSeed(
                    "reinforced_chain",
                    "加固链扣",
                    "由回收合金制成的链扣，能够分散正面冲击。",
                    EquipmentItemCategory.Accessory,
                    EquipmentRarity.Rare,
                    "street_guard",
                    "gem-chain.png",
                    new[]
                    {
                        Modifier(
                            StatType.MaxHealth,
                            StatModifierOperation.Flat,
                            140f)
                    }),
                new EquipmentSeed(
                    "patrol_compass",
                    "巡逻罗盘",
                    "保存着街区安全路线的电子罗盘，指针从不迟疑。",
                    EquipmentItemCategory.Accessory,
                    EquipmentRarity.Rare,
                    "street_guard",
                    "globe-ring.png",
                    new[]
                    {
                        Modifier(
                            StatType.Defense,
                            StatModifierOperation.AdditivePercent,
                            0.08f)
                    }),
                new EquipmentSeed(
                    "emergency_core",
                    "应急护芯",
                    "在护盾失效前释放备用能量的便携防护核心。",
                    EquipmentItemCategory.Accessory,
                    EquipmentRarity.Epic,
                    "street_guard",
                    "gem-necklace.png",
                    new[]
                    {
                        Modifier(
                            StatType.MaxHealth,
                            StatModifierOperation.AdditivePercent,
                            0.08f)
                    })
            };
        }

        private static void ConfigureSettings(
            EquipmentSettings settings,
            IReadOnlyList<EquipmentItemDefinition> items,
            IReadOnlyList<EquipmentSetDefinition> sets,
            IReadOnlyDictionary<string, EquipmentItemDefinition> itemById)
        {
            var serialized = new SerializedObject(settings);

            var baseStats = serialized.FindProperty("_baseStats");
            var baseSeeds = new[]
            {
                new BaseStatSeed(StatType.MaxHealth, 1000f),
                new BaseStatSeed(StatType.Attack, 100f),
                new BaseStatSeed(StatType.Defense, 50f),
                new BaseStatSeed(StatType.CritRate, 0.05f),
                new BaseStatSeed(StatType.CritDamage, 0.5f),
                new BaseStatSeed(StatType.ElectricDamageBonus, 0f)
            };
            baseStats.arraySize = baseSeeds.Length;
            for (var i = 0; i < baseSeeds.Length; i++)
            {
                var element = baseStats.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_statType").enumValueIndex =
                    (int)baseSeeds[i].StatType;
                element.FindPropertyRelative("_value").floatValue =
                    baseSeeds[i].Value;
            }

            var itemList = serialized.FindProperty("_items");
            itemList.arraySize = items.Count;
            for (var i = 0; i < items.Count; i++)
            {
                itemList.GetArrayElementAtIndex(i).objectReferenceValue =
                    items[i];
            }

            var setList = serialized.FindProperty("_sets");
            setList.arraySize = sets.Count;
            for (var i = 0; i < sets.Count; i++)
            {
                setList.GetArrayElementAtIndex(i).objectReferenceValue =
                    sets[i];
            }

            // 新 Run 不预穿任何装备，装备通过掉落、任务和商店获得。
            var startingList =
                serialized.FindProperty("_startingEquipment");
            startingList.arraySize = 0;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static EquipmentItemDefinition CreateOrUpdateItem(
            EquipmentSeed seed)
        {
            var path = $"{ItemFolder}/{seed.ItemId}.asset";
            var item =
                AssetDatabase.LoadAssetAtPath<EquipmentItemDefinition>(path);
            if (item == null)
            {
                item =
                    ScriptableObject.CreateInstance<EquipmentItemDefinition>();
                AssetDatabase.CreateAsset(item, path);
            }

            var serialized = new SerializedObject(item);
            serialized.FindProperty("_itemId").stringValue = seed.ItemId;
            serialized.FindProperty("_displayName").stringValue =
                seed.DisplayName;
            serialized.FindProperty("_description").stringValue =
                seed.Description;
            serialized.FindProperty("_rarity").enumValueIndex =
                (int)seed.Rarity - 1;
            serialized.FindProperty("_category").enumValueIndex =
                (int)seed.Category;
            // 套装关系由 Luban 的 equipment_set_members.csv 管理，
            // 静态 SO 不再写入第二份套装来源。
            serialized.FindProperty("_setId").stringValue = string.Empty;
            serialized.FindProperty("_icon").objectReferenceValue =
                LoadSprite(seed.IconFileName);
            WriteModifiers(
                serialized.FindProperty("_modifiers"),
                seed.Modifiers);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
            return item;
        }

        private static void WriteModifiers(
            SerializedProperty target,
            IReadOnlyList<ModifierSeed> modifiers)
        {
            target.arraySize = modifiers.Count;
            for (var i = 0; i < modifiers.Count; i++)
            {
                var element = target.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_statType").enumValueIndex =
                    (int)modifiers[i].StatType;
                element.FindPropertyRelative("_operation").enumValueIndex =
                    (int)modifiers[i].Operation;
                element.FindPropertyRelative("_value").floatValue =
                    modifiers[i].Value;
            }
        }

        private static Sprite LoadSprite(string fileName)
        {
            var path = $"{IconFolder}/{fileName}";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    $"装备图标不存在或不是纹理：'{path}'。");
            }

            var changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path) ??
                   throw new InvalidOperationException(
                       $"无法以 Sprite 读取装备图标：'{path}'。");
        }

        private static ModifierSeed Modifier(
            StatType statType,
            StatModifierOperation operation,
            float value)
        {
            return new ModifierSeed(statType, operation, value);
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

        /// <summary>保存构建一件装备资产所需的确定性内容。</summary>
        private readonly struct EquipmentSeed
        {
            /// <summary>创建一件装备内容记录。</summary>
            public EquipmentSeed(
                string itemId,
                string displayName,
                string description,
                EquipmentItemCategory category,
                EquipmentRarity rarity,
                string setId,
                string iconFileName,
                IReadOnlyList<ModifierSeed> modifiers)
            {
                ItemId = itemId;
                DisplayName = displayName;
                Description = description;
                Category = category;
                Rarity = rarity;
                SetId = setId;
                IconFileName = iconFileName;
                Modifiers = modifiers;
            }

            /// <summary>获取装备稳定标识。</summary>
            public string ItemId { get; }

            /// <summary>获取装备显示名称。</summary>
            public string DisplayName { get; }

            /// <summary>获取装备说明。</summary>
            public string Description { get; }

            /// <summary>获取装备类别。</summary>
            public EquipmentItemCategory Category { get; }

            /// <summary>获取装备品质。</summary>
            public EquipmentRarity Rarity { get; }

            /// <summary>获取套装稳定标识。</summary>
            public string SetId { get; }

            /// <summary>获取图标文件名。</summary>
            public string IconFileName { get; }

            /// <summary>获取属性词条。</summary>
            public IReadOnlyList<ModifierSeed> Modifiers { get; }
        }

        /// <summary>保存一条待写入 Inspector 配置的属性词条。</summary>
        private readonly struct ModifierSeed
        {
            /// <summary>创建一条属性词条内容记录。</summary>
            public ModifierSeed(
                StatType statType,
                StatModifierOperation operation,
                float value)
            {
                StatType = statType;
                Operation = operation;
                Value = value;
            }

            /// <summary>获取目标属性。</summary>
            public StatType StatType { get; }

            /// <summary>获取运算方式。</summary>
            public StatModifierOperation Operation { get; }

            /// <summary>获取词条数值。</summary>
            public float Value { get; }
        }

        /// <summary>保存一项基础属性配置。</summary>
        private readonly struct BaseStatSeed
        {
            /// <summary>创建一项基础属性内容记录。</summary>
            public BaseStatSeed(StatType statType, float value)
            {
                StatType = statType;
                Value = value;
            }

            /// <summary>获取基础属性类型。</summary>
            public StatType StatType { get; }

            /// <summary>获取基础属性数值。</summary>
            public float Value { get; }
        }

        /// <summary>保存一件初始穿戴装备及目标槽位。</summary>
        private readonly struct StartingSeed
        {
            /// <summary>创建一条初始穿戴内容记录。</summary>
            public StartingSeed(
                string itemId,
                EquipmentSlot targetSlot)
            {
                ItemId = itemId;
                TargetSlot = targetSlot;
            }

            /// <summary>获取装备稳定标识。</summary>
            public string ItemId { get; }

            /// <summary>获取目标槽位。</summary>
            public EquipmentSlot TargetSlot { get; }
        }
    }
}
#endif
