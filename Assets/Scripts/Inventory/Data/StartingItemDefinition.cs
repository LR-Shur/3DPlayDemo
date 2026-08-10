using System;
using UnityEngine;

namespace Train.Inventory.Data
{
    /// <summary>
    /// 配置创建背包时需要放入的一种初始物品及其数量。
    /// </summary>
    [Serializable]
    public sealed class StartingItemDefinition
    {
        [SerializeField] private string _itemId;
        [SerializeField, Min(1)] private int _count = 1;

        /// <summary>获取初始物品的稳定标识。</summary>
        public string ItemId => _itemId;

        /// <summary>获取至少为一的初始数量。</summary>
        public int Count => Mathf.Max(1, _count);
    }
}
