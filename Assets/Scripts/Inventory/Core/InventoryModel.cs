using System;
using System.Collections.Generic;

namespace Train.Inventory.Core
{
    /// <summary>
    /// 固定槽位背包的纯领域模型。
    ///
    /// 所有修改都会先完整校验容量与数量，再写入槽位。
    /// 因此失败的 TryAdd 或 TryRemove 不会改变槽位数据和修订号。
    /// </summary>
    public sealed class InventoryModel
    {
        private readonly List<SlotState> _slots;
        private readonly IItemStackLimitProvider _stackLimitProvider;

        /// <summary>
        /// 创建固定容量的背包模型。
        /// </summary>
        public InventoryModel(
            int capacity,
            IItemStackLimitProvider stackLimitProvider)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    capacity,
                    "Inventory capacity must be greater than zero.");
            }

            _stackLimitProvider = stackLimitProvider ??
                throw new ArgumentNullException(nameof(stackLimitProvider));
            _slots = new List<SlotState>(capacity);
            for (var index = 0; index < capacity; index++)
            {
                _slots.Add(default);
            }
        }

        /// <summary>
        /// 在一次背包修改成功提交后触发。
        /// </summary>
        public event EventHandler<InventoryChangedEventArgs> Changed;

        /// <summary>获取背包的固定槽位数量。</summary>
        public int Capacity => _slots.Count;

        /// <summary>获取随每次成功修改递增的修订号。</summary>
        public long Revision { get; private set; }

        /// <summary>获取当前背包的不可变快照。</summary>
        public InventorySnapshot Snapshot => CreateSnapshot();

        /// <summary>
        /// 原子地尝试向背包增加指定数量的物品。
        /// </summary>
        /// <returns>容量足够并完成增加时返回 <see langword="true"/>。</returns>
        public bool TryAdd(string itemId, int quantity)
        {
            ValidateOperation(itemId, quantity);
            var maxStack = GetValidatedMaxStack(itemId);

            var remaining = quantity;

            // 先按槽位顺序填充已有堆叠。
            for (var i = 0; i < _slots.Count && remaining > 0; i++)
            {
                var slot = _slots[i];
                if (slot.IsEmpty ||
                    !string.Equals(
                        slot.ItemId,
                        itemId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                var room = maxStack - slot.Quantity;
                var amount = Math.Min(room, remaining);
                slot.Quantity += amount;
                _slots[i] = slot;
                remaining -= amount;
            }

            // 新堆叠始终使用索引最小的空槽位。
            for (var i = 0; i < _slots.Count && remaining > 0; i++)
            {
                var slot = _slots[i];
                if (!slot.IsEmpty)
                {
                    continue;
                }

                var amount = Math.Min(maxStack, remaining);
                slot.ItemId = itemId;
                slot.Quantity = amount;
                _slots[i] = slot;
                remaining -= amount;
            }

            while (remaining > 0)
            {
                var amount = Math.Min(maxStack, remaining);
                _slots.Add(new SlotState
                {
                    ItemId = itemId,
                    Quantity = amount
                });
                remaining -= amount;
            }

            CommitChange(InventoryChangeKind.Added, itemId, quantity);
            return true;
        }

        /// <summary>
        /// 原子地尝试从背包移除指定数量的物品。
        /// </summary>
        /// <returns>持有数量足够并完成移除时返回 <see langword="true"/>。</returns>
        public bool TryRemove(string itemId, int quantity)
        {
            ValidateOperation(itemId, quantity);

            if (CalculateItemQuantity(itemId) < quantity)
            {
                return false;
            }

            var remaining = quantity;

            // 从最后一个堆叠开始移除，使前面的槽位保持紧凑且结果可预测。
            for (var i = _slots.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var slot = _slots[i];
                if (slot.IsEmpty ||
                    !string.Equals(
                        slot.ItemId,
                        itemId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                var amount = Math.Min(slot.Quantity, remaining);
                slot.Quantity -= amount;
                remaining -= amount;

                if (slot.Quantity == 0)
                {
                    slot.ItemId = null;
                }

                _slots[i] = slot;
            }

            CommitChange(InventoryChangeKind.Removed, itemId, quantity);
            return true;
        }

        /// <summary>
        /// 获取背包中指定物品的总数量。
        /// </summary>
        public int GetTotalQuantity(string itemId)
        {
            ValidateItemId(itemId);

            var total = CalculateItemQuantity(itemId);
            if (total > int.MaxValue)
            {
                throw new OverflowException(
                    $"Item quantity for '{itemId}' exceeds Int32.MaxValue.");
            }

            return (int)total;
        }

        /// <summary>
        /// 创建包含所有空槽位和已占用槽位的不可变快照。
        /// </summary>
        public InventorySnapshot CreateSnapshot()
        {
            var slots = new InventorySlotSnapshot[_slots.Count];
            for (var i = 0; i < _slots.Count; i++)
            {
                slots[i] = new InventorySlotSnapshot(
                    i,
                    _slots[i].ItemId,
                    _slots[i].Quantity);
            }

            return new InventorySnapshot(Revision, slots);
        }

        private long CalculateItemQuantity(string itemId)
        {
            long quantity = 0;
            for (var i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (!slot.IsEmpty &&
                    string.Equals(
                        slot.ItemId,
                        itemId,
                        StringComparison.Ordinal))
                {
                    quantity += slot.Quantity;
                }
            }

            return quantity;
        }

        private int GetValidatedMaxStack(string itemId)
        {
            var maxStack = _stackLimitProvider.GetMaxStack(itemId);
            if (maxStack <= 0)
            {
                throw new InvalidOperationException(
                    $"The maximum stack for '{itemId}' must be greater than " +
                    $"zero, but the provider returned {maxStack}.");
            }

            return maxStack;
        }

        private void CommitChange(
            InventoryChangeKind kind,
            string itemId,
            int quantity)
        {
            checked
            {
                Revision++;
            }

            var snapshot = CreateSnapshot();
            Changed?.Invoke(
                this,
                new InventoryChangedEventArgs(
                    kind,
                    itemId,
                    quantity,
                    snapshot));
        }

        private static void ValidateOperation(
            string itemId,
            int quantity)
        {
            ValidateItemId(itemId);
            if (quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(quantity),
                    quantity,
                    "Quantity must be greater than zero.");
            }
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

        /// <summary>
        /// 背包内部使用的可变槽位状态。
        /// </summary>
        private struct SlotState
        {
            public string ItemId;
            public int Quantity;

            public bool IsEmpty => Quantity == 0;
        }
    }
}
