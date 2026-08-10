using System;
using System.Collections.Generic;
using Train.Architecture.Events;

namespace Train.Composition.Progression
{
    /// <summary>
    /// 当前运行的金币与关卡进度服务。
    /// 存档系统接入前，生命周期与 GameBootstrap 相同，避免换场景丢失成长。
    /// </summary>
    public sealed class ProgressionService : IProgressionService
    {
        private readonly IEventBus _events;
        private readonly List<RunNode> _nodes = new()
        {
            new RunNode(
                "level.combat.001",
                "第 1 训练区 · 霓虹外环",
                "Assets/Scenes/Playable/Level_Combat_001.unity",
                false,
                180),
            new RunNode(
                "level.combat.002",
                "第 2 战区 · 断电回廊",
                "Assets/Scenes/Playable/Level_Combat_002.unity",
                false,
                300),
            new RunNode(
                "level.boss.001",
                "核心熔炉 · Rusk 原型机",
                "Assets/Scenes/Playable/Level_Boss_001.unity",
                true,
                800)
        };
        private readonly HashSet<string> _clearedLevels = new(
            StringComparer.Ordinal);

        private int _coins = 150;
        private int _currentNodeIndex;
        private string _currentLevelId = "level.combat.001";

        public ProgressionService(IEventBus events)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public RunProgressSnapshot Snapshot => new(
            _coins,
            _currentNodeIndex,
            _clearedLevels.Count,
            _currentLevelId);

        public IReadOnlyList<RunNode> Nodes => _nodes;

        /// <summary>导出当前 Run 的可持久化进度。</summary>
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

        /// <summary>
        /// 恢复已校验的 Run 进度，并发布一次完整进度变更。
        /// </summary>
        public void RestoreState(ProgressionSaveData data)
        {
            if (data == null)
            {
                return;
            }

            _coins = Math.Max(0, data.Coins);
            _clearedLevels.Clear();
            for (var i = 0; i < data.ClearedLevels.Count; i++)
            {
                if (TryGetNode(data.ClearedLevels[i], out var clearedNode))
                {
                    _clearedLevels.Add(clearedNode.LevelId);
                }
            }

            var requestedIndex = Math.Max(
                0,
                Math.Min(_nodes.Count - 1, data.CurrentNodeIndex));
            if (TryGetNode(data.CurrentLevelId, out var currentNode))
            {
                requestedIndex = _nodes.IndexOf(currentNode);
            }

            _currentNodeIndex = requestedIndex;
            _currentLevelId = _nodes[_currentNodeIndex].LevelId;
            _events.Publish(new CurrencyChangedEvent(_coins, 0, "读取 Run 存档"));
            _events.Publish(new ProgressionChangedEvent(Snapshot));
        }

        public void AddCoins(int amount, string reason)
        {
            if (amount <= 0)
            {
                return;
            }

            _coins += amount;
            _events.Publish(new CurrencyChangedEvent(_coins, amount, reason));
            _events.Publish(new ProgressionChangedEvent(Snapshot));
        }

        public bool TrySpendCoins(int amount, string reason)
        {
            if (amount <= 0 || amount > _coins)
            {
                return false;
            }

            _coins -= amount;
            _events.Publish(new CurrencyChangedEvent(_coins, -amount, reason));
            _events.Publish(new ProgressionChangedEvent(Snapshot));
            return true;
        }

        public bool CompleteLevel(string levelId, out RunNode nextNode)
        {
            nextNode = default;
            if (!TryGetNode(levelId, out var node))
            {
                return false;
            }

            _currentLevelId = levelId;
            _currentNodeIndex = _nodes.IndexOf(node);
            if (!_clearedLevels.Add(levelId))
            {
                nextNode = GetNextNode(_currentNodeIndex);
                return false;
            }

            AddCoins(node.ClearBonus, $"通关奖励：{node.DisplayName}");
            nextNode = GetNextNode(_currentNodeIndex);
            _events.Publish(new ProgressionChangedEvent(Snapshot));
            return true;
        }

        public bool TryGetNode(string levelId, out RunNode node)
        {
            for (var i = 0; i < _nodes.Count; i++)
            {
                if (string.Equals(_nodes[i].LevelId, levelId, StringComparison.Ordinal) ||
                    string.Equals(levelId, NormalizeLegacyId(_nodes[i].LevelId), StringComparison.Ordinal))
                {
                    node = _nodes[i];
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

        private static string NormalizeLegacyId(string id)
        {
            return id.Replace("level.", string.Empty).Replace('.', '_');
        }
    }

    /// <summary>
    /// ProgressionService 的轻量 JSON 存档结构。
    /// </summary>
    [Serializable]
    public sealed class ProgressionSaveData
    {
        public int Coins = 150;
        public int CurrentNodeIndex;
        public string CurrentLevelId = "level.combat.001";
        public List<string> ClearedLevels = new();
    }
}
