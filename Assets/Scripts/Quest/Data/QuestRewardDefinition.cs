using System;
using Train.Quest.Core;
using UnityEngine;

namespace Train.Quest.Data
{
    /// <summary>
    /// 描述领取任务时发放到背包的一条物品奖励。
    /// </summary>
    [Serializable]
    public sealed class QuestRewardDefinition
    {
        [SerializeField] private string _itemId;
        [SerializeField, Min(1)] private int _amount = 1;

        /// <summary>获取奖励物品的稳定标识。</summary>
        public string ItemId => _itemId;

        /// <summary>获取奖励数量。</summary>
        public int Amount => Mathf.Max(1, _amount);

        /// <summary>把编辑器数据转换为不依赖 Unity 的领域规格。</summary>
        public QuestRewardSpec ToCoreSpec()
        {
            return new QuestRewardSpec(_itemId, _amount);
        }
    }
}
