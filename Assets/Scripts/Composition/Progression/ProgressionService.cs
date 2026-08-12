using System;
using System.Collections.Generic;
using Train.Architecture.Events;
using UnityEngine;

namespace Train.Composition.Progression
{
    /// <summary>读取流程 SO 并维护金币、当前节点和已完成节点的运行时服务。</summary>
    public sealed class ProgressionService : IProgressionService
    {
        private readonly IEventBus _events;
        private readonly List<RunNode> _nodes;
        private readonly HashSet<string> _clearedLevels = new(StringComparer.Ordinal);
        private int _coins = 150;
        private int _currentNodeIndex;
        private string _currentLevelId;

        /// <summary>根据流程配置创建服务。</summary>
        public ProgressionService(IEventBus events, ProgressionSettings settings = null)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            settings ??= Resources.Load<ProgressionSettings>(ProgressionSettings.ResourcesLocation);
            if (settings == null || settings.Nodes.Count == 0)
            {
                throw new InvalidOperationException("缺少有效的 ProgressionSettings SO，无法建立关卡推进流程。");
            }

            _nodes = new List<RunNode>(settings.Nodes.Count);
            foreach (var definition in settings.Nodes)
            {
                if (definition != null)
                {
                    _nodes.Add(definition.ToRunNode());
                }
            }

            if (_nodes.Count == 0)
            {
                throw new InvalidOperationException("ProgressionSettings SO 中没有有效的流程节点。");
            }

            _currentLevelId = _nodes[0].LevelId;
        }

        /// <inheritdoc />
        public RunProgressSnapshot Snapshot => new(
            _coins, _currentNodeIndex, _clearedLevels.Count, _currentLevelId);

        /// <inheritdoc />
        public IReadOnlyList<RunNode> Nodes => _nodes;

        /// <summary>导出当前流程存档数据。</summary>
        public ProgressionSaveData CaptureState()
        {
            var data = new ProgressionSaveData
            {
                Coins = _coins,
                CurrentNodeIndex = _currentNodeIndex,
                CurrentLevelId = _currentLevelId
            };
            data.ClearedLevels.AddRange(_clearedLevels);
            return data;
        }

        /// <summary>恢复流程存档并校正到当前 SO 中存在的节点。</summary>
        public void RestoreState(ProgressionSaveData data)
        {
            if (data == null) return;
            _coins = Math.Max(0, data.Coins);
            _clearedLevels.Clear();
            foreach (var levelId in data.ClearedLevels)
            {
                if (TryGetNode(levelId, out var node)) _clearedLevels.Add(node.LevelId);
            }

            var index = Math.Max(0, Math.Min(_nodes.Count - 1, data.CurrentNodeIndex));
            if (TryGetNode(data.CurrentLevelId, out var current)) index = _nodes.IndexOf(current);
            _currentNodeIndex = index;
            _currentLevelId = _nodes[index].LevelId;
            _events.Publish(new CurrencyChangedEvent(_coins, 0, "读取流程存档"));
            _events.Publish(new ProgressionChangedEvent(Snapshot));
        }

        /// <inheritdoc />
        public void AddCoins(int amount, string reason)
        {
            if (amount <= 0) return;
            _coins += amount;
            _events.Publish(new CurrencyChangedEvent(_coins, amount, reason));
            _events.Publish(new ProgressionChangedEvent(Snapshot));
        }

        /// <inheritdoc />
        public bool TrySpendCoins(int amount, string reason)
        {
            if (amount <= 0 || amount > _coins) return false;
            _coins -= amount;
            _events.Publish(new CurrencyChangedEvent(_coins, -amount, reason));
            _events.Publish(new ProgressionChangedEvent(Snapshot));
            return true;
        }

        /// <inheritdoc />
        public bool CompleteLevel(string levelId, out RunNode nextNode)
        {
            nextNode = default;
            if (!TryGetNode(levelId, out var node)) return false;
            _currentLevelId = node.LevelId;
            _currentNodeIndex = _nodes.IndexOf(node);
            if (!_clearedLevels.Add(node.LevelId))
            {
                nextNode = GetNextNode(_currentNodeIndex);
                return false;
            }

            AddCoins(node.ClearBonus, $"通关奖励：{node.DisplayName}");
            nextNode = GetNextNode(_currentNodeIndex);
            _events.Publish(new ProgressionChangedEvent(Snapshot));
            return true;
        }

        /// <inheritdoc />
        public bool TryGetNode(string levelId, out RunNode node)
        {
            for (var index = 0; index < _nodes.Count; index++)
            {
                if (string.Equals(_nodes[index].LevelId, levelId, StringComparison.Ordinal) ||
                    string.Equals(NormalizeLegacyId(_nodes[index].LevelId), levelId, StringComparison.Ordinal))
                {
                    node = _nodes[index];
                    return true;
                }
            }

            node = default;
            return false;
        }

        private RunNode GetNextNode(int index)
        {
            var nextIndex = index + 1;
            return nextIndex < _nodes.Count ? _nodes[nextIndex] : default;
        }

        private static string NormalizeLegacyId(string id) =>
            id.Replace("level.", string.Empty).Replace('.', '_');
    }

    /// <summary>流程服务的轻量存档结构。</summary>
    [Serializable]
    public sealed class ProgressionSaveData
    {
        public int Coins = 150;
        public int CurrentNodeIndex;
        public string CurrentLevelId = "level.combat.001";
        public List<string> ClearedLevels = new();
    }
}
