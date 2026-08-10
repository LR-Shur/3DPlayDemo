using Train.Characters.Core;
using UnityEngine;

namespace Train.Characters.Data
{
    /// <summary>
    /// 保存一名角色的展示资料、默认解锁规则及可选的模型与待机动画引用。
    /// 该资产仅描述内容，不会自行把角色实例化到场景。
    /// </summary>
    [CreateAssetMenu(
        menuName = "Train/Characters/Character Definition",
        fileName = "CharacterDefinition")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [SerializeField] private string _characterId;
        [SerializeField] private string _displayName;
        [TextArea(2, 5)]
        [SerializeField] private string _description;
        [SerializeField] private string _faction;
        [SerializeField] private string _combatRole;
        [SerializeField] private bool _unlockedByDefault;
        [SerializeField] private GameObject _modelAsset;
        [SerializeField] private AnimationClip _idleAnimation;

        /// <summary>获取角色稳定标识。</summary>
        public string CharacterId => _characterId;

        /// <summary>获取角色中文展示名。</summary>
        public string DisplayName => _displayName;

        /// <summary>获取角色简介。</summary>
        public string Description => _description;

        /// <summary>获取角色所属阵营。</summary>
        public string Faction => _faction;

        /// <summary>获取角色战斗定位。</summary>
        public string CombatRole => _combatRole;

        /// <summary>获取角色是否在新名册中默认解锁。</summary>
        public bool UnlockedByDefault => _unlockedByDefault;

        /// <summary>获取用于预览或后续实例化的模型资产。</summary>
        public GameObject ModelAsset => _modelAsset;

        /// <summary>获取用于预览或后续 Animator 配置的待机动画。</summary>
        public AnimationClip IdleAnimation => _idleAnimation;

        /// <summary>转换为不依赖 Unity 的领域定义。</summary>
        public CharacterRosterEntrySpec ToCoreSpec()
        {
            return new CharacterRosterEntrySpec(
                _characterId,
                _unlockedByDefault);
        }
    }
}
