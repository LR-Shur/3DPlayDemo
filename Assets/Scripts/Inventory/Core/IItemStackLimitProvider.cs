namespace Train.Inventory.Core
{
    /// <summary>
    /// 提供指定物品在单个槽位中允许堆叠的最大数量。
    /// 实现可以从物品定义、配置或测试数据中读取该规则。
    /// </summary>
    public interface IItemStackLimitProvider
    {
        /// <summary>
        /// 获取指定物品的单槽堆叠上限。
        /// </summary>
        int GetMaxStack(string itemId);
    }
}
