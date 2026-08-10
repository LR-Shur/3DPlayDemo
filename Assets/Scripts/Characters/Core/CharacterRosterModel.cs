using System;
using System.Collections.Generic;

namespace Train.Characters.Core
{
    /// <summary>
    /// 管理角色登记、解锁与选择规则的纯领域模型。
    /// 选择行为只改变名册状态，不负责替换场景中的玩家控制器。
    /// </summary>
    public sealed class CharacterRosterModel
    {
        private readonly List<EntryState> _entries = new();
        private readonly Dictionary<string, EntryState> _entryById =
            new(StringComparer.Ordinal);
        private CharacterRosterSnapshot _snapshot;
        private string _selectedCharacterId;
        private long _revision;

        /// <summary>
        /// 根据角色定义建立名册，并选择指定的默认已解锁角色。
        /// 默认标识为空时，会选择第一名已解锁角色。
        /// </summary>
        public CharacterRosterModel(
            IEnumerable<CharacterRosterEntrySpec> entries,
            string defaultSelectedCharacterId)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            foreach (var spec in entries)
            {
                var state = new EntryState(
                    spec.CharacterId,
                    spec.UnlockedByDefault);
                if (!_entryById.TryAdd(state.CharacterId, state))
                {
                    throw new InvalidOperationException(
                        $"角色名册包含重复标识 '{state.CharacterId}'。");
                }

                _entries.Add(state);
            }

            if (_entries.Count == 0)
            {
                throw new InvalidOperationException(
                    "角色名册至少需要登记一名角色。");
            }

            _selectedCharacterId = ResolveInitialSelection(
                defaultSelectedCharacterId);
            RebuildSnapshot();
        }

        /// <summary>获取当前不可变名册快照。</summary>
        public CharacterRosterSnapshot Snapshot => _snapshot;

        /// <summary>
        /// 解锁指定角色。角色不存在或已经解锁时不产生修订。
        /// </summary>
        public bool Unlock(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId) ||
                !_entryById.TryGetValue(characterId, out var entry) ||
                entry.IsUnlocked)
            {
                return false;
            }

            entry.IsUnlocked = true;
            _revision = checked(_revision + 1);
            if (_selectedCharacterId == null)
            {
                _selectedCharacterId = entry.CharacterId;
            }

            RebuildSnapshot();
            return true;
        }

        /// <summary>
        /// 选择一名已解锁角色。未登记、未解锁或重复选择时不产生修订。
        /// </summary>
        public bool Select(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId) ||
                !_entryById.TryGetValue(characterId, out var entry) ||
                !entry.IsUnlocked ||
                string.Equals(
                    _selectedCharacterId,
                    characterId,
                    StringComparison.Ordinal))
            {
                return false;
            }

            _selectedCharacterId = entry.CharacterId;
            _revision = checked(_revision + 1);
            RebuildSnapshot();
            return true;
        }

        private string ResolveInitialSelection(string requestedCharacterId)
        {
            if (!string.IsNullOrWhiteSpace(requestedCharacterId))
            {
                if (!_entryById.TryGetValue(
                        requestedCharacterId,
                        out var requested))
                {
                    throw new InvalidOperationException(
                        $"默认角色 '{requestedCharacterId}' 未登记在名册中。");
                }

                if (!requested.IsUnlocked)
                {
                    throw new InvalidOperationException(
                        $"默认角色 '{requestedCharacterId}' 尚未解锁。");
                }

                return requested.CharacterId;
            }

            for (var i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].IsUnlocked)
                {
                    return _entries[i].CharacterId;
                }
            }

            return null;
        }

        private void RebuildSnapshot()
        {
            var entries =
                new CharacterRosterEntrySnapshot[_entries.Count];
            for (var i = 0; i < _entries.Count; i++)
            {
                var state = _entries[i];
                entries[i] = new CharacterRosterEntrySnapshot(
                    state.CharacterId,
                    state.IsUnlocked,
                    string.Equals(
                        state.CharacterId,
                        _selectedCharacterId,
                        StringComparison.Ordinal));
            }

            _snapshot = new CharacterRosterSnapshot(
                entries,
                _selectedCharacterId,
                _revision);
        }

        /// <summary>保存领域模型内部可变角色状态。</summary>
        private sealed class EntryState
        {
            /// <summary>创建一条内部角色状态。</summary>
            public EntryState(string characterId, bool isUnlocked)
            {
                CharacterId = characterId;
                IsUnlocked = isUnlocked;
            }

            /// <summary>获取角色稳定标识。</summary>
            public string CharacterId { get; }

            /// <summary>获取或设置角色是否已解锁。</summary>
            public bool IsUnlocked { get; set; }
        }
    }
}
