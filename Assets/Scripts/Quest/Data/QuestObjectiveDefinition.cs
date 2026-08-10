using System;
using Train.Quest.Core;
using UnityEngine;

namespace Train.Quest.Data
{
    /// <summary>
    /// 描述任务中的一条可序列化目标，并负责转换为纯 C# 领域规格。
    /// </summary>
    [Serializable]
    public sealed class QuestObjectiveDefinition
    {
        [SerializeField] private string _objectiveId;
        [SerializeField] private QuestFactType _factType;
        [SerializeField] private string _targetId;
        [TextArea(1, 3)]
        [SerializeField] private string _displayText;
        [SerializeField, Min(1)] private int _requiredAmount = 1;

        /// <summary>获取目标的稳定标识。</summary>
        public string ObjectiveId => _objectiveId;

        /// <summary>获取能够推进此目标的事实类型。</summary>
        public QuestFactType FactType => _factType;

        /// <summary>获取需要精确匹配的目标标识。</summary>
        public string TargetId => _targetId;

        /// <summary>获取供任务界面展示的目标说明。</summary>
        public string DisplayText => _displayText;

        /// <summary>获取完成目标需要累计的数量。</summary>
        public int RequiredAmount => Mathf.Max(1, _requiredAmount);

        /// <summary>把编辑器数据转换为不依赖 Unity 的领域规格。</summary>
        public QuestObjectiveSpec ToCoreSpec()
        {
            return new QuestObjectiveSpec(
                _objectiveId,
                _factType,
                _targetId,
                _requiredAmount);
        }
    }
}
