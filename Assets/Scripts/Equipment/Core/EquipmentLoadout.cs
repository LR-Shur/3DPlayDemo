using System;
using System.Collections.Generic;
using System.Linq;

namespace Train.Equipment.Core
{
    /// <summary>
    /// 管理单个角色的装备栏，并依据基础属性、装备词条和套装奖励生成最终属性。
    /// 所有失败或无实际变化的操作都不会修改状态、修订号或发送事件。
    /// </summary>
    public sealed class EquipmentLoadout
    {
        private readonly Dictionary<StatType, float> _baseStats;
        private readonly Dictionary<EquipmentSlot, EquipmentItemSpec>
            _equippedItems;
        private readonly Dictionary<string, EquipmentSetSpec> _setDefinitions;

        public EquipmentLoadout(
            IReadOnlyDictionary<StatType, float> baseStats,
            IEnumerable<EquipmentSetSpec> setDefinitions)
        {
            if (baseStats == null)
            {
                throw new ArgumentNullException(nameof(baseStats));
            }

            if (setDefinitions == null)
            {
                throw new ArgumentNullException(nameof(setDefinitions));
            }

            _baseStats = CopyAndValidateBaseStats(baseStats);
            _setDefinitions = CopyAndValidateSetDefinitions(setDefinitions);
            _equippedItems =
                new Dictionary<EquipmentSlot, EquipmentItemSpec>();
        }

        public event EventHandler<EquipmentChangedEventArgs> Changed;

        public long Revision { get; private set; }

        public EquipmentSnapshot Snapshot => CreateSnapshot();

        public bool Equip(EquipmentItemSpec item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            return Equip(item, item.Slot);
        }

        public bool Equip(
            EquipmentItemSpec item,
            EquipmentSlot targetSlot)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            ValidateSlot(targetSlot);
            ValidateSlotCompatibility(item, targetSlot);

            EquipmentSlot? sourceSlot = null;
            foreach (var pair in _equippedItems)
            {
                if (string.Equals(
                        pair.Value.ItemId,
                        item.ItemId,
                        StringComparison.Ordinal))
                {
                    if (!ReferenceEquals(pair.Value, item))
                    {
                        return false;
                    }

                    sourceSlot = pair.Key;
                    break;
                }
            }

            if (sourceSlot == targetSlot)
            {
                return false;
            }

            _equippedItems.TryGetValue(
                targetSlot,
                out var previousItem);
            var nextRevision = GetNextRevision();
            if (sourceSlot.HasValue)
            {
                _equippedItems.Remove(sourceSlot.Value);
            }

            _equippedItems[targetSlot] = item;
            EquipmentSnapshot snapshot;
            try
            {
                snapshot = CreateSnapshot(nextRevision);
            }
            catch
            {
                if (previousItem == null)
                {
                    _equippedItems.Remove(targetSlot);
                }
                else
                {
                    _equippedItems[targetSlot] = previousItem;
                }

                if (sourceSlot.HasValue)
                {
                    _equippedItems[sourceSlot.Value] = item;
                }

                throw;
            }

            CommitChange(
                sourceSlot.HasValue
                    ? EquipmentChangeKind.Moved
                    : EquipmentChangeKind.Equipped,
                targetSlot,
                sourceSlot,
                previousItem,
                item,
                nextRevision,
                snapshot);
            return true;
        }

        public bool Unequip(EquipmentSlot slot)
        {
            ValidateSlot(slot);
            if (!_equippedItems.TryGetValue(slot, out var previousItem))
            {
                return false;
            }

            var nextRevision = GetNextRevision();
            _equippedItems.Remove(slot);
            EquipmentSnapshot snapshot;
            try
            {
                snapshot = CreateSnapshot(nextRevision);
            }
            catch
            {
                _equippedItems[slot] = previousItem;
                throw;
            }

            CommitChange(
                EquipmentChangeKind.Unequipped,
                slot,
                slot,
                previousItem,
                null,
                nextRevision,
                snapshot);
            return true;
        }

        public EquipmentSnapshot CreateSnapshot()
        {
            return CreateSnapshot(Revision);
        }

        private EquipmentSnapshot CreateSnapshot(long revision)
        {
            var slots = CreateSlotSnapshots();
            var activeSets = CreateActivatedSetsAndCollectModifiers(
                out var setModifiers);
            var allModifiers = CollectEquipmentModifiers();
            allModifiers.AddRange(setModifiers);
            var finalStats = CalculateFinalStats(allModifiers);

            return new EquipmentSnapshot(
                revision,
                slots,
                _baseStats,
                finalStats,
                activeSets);
        }

        private EquipmentSlotSnapshot[] CreateSlotSnapshots()
        {
            var definedSlots = GetDefinedEnumValues<EquipmentSlot>();
            var slots = new EquipmentSlotSnapshot[definedSlots.Length];
            for (var i = 0; i < definedSlots.Length; i++)
            {
                var slot = definedSlots[i];
                _equippedItems.TryGetValue(slot, out var item);
                slots[i] = new EquipmentSlotSnapshot(slot, item);
            }

            return slots;
        }

        private ActivatedEquipmentSetSnapshot[]
            CreateActivatedSetsAndCollectModifiers(
                out List<StatModifier> activeModifiers)
        {
            activeModifiers = new List<StatModifier>();
            var pieceCounts = new Dictionary<string, int>(
                StringComparer.Ordinal);
            foreach (var item in _equippedItems.Values)
            {
                if (item.SetId == null)
                {
                    continue;
                }

                pieceCounts.TryGetValue(item.SetId, out var currentCount);
                pieceCounts[item.SetId] = currentCount + 1;
            }

            var activatedSets =
                new List<ActivatedEquipmentSetSnapshot>();
            var orderedDefinitions = _setDefinitions.Values
                .OrderBy(definition => definition.SetId, StringComparer.Ordinal);
            foreach (var definition in orderedDefinitions)
            {
                pieceCounts.TryGetValue(
                    definition.SetId,
                    out var equippedPieceCount);
                var activeCounts = new List<int>();
                for (var i = 0; i < definition.Bonuses.Count; i++)
                {
                    var bonus = definition.Bonuses[i];
                    if (equippedPieceCount < bonus.RequiredPieceCount)
                    {
                        continue;
                    }

                    activeCounts.Add(bonus.RequiredPieceCount);
                    activeModifiers.AddRange(bonus.Modifiers);
                }

                if (activeCounts.Count > 0)
                {
                    activatedSets.Add(
                        new ActivatedEquipmentSetSnapshot(
                            definition.SetId,
                            definition.DisplayName,
                            equippedPieceCount,
                            activeCounts.ToArray()));
                }
            }

            return activatedSets.ToArray();
        }

        private List<StatModifier> CollectEquipmentModifiers()
        {
            var modifiers = new List<StatModifier>();
            var orderedItems = _equippedItems
                .OrderBy(pair => pair.Key)
                .Select(pair => pair.Value);
            foreach (var item in orderedItems)
            {
                modifiers.AddRange(item.Modifiers);
            }

            return modifiers;
        }

        private Dictionary<StatType, float> CalculateFinalStats(
            IReadOnlyList<StatModifier> modifiers)
        {
            var finalStats = new Dictionary<StatType, float>();
            var definedStats = GetDefinedEnumValues<StatType>();
            for (var i = 0; i < definedStats.Length; i++)
            {
                var statType = definedStats[i];
                _baseStats.TryGetValue(statType, out var baseValue);
                finalStats[statType] = CalculateStat(
                    statType,
                    baseValue,
                    modifiers);
            }

            return finalStats;
        }

        private static float CalculateStat(
            StatType statType,
            float baseValue,
            IReadOnlyList<StatModifier> modifiers)
        {
            var flatValues = new List<float>();
            var additiveValues = new List<float>();
            var multiplicativeValues = new List<float>();

            for (var i = 0; i < modifiers.Count; i++)
            {
                var modifier = modifiers[i];
                if (modifier.StatType != statType)
                {
                    continue;
                }

                switch (modifier.Operation)
                {
                    case StatModifierOperation.Flat:
                        flatValues.Add(modifier.Value);
                        break;
                    case StatModifierOperation.AdditivePercent:
                        additiveValues.Add(modifier.Value);
                        break;
                    case StatModifierOperation.MultiplicativePercent:
                        multiplicativeValues.Add(modifier.Value);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(modifiers),
                            modifier.Operation,
                            "词条运算方式不在系统定义范围内。");
                }
            }

            flatValues.Sort();
            additiveValues.Sort();
            multiplicativeValues.Sort();

            double flatTotal = 0;
            for (var i = 0; i < flatValues.Count; i++)
            {
                flatTotal += flatValues[i];
            }

            double additiveTotal = 0;
            for (var i = 0; i < additiveValues.Count; i++)
            {
                additiveTotal += additiveValues[i];
            }

            var result = (baseValue + flatTotal) * (1d + additiveTotal);
            for (var i = 0; i < multiplicativeValues.Count; i++)
            {
                result *= 1d + multiplicativeValues[i];
            }

            if (double.IsNaN(result) ||
                double.IsInfinity(result) ||
                result > float.MaxValue ||
                result < -float.MaxValue)
            {
                throw new OverflowException(
                    $"属性 {statType} 的装备重算结果超出 Single 可表示范围。");
            }

            return (float)result;
        }

        private void CommitChange(
            EquipmentChangeKind kind,
            EquipmentSlot slot,
            EquipmentSlot? sourceSlot,
            EquipmentItemSpec previousItem,
            EquipmentItemSpec currentItem,
            long nextRevision,
            EquipmentSnapshot snapshot)
        {
            Revision = nextRevision;

            Changed?.Invoke(
                this,
                new EquipmentChangedEventArgs(
                    kind,
                    slot,
                    sourceSlot,
                    previousItem,
                    currentItem,
                    snapshot));
        }

        private static Dictionary<StatType, float>
            CopyAndValidateBaseStats(
                IReadOnlyDictionary<StatType, float> baseStats)
        {
            var result = new Dictionary<StatType, float>();
            var definedStats = GetDefinedEnumValues<StatType>();
            for (var i = 0; i < definedStats.Length; i++)
            {
                result.Add(definedStats[i], 0f);
            }

            foreach (var pair in baseStats)
            {
                if (!Enum.IsDefined(typeof(StatType), pair.Key))
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(baseStats),
                        pair.Key,
                        "基础属性包含系统未定义的属性类型。");
                }

                if (float.IsNaN(pair.Value) ||
                    float.IsInfinity(pair.Value))
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(baseStats),
                        pair.Value,
                        "基础属性必须是有限数字。");
                }

                result[pair.Key] = pair.Value;
            }

            return result;
        }

        private static Dictionary<string, EquipmentSetSpec>
            CopyAndValidateSetDefinitions(
                IEnumerable<EquipmentSetSpec> setDefinitions)
        {
            var result = new Dictionary<string, EquipmentSetSpec>(
                StringComparer.Ordinal);
            foreach (var definition in setDefinitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException(
                        "套装定义列表不能包含 null。",
                        nameof(setDefinitions));
                }

                if (!result.TryAdd(definition.SetId, definition))
                {
                    throw new ArgumentException(
                        $"套装稳定标识 '{definition.SetId}' 重复。",
                        nameof(setDefinitions));
                }
            }

            return result;
        }

        private static TEnum[] GetDefinedEnumValues<TEnum>()
            where TEnum : struct
        {
            return Enum.GetValues(typeof(TEnum))
                .Cast<TEnum>()
                .OrderBy(value => Convert.ToInt32(value))
                .ToArray();
        }

        private static void ValidateSlot(EquipmentSlot slot)
        {
            if (!Enum.IsDefined(typeof(EquipmentSlot), slot))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(slot),
                    slot,
                    "装备槽位不在系统定义范围内。");
            }
        }

        private static void ValidateSlotCompatibility(
            EquipmentItemSpec item,
            EquipmentSlot targetSlot)
        {
            var itemIsAccessory = IsAccessorySlot(item.Slot);
            var targetIsAccessory = IsAccessorySlot(targetSlot);
            if (itemIsAccessory && targetIsAccessory)
            {
                return;
            }

            if (!itemIsAccessory &&
                !targetIsAccessory &&
                item.Slot == targetSlot)
            {
                return;
            }

            throw new InvalidOperationException(
                $"装备 '{item.ItemId}' 的类型槽位为 {item.Slot}，" +
                $"不能放入目标槽位 {targetSlot}。");
        }

        private static bool IsAccessorySlot(EquipmentSlot slot)
        {
            return slot >= EquipmentSlot.Accessory1 &&
                   slot <= EquipmentSlot.Accessory5;
        }

        private long GetNextRevision()
        {
            checked
            {
                return Revision + 1;
            }
        }
    }
}
