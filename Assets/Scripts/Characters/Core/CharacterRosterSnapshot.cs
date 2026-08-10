using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Train.Characters.Core
{
    /// <summary>
    /// 保存角色名册在某一时刻的不可变快照，供 UI、存档和玩法层安全读取。
    /// </summary>
    public sealed class CharacterRosterSnapshot
    {
        private readonly ReadOnlyCollection<CharacterRosterEntrySnapshot>
            _entries;

        /// <summary>创建角色名册快照，并复制传入集合以隔离外部修改。</summary>
        public CharacterRosterSnapshot(
            IEnumerable<CharacterRosterEntrySnapshot> entries,
            string selectedCharacterId,
            long revision)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            _entries = Array.AsReadOnly(
                new List<CharacterRosterEntrySnapshot>(entries).ToArray());
            SelectedCharacterId = selectedCharacterId;
            Revision = revision;
        }

        /// <summary>获取所有已登记角色的只读状态。</summary>
        public IReadOnlyList<CharacterRosterEntrySnapshot> Entries =>
            _entries;

        /// <summary>获取当前选择的角色标识；无可用角色时为 null。</summary>
        public string SelectedCharacterId { get; }

        /// <summary>获取当前快照修订号。</summary>
        public long Revision { get; }

        /// <summary>获取已解锁角色数量。</summary>
        public int UnlockedCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < _entries.Count; i++)
                {
                    if (_entries[i].IsUnlocked)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>按稳定标识查询角色状态。</summary>
        public bool TryGetEntry(
            string characterId,
            out CharacterRosterEntrySnapshot entry)
        {
            if (!string.IsNullOrWhiteSpace(characterId))
            {
                for (var i = 0; i < _entries.Count; i++)
                {
                    if (string.Equals(
                            _entries[i].CharacterId,
                            characterId,
                            StringComparison.Ordinal))
                    {
                        entry = _entries[i];
                        return true;
                    }
                }
            }

            entry = default;
            return false;
        }
    }
}
