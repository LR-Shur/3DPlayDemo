using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Train.Equipment.Core
{
    /// <summary>
    /// 表示装备栏、基础属性、最终属性和已激活套装的不可变时间点快照。
    /// 后续换装不会改变已经取得的旧快照。
    /// </summary>
    public sealed class EquipmentSnapshot
    {
        private readonly ReadOnlyCollection<EquipmentSlotSnapshot> _slots;
        private readonly ReadOnlyDictionary<StatType, float> _baseStats;
        private readonly ReadOnlyDictionary<StatType, float> _finalStats;
        private readonly ReadOnlyCollection<ActivatedEquipmentSetSnapshot>
            _activatedSets;

        internal EquipmentSnapshot(
            long revision,
            EquipmentSlotSnapshot[] slots,
            IDictionary<StatType, float> baseStats,
            IDictionary<StatType, float> finalStats,
            ActivatedEquipmentSetSnapshot[] activatedSets)
        {
            Revision = revision;
            _slots = Array.AsReadOnly(
                slots ?? throw new ArgumentNullException(nameof(slots)));
            _baseStats = new ReadOnlyDictionary<StatType, float>(
                new Dictionary<StatType, float>(
                    baseStats ??
                    throw new ArgumentNullException(nameof(baseStats))));
            _finalStats = new ReadOnlyDictionary<StatType, float>(
                new Dictionary<StatType, float>(
                    finalStats ??
                    throw new ArgumentNullException(nameof(finalStats))));
            _activatedSets = Array.AsReadOnly(
                activatedSets ??
                throw new ArgumentNullException(nameof(activatedSets)));
        }

        public long Revision { get; }

        public IReadOnlyList<EquipmentSlotSnapshot> Slots => _slots;

        public IReadOnlyDictionary<StatType, float> BaseStats => _baseStats;

        public IReadOnlyDictionary<StatType, float> FinalStats => _finalStats;

        public IReadOnlyList<ActivatedEquipmentSetSnapshot> ActivatedSets =>
            _activatedSets;

        public EquipmentItemSpec GetEquippedItem(EquipmentSlot slot)
        {
            ValidateSlot(slot);
            for (var i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Slot == slot)
                {
                    return _slots[i].Item;
                }
            }

            return null;
        }

        public float GetBaseStat(StatType statType)
        {
            ValidateStat(statType);
            return _baseStats[statType];
        }

        public float GetFinalStat(StatType statType)
        {
            ValidateStat(statType);
            return _finalStats[statType];
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

        private static void ValidateStat(StatType statType)
        {
            if (!Enum.IsDefined(typeof(StatType), statType))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(statType),
                    statType,
                    "属性类型不在系统定义范围内。");
            }
        }
    }
}
