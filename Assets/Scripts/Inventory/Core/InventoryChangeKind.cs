namespace Train.Inventory.Core
{
    /// <summary>
    /// 表示背包中一次已提交变化的种类。
    /// </summary>
    public enum InventoryChangeKind
    {
        /// <summary>物品数量增加。</summary>
        Added = 0,

        /// <summary>物品数量减少。</summary>
        Removed = 1
    }
}
