using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Train.Inventory.Core
{
    /// <summary>
    /// 按槽位索引升序排列的只读背包即时数据。
    /// 快照保留空槽位，便于表现层渲染固定网格。
    /// </summary>
    public sealed class InventorySnapshot
    {
        private readonly ReadOnlyCollection<InventorySlotSnapshot> _slots;

        internal InventorySnapshot(
            long revision,
            InventorySlotSnapshot[] slots)
        {
            Revision = revision;
            _slots = Array.AsReadOnly(slots);
        }

        /// <summary>获取产生此快照时的背包修订号。</summary>
        public long Revision { get; }

        /// <summary>获取背包的固定槽位容量。</summary>
        public int Capacity => _slots.Count;

        /// <summary>获取按索引排列的只读槽位列表。</summary>
        public IReadOnlyList<InventorySlotSnapshot> Slots => _slots;

        /// <summary>获取当前已占用的槽位数量。</summary>
        public int OccupiedSlotCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < _slots.Count; i++)
                {
                    if (!_slots[i].IsEmpty)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>
        /// 获取此快照中指定物品的总数量。
        /// </summary>
        public int GetTotalQuantity(string itemId)
        {
            ValidateItemId(itemId);

            var total = 0;
            for (var i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (!slot.IsEmpty &&
                    string.Equals(
                        slot.ItemId,
                        itemId,
                        StringComparison.Ordinal))
                {
                    checked
                    {
                        total += slot.Quantity;
                    }
                }
            }

            return total;
        }

        private static void ValidateItemId(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                throw new ArgumentException(
                    "Item id cannot be null, empty, or whitespace.",
                    nameof(itemId));
            }
        }
    }
}
