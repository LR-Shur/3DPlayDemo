using System;
using System.Collections.Generic;
using Train.Quest.Core;
using UnityEngine;

namespace Train.Quest.Data
{
    /// <summary>
    /// 保存一项任务的策划数据，包括展示信息、目标、奖励和自动接取规则。
    /// </summary>
    [CreateAssetMenu(
        menuName = "Train/Quest/Quest Definition",
        fileName = "QuestDefinition")]
    public sealed class QuestDefinition : ScriptableObject
    {
        [SerializeField] private string _questId;
        [SerializeField] private string _title;
        [TextArea(2, 6)]
        [SerializeField] private string _description;
        [SerializeField] private bool _autoAccept;
        [SerializeField] private int _sortOrder;
        [SerializeField] private List<QuestObjectiveDefinition> _objectives =
            new();
        [SerializeField] private List<QuestRewardDefinition> _rewards = new();

        /// <summary>获取任务的稳定标识。</summary>
        public string QuestId => _questId;

        /// <summary>获取任务标题。</summary>
        public string Title => _title;

        /// <summary>获取任务背景和玩法说明。</summary>
        public string Description => _description;

        /// <summary>获取任务系统初始化时是否自动接取此任务。</summary>
        public bool AutoAccept => _autoAccept;

        /// <summary>获取任务在列表中的策划排序值。</summary>
        public int SortOrder => _sortOrder;

        /// <summary>获取只读目标配置列表。</summary>
        public IReadOnlyList<QuestObjectiveDefinition> Objectives =>
            _objectives;

        /// <summary>获取只读奖励配置列表。</summary>
        public IReadOnlyList<QuestRewardDefinition> Rewards => _rewards;

        /// <summary>把可序列化配置转换为纯 C# 任务领域规格。</summary>
        public QuestSpec ToCoreSpec()
        {
            var objectives = new QuestObjectiveSpec[_objectives.Count];
            for (var i = 0; i < _objectives.Count; i++)
            {
                var definition = _objectives[i] ??
                    throw new InvalidOperationException(
                        $"任务 '{name}' 的目标列表包含空项。");
                objectives[i] = definition.ToCoreSpec();
            }

            var rewards = new QuestRewardSpec[_rewards.Count];
            for (var i = 0; i < _rewards.Count; i++)
            {
                var definition = _rewards[i] ??
                    throw new InvalidOperationException(
                        $"任务 '{name}' 的奖励列表包含空项。");
                rewards[i] = definition.ToCoreSpec();
            }

            return new QuestSpec(
                _questId,
                _title,
                objectives,
                rewards);
        }
    }
}
