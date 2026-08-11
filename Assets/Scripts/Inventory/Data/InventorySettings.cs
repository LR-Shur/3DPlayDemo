using System;
using System.Collections.Generic;
using UnityEngine;

namespace Train.Inventory.Data
{
    /// <summary>
    /// 配置背包容量、可用物品目录和玩家初始持有物品。
    /// </summary>
    [CreateAssetMenu(
        menuName = "Train/Inventory/Inventory Settings",
        fileName = "InventorySettings")]
    public sealed class InventorySettings : ScriptableObject
    {
        [SerializeField, Min(1)] private int _capacity = 24;
        [SerializeField] private List<ItemDefinition> _items = new();
        [SerializeField] private List<StartingItemDefinition> _startingItems = new();

        /// <summary>获取经过安全限制后的背包槽位容量。</summary>
        public int Capacity => Mathf.Max(1, _capacity);

        /// <summary>获取背包允许使用的全部物品配置。</summary>
        public IReadOnlyList<ItemDefinition> Items => _items;

        /// <summary>获取创建背包时需要放入的初始物品。</summary>
        public IReadOnlyList<StartingItemDefinition> StartingItems =>
            _startingItems;

        /// <summary>
        /// 按稳定物品标识查询物品配置。
        /// </summary>
        public bool TryGetItem(string itemId, out ItemDefinition definition)
        {
            foreach (var candidate in _items)
            {
                if (candidate != null &&
                    string.Equals(
                        candidate.ItemId,
                        itemId,
                        StringComparison.Ordinal))
                {
                    definition = candidate;
                    return true;
                }
            }

            definition = null;
            return false;
        }

        /// <summary>创建由 Luban 物品表驱动的运行时背包配置，初始背包保持为空。</summary>
        public static InventorySettings CreateRuntime(
            int capacity,
            IReadOnlyList<ItemDefinition> items)
        {
            var settings = CreateInstance<InventorySettings>();
            settings.name = "LubanInventorySettings";
            settings._capacity = Mathf.Max(1, capacity);
            settings._items = items != null
                ? new List<ItemDefinition>(items)
                : new List<ItemDefinition>();
            settings._startingItems = new List<StartingItemDefinition>();
            return settings;
        }
    }
}
