using System.Collections.Generic;
using Train.Characters.Core;
using Train.Characters.Data;

namespace Train.Characters.Application
{
    /// <summary>
    /// 向表现层与玩法层提供角色目录查询、解锁和当前角色选择用例。
    /// 当前选择仅记录意图，不会直接切换玩家控制器。
    /// </summary>
    public interface ICharacterRosterService
    {
        /// <summary>获取角色名册不可变快照。</summary>
        CharacterRosterSnapshot Snapshot { get; }

        /// <summary>获取全部角色静态配置的只读目录。</summary>
        IReadOnlyList<CharacterDefinition> Catalog { get; }

        /// <summary>按稳定标识查询角色静态配置。</summary>
        bool TryGetDefinition(
            string characterId,
            out CharacterDefinition definition);

        /// <summary>尝试解锁一名已登记角色。</summary>
        bool Unlock(string characterId);

        /// <summary>尝试选择一名已解锁角色。</summary>
        bool Select(string characterId);
    }
}
