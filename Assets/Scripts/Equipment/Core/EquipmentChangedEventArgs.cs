using System;

namespace Train.Equipment.Core
{
    /// <summary>
    /// 携带一次成功装备变更及其提交后快照的领域事件参数。
    /// </summary>
    public sealed class EquipmentChangedEventArgs : EventArgs
    {
        public EquipmentChangedEventArgs(
            EquipmentChangeKind kind,
            EquipmentSlot slot,
            EquipmentSlot? sourceSlot,
            EquipmentItemSpec previousItem,
            EquipmentItemSpec currentItem,
            EquipmentSnapshot snapshot)
        {
            Kind = kind;
            Slot = slot;
            SourceSlot = sourceSlot;
            PreviousItem = previousItem;
            CurrentItem = currentItem;
            Snapshot = snapshot ??
                throw new ArgumentNullException(nameof(snapshot));
        }

        public EquipmentChangeKind Kind { get; }

        public EquipmentSlot Slot { get; }

        public EquipmentSlot? SourceSlot { get; }

        public EquipmentItemSpec PreviousItem { get; }

        public EquipmentItemSpec CurrentItem { get; }

        public EquipmentSnapshot Snapshot { get; }
    }
}
