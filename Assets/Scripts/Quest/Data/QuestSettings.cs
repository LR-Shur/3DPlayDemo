using System;
using System.Collections.Generic;
using UnityEngine;

namespace Train.Quest.Data
{
    /// <summary>
    /// 汇总任务系统需要加载的完整任务目录。
    /// </summary>
    [CreateAssetMenu(
        menuName = "Train/Quest/Quest Settings",
        fileName = "QuestSettings")]
    public sealed class QuestSettings : ScriptableObject
    {
        [SerializeField] private List<QuestDefinition> _quests = new();

        /// <summary>获取按策划顺序配置的任务目录。</summary>
        public IReadOnlyList<QuestDefinition> Quests => _quests;

        /// <summary>按照稳定任务标识查询任务定义。</summary>
        public bool TryGetQuest(
            string questId,
            out QuestDefinition definition)
        {
            for (var i = 0; i < _quests.Count; i++)
            {
                var candidate = _quests[i];
                if (candidate != null &&
                    string.Equals(
                        candidate.QuestId,
                        questId,
                        StringComparison.Ordinal))
                {
                    definition = candidate;
                    return true;
                }
            }

            definition = null;
            return false;
        }
    }
}
