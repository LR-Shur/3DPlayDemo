using System;

namespace Train.Inventory.Core
{
    /// <summary>
    /// 表示一次背包领域操作已经成功提交后的变化通知。
    /// </summary>
    public sealed class InventoryChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 创建一条已提交的背包变化通知。
        /// </summary>
        public InventoryChangedEventArgs(
            InventoryChangeKind kind,
            string itemId,
            int quantity,
            InventorySnapshot snapshot)
        {
            Kind = kind;
            ItemId = itemId ??
                throw new ArgumentNullException(nameof(itemId));
            Quantity = quantity;
            Snapshot = snapshot ??
                throw new ArgumentNullException(nameof(snapshot));
        }

        /// <summary>获取变化类型。</summary>
        public InventoryChangeKind Kind { get; }

        /// <summary>获取发生变化的物品标识。</summary>
        public string ItemId { get; }

        /// <summary>获取本次增加或移除的数量。</summary>
        public int Quantity { get; }

        /// <summary>获取变化提交后的完整背包快照。</summary>
        public InventorySnapshot Snapshot { get; }
    }
}
