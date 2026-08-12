using Train.Inventory.Core;
using Train.Inventory.Data;

namespace Train.Inventory.Application
{
    /// <summary>
    /// 面向应用层的背包服务接口。
    /// 表现层和玩法层依赖此接口，而不直接接触可变的领域模型。
    /// </summary>
    public interface IInventoryService
    {
        /// <summary>获取全部物品定义，供商店和编辑器展示。</summary>
        System.Collections.Generic.IReadOnlyList<ItemDefinition> Catalog { get; }

        /// <summary>获取当前背包的不可变快照。</summary>
        InventorySnapshot Snapshot { get; }

        /// <summary>尝试增加指定数量的已登记物品。</summary>
        bool TryAdd(string itemId, int quantity);

        /// <summary>尝试移除指定数量的已登记物品。</summary>
        bool TryRemove(string itemId, int quantity);

        /// <summary>获取指定物品的总持有数量。</summary>
        int GetTotalQuantity(string itemId);

        /// <summary>尝试获取指定物品的静态配置。</summary>
        bool TryGetDefinition(
            string itemId,
            out ItemDefinition definition);
    }
}
