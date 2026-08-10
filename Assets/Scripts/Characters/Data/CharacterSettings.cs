using System.Collections.Generic;
using UnityEngine;

namespace Train.Characters.Data
{
    /// <summary>
    /// 汇总角色目录与新游戏默认选中角色，作为角色名册系统的统一入口资产。
    /// </summary>
    [CreateAssetMenu(
        menuName = "Train/Characters/Character Settings",
        fileName = "CharacterSettings")]
    public sealed class CharacterSettings : ScriptableObject
    {
        [SerializeField] private List<CharacterDefinition> _characters = new();
        [SerializeField] private string _defaultSelectedCharacterId;

        /// <summary>获取按展示顺序排列的只读角色目录。</summary>
        public IReadOnlyList<CharacterDefinition> Characters => _characters;

        /// <summary>获取新名册默认选中的角色标识。</summary>
        public string DefaultSelectedCharacterId =>
            _defaultSelectedCharacterId;
    }
}
