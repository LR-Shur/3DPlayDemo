using System;

namespace Train.Characters.Core
{
    /// <summary>
    /// 描述角色名册中一名角色的稳定标识与初始解锁状态。
    /// 该类型不依赖 Unity，可直接用于领域测试或存档恢复。
    /// </summary>
    public readonly struct CharacterRosterEntrySpec
    {
        /// <summary>创建一条角色名册定义。</summary>
        public CharacterRosterEntrySpec(
            string characterId,
            bool unlockedByDefault)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                throw new ArgumentException(
                    "角色标识不能为空。",
                    nameof(characterId));
            }

            CharacterId = characterId.Trim();
            UnlockedByDefault = unlockedByDefault;
        }

        /// <summary>获取角色的稳定标识。</summary>
        public string CharacterId { get; }

        /// <summary>获取角色是否默认解锁。</summary>
        public bool UnlockedByDefault { get; }
    }
}
