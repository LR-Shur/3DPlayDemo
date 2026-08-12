using System;
using UnityEngine;

namespace Train.GameFlow.Data
{
    /// <summary>
    /// 关卡完成时发放的一条物品奖励配置。
    /// </summary>
    [Serializable]
    public sealed class LevelRewardDefinition
    {
        [SerializeField] private string _itemId = "training_chip";
        [SerializeField, Min(1)] private int _count = 1;

        /// <summary>奖励物品稳定 ID，对应背包物品表。</summary>
        public string ItemId => _itemId;

        /// <summary>奖励数量。</summary>
        public int Count => Mathf.Max(1, _count);

        /// <summary>由编辑器或迁移工具写入奖励数据。</summary>
        public void Configure(string itemId, int count)
        {
            _itemId = itemId ?? string.Empty;
            _count = Mathf.Max(1, count);
        }
    }
}
