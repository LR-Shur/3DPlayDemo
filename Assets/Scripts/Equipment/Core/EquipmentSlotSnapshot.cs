namespace Train.Equipment.Core
{
    /// <summary>
    /// 表示某一装备槽位在特定修订版本中的只读状态。
    /// </summary>
    public readonly struct EquipmentSlotSnapshot
    {
        public EquipmentSlotSnapshot(
            EquipmentSlot slot,
            EquipmentItemSpec item)
        {
            Slot = slot;
            Item = item;
        }

        public EquipmentSlot Slot { get; }

        public EquipmentItemSpec Item { get; }

        public bool IsEmpty => Item == null;
    }
}
