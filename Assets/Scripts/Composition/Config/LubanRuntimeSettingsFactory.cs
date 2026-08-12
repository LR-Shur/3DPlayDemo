using System;
using System.Collections.Generic;
using Train.Architecture.Assets;
using Train.Equipment.Core;
using Train.Equipment.Data;
using Train.Inventory.Data;
using UnityEngine;

namespace Train.Composition.Config
{
    /// <summary>
    /// 将 Luban 行转换为领域服务现有的只读配置对象。
    /// 转换发生在组合层，Inventory/Equipment 模块不需要依赖 Luban 运行时。
    /// </summary>
    public static class LubanRuntimeSettingsFactory
    {
        /// <summary>根据 Luban 物品表生成背包运行时配置。</summary>
        public static InventorySettings CreateInventorySettings(
            cfg.Tables tables,
            InventorySettings baseline)
        {
            if (tables == null)
            {
                throw new ArgumentNullException(nameof(tables));
            }

            var items = new List<ItemDefinition>();
            var activeItems = new Dictionary<string, cfg.game.ActiveItem>(
                StringComparer.Ordinal);
            foreach (var activeItem in tables.TbActiveItem.DataList)
            {
                if (activeItem != null &&
                    !string.IsNullOrWhiteSpace(activeItem.ItemId) &&
                    !activeItems.ContainsKey(activeItem.ItemId))
                {
                    activeItems.Add(activeItem.ItemId, activeItem);
                }
            }

            foreach (var row in tables.TbItem.DataList)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.Id))
                {
                    continue;
                }

                activeItems.TryGetValue(row.Id, out var activeItem);
                items.Add(ItemDefinition.CreateRuntime(
                    row.Id,
                    row.Name,
                    row.Description,
                    ToItemCategory(row.Category),
                    ToItemRarity(row.Quality),
                    row.MaxStack,
                    row.Icon,
                    activeItem == null
                        ? ActiveItemEffectType.None
                        : ToActiveItemEffect(activeItem.Effect),
                    activeItem != null ? activeItem.Value : 0f,
                    activeItem != null ? activeItem.Cooldown : 0f));
            }

            return InventorySettings.CreateRuntime(
                baseline != null ? baseline.Capacity : 36,
                items);
        }

        /// <summary>根据 Luban 装备表生成装备运行时配置。</summary>
        public static EquipmentSettings CreateEquipmentSettings(
            cfg.Tables tables,
            EquipmentSettings baseline)
        {
            if (tables == null)
            {
                throw new ArgumentNullException(nameof(tables));
            }

            var baseStats = new List<EquipmentBaseStatDefinition>();
            if (baseline != null)
            {
                foreach (var stat in baseline.BaseStats)
                {
                    if (stat != null)
                    {
                        baseStats.Add(
                            EquipmentBaseStatDefinition.CreateRuntime(
                                stat.StatType,
                                stat.Value));
                    }
                }
            }

            var items = new List<EquipmentItemDefinition>();
            var effectsByEquipment = new Dictionary<string, cfg.game.EquipmentEffect>(
                StringComparer.Ordinal);
            foreach (var effect in tables.TbEquipmentEffect.DataList)
            {
                if (effect != null &&
                    !string.IsNullOrWhiteSpace(effect.EquipmentId) &&
                    !effectsByEquipment.ContainsKey(effect.EquipmentId))
                {
                    effectsByEquipment.Add(effect.EquipmentId, effect);
                }
            }

            foreach (var row in tables.TbEquipment.DataList)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.Id))
                {
                    continue;
                }

                var modifiers = new List<EquipmentStatModifierDefinition>();
                AddFlatModifier(modifiers, StatType.Attack, row.Attack);
                AddFlatModifier(modifiers, StatType.Defense, row.Defense);
                AddFlatModifier(modifiers, StatType.MaxHealth, row.MaxHealth);
                effectsByEquipment.TryGetValue(row.Id, out var effect);
                items.Add(EquipmentItemDefinition.CreateRuntime(
                    row.Id,
                    row.Name,
                    ToEquipmentRarity(row.Quality),
                    ToEquipmentCategory(row.Slot),
                    modifiers,
                    effect != null ? effect.Element.ToString() : "NONE",
                    effect != null ? effect.Trigger.ToString() : string.Empty,
                    effect != null ? effect.BuffId : string.Empty,
                    effect != null ? effect.Duration : 0f,
                    effect != null ? effect.Magnitude : 0f,
                    effect != null ? effect.StackAmount : 1,
                    effect != null ? effect.MaxStacks : 1,
                    effect != null ? effect.Cooldown : 0f));
            }

            return EquipmentSettings.CreateRuntime(baseStats, items);
        }

        /// <summary>创建运行时 ScriptableObject 的资源租约，统一回收生成对象。</summary>
        public static IAssetLease<T> CreateLease<T>(T asset, string location)
            where T : UnityEngine.Object
        {
            return new RuntimeSettingsLease<T>(asset, location);
        }

        private static void AddFlatModifier(
            ICollection<EquipmentStatModifierDefinition> modifiers,
            StatType statType,
            int value)
        {
            if (value != 0)
            {
                modifiers.Add(
                    EquipmentStatModifierDefinition.CreateRuntime(
                        statType,
                        StatModifierOperation.Flat,
                        value));
            }
        }

        private static ItemCategory ToItemCategory(cfg.game.EItemCategory category)
        {
            return category switch
            {
                cfg.game.EItemCategory.CONSUMABLE => ItemCategory.Consumable,
                cfg.game.EItemCategory.EQUIPMENT => ItemCategory.Equipment,
                cfg.game.EItemCategory.QUEST => ItemCategory.Quest,
                _ => ItemCategory.Material
            };
        }

        private static ActiveItemEffectType ToActiveItemEffect(
            cfg.game.EActiveItemEffect effect)
        {
            return effect switch
            {
                cfg.game.EActiveItemEffect.HEAL => ActiveItemEffectType.Heal,
                cfg.game.EActiveItemEffect.GRENADE => ActiveItemEffectType.Grenade,
                _ => ActiveItemEffectType.None
            };
        }

        private static ItemRarity ToItemRarity(int quality)
        {
            return Enum.IsDefined(typeof(ItemRarity), quality)
                ? (ItemRarity)quality
                : ItemRarity.Common;
        }

        private static EquipmentRarity ToEquipmentRarity(int quality)
        {
            return Enum.IsDefined(typeof(EquipmentRarity), quality)
                ? (EquipmentRarity)quality
                : EquipmentRarity.Common;
        }

        private static EquipmentItemCategory ToEquipmentCategory(
            cfg.game.EEquipmentSlot slot)
        {
            return slot switch
            {
                cfg.game.EEquipmentSlot.ARMOR => EquipmentItemCategory.Armor,
                cfg.game.EEquipmentSlot.SHOES => EquipmentItemCategory.Shoes,
                cfg.game.EEquipmentSlot.HELMET => EquipmentItemCategory.Helmet,
                cfg.game.EEquipmentSlot.GLOVES => EquipmentItemCategory.Gloves,
                cfg.game.EEquipmentSlot.ACCESSORY => EquipmentItemCategory.Accessory,
                _ => EquipmentItemCategory.Weapon
            };
        }

        private sealed class RuntimeSettingsLease<T> : IAssetLease<T>
            where T : UnityEngine.Object
        {
            private T _asset;

            public RuntimeSettingsLease(T asset, string location)
            {
                _asset = asset ?? throw new ArgumentNullException(nameof(asset));
                Location = location;
            }

            public string Location { get; }
            public T Asset => _asset;
            public bool IsValid => _asset != null;

            public void Dispose()
            {
                if (_asset == null)
                {
                    return;
                }

                UnityEngine.Object.Destroy(_asset);
                _asset = null;
            }
        }
    }
}
