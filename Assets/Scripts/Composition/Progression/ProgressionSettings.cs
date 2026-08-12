using System;
using System.Collections.Generic;
using UnityEngine;

namespace Train.Composition.Progression
{
    /// <summary>运行流程配置资产，节点顺序就是关卡推进顺序。</summary>
    [CreateAssetMenu(menuName = "Train/Progression/Progression Settings", fileName = "ProgressionSettings")]
    public sealed class ProgressionSettings : ScriptableObject
    {
        /// <summary>Resources 中默认流程资产的加载路径。</summary>
        public const string ResourcesLocation = "Progression/DefaultProgressionSettings";

        [SerializeField] private List<ProgressionNodeDefinition> _nodes = new();

        /// <summary>只读流程节点列表。</summary>
        public IReadOnlyList<ProgressionNodeDefinition> Nodes => _nodes;
    }

    /// <summary>一个流程节点的可序列化数据。</summary>
    [Serializable]
    public sealed class ProgressionNodeDefinition
    {
        [SerializeField] private string _levelId;
        [SerializeField] private string _displayName;
        [SerializeField] private string _scenePath;
        [SerializeField] private bool _isBoss;
        [SerializeField, Min(0)] private int _clearBonus;

        /// <summary>转换成运行时节点。</summary>
        public RunNode ToRunNode() => new(
            _levelId,
            _displayName,
            _scenePath,
            _isBoss,
            _clearBonus);
    }
}
