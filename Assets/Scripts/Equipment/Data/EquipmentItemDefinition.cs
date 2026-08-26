using System;
using System.Collections.Generic;
using Train.Equipment.Core;
using UnityEngine;

namespace Train.Equipment.Data
{
    /// <summary>
    /// 描述一件装备的稳定标识、展示内容、槽位类别、套装和属性词条。
    /// </summary>
    [CreateAssetMenu(
        menuName = "Train/Equipment/Item Definition",
        fileName = "EquipmentItemDefinition")]
    public sealed class EquipmentItemDefinition : ScriptableObject
    {
        [SerializeField] private string _itemId;
        [SerializeField] private string _displayName;
        [TextArea(2, 6)]
        [SerializeField] private string _description;
        [SerializeField] private EquipmentRarity _rarity =
            EquipmentRarity.Common;
        [SerializeField] private EquipmentItemCategory _category;
        [SerializeField] private string _setId;
        [SerializeField] private string _elementId;
        [SerializeField] private string _effectTriggerId;
        [SerializeField] private string _onHitBuffId;
        [SerializeField, Min(0f)] private float _effectDuration;
        [SerializeField] private float _effectMagnitude;
        [SerializeField, Min(1)] private int _effectStackAmount = 1;
        [SerializeField, Min(1)] private int _effectMaxStacks = 1;
        [SerializeField, Min(0f)] private float _effectCooldown;
        [SerializeField] private Sprite _icon;
        [SerializeField]
        private List<EquipmentStatModifierDefinition> _modifiers = new();

        /// <summary>获取用于存档、查询和去重的稳定标识。</summary>
        public string ItemId => _itemId;

        /// <summary>获取玩家可见的装备名称。</summary>
        public string DisplayName => _displayName;

        /// <summary>获取玩家可见的装备说明。</summary>
        public string Description => _description;

        /// <summary>获取装备品质。</summary>
        public EquipmentRarity Rarity => _rarity;

        /// <summary>获取固定装备类别或通用饰品类别。</summary>
        public EquipmentItemCategory Category => _category;

        /// <summary>获取套装稳定标识；空字符串表示不属于套装。</summary>
        public string SetId => _setId;

        /// <summary>装备绑定的元素 ID，例如 FIRE、WATER、WIND 或 EARTH。</summary>
        public string ElementId => _elementId;

        /// <summary>装备效果触发时机，例如 ON_HIT、ON_DODGE 或 ON_TAKE_DAMAGE。</summary>
        public string EffectTriggerId => _effectTriggerId;

        /// <summary>装备命中时申请的 Buff ID。</summary>
        public string OnHitBuffId => _onHitBuffId;

        /// <summary>命中 Buff 持续时间。</summary>
        public float EffectDuration => _effectDuration;

        /// <summary>命中 Buff 每层强度。</summary>
        public float EffectMagnitude => _effectMagnitude;

        /// <summary>命中 Buff 每次增加的层数。</summary>
        public int EffectStackAmount => Mathf.Max(1, _effectStackAmount);

        /// <summary>命中 Buff 最大层数。</summary>
        public int EffectMaxStacks => Mathf.Max(1, _effectMaxStacks);

        /// <summary>命中效果冷却时间。</summary>
        public float EffectCooldown => Mathf.Max(0f, _effectCooldown);

        /// <summary>获取装备界面使用的图标。</summary>
        public Sprite Icon => _icon;

        /// <summary>获取只读属性词条配置列表。</summary>
        public IReadOnlyList<EquipmentStatModifierDefinition> Modifiers =>
            _modifiers;

        /// <summary>
        /// 判断该装备能否放入目标槽位。
        /// 饰品可进入任意饰品槽，普通装备只能进入唯一固定槽。
        /// </summary>
        public bool CanEquipIn(EquipmentSlot targetSlot)
        {
            if (!Enum.IsDefined(typeof(EquipmentSlot), targetSlot))
            {
                return false;
            }

            if (_category == EquipmentItemCategory.Accessory)
            {
                return targetSlot >= EquipmentSlot.Accessory1 &&
                       targetSlot <= EquipmentSlot.Accessory5;
            }

            return targetSlot == GetDefaultSlot();
        }

        /// <summary>
        /// 获取普通装备的固定槽，或饰品未指定目标时使用的默认饰品槽。
        /// </summary>
        public EquipmentSlot GetDefaultSlot()
        {
            return _category switch
            {
                EquipmentItemCategory.Weapon => EquipmentSlot.Weapon,
                EquipmentItemCategory.Helmet => EquipmentSlot.Helmet,
                EquipmentItemCategory.Armor => EquipmentSlot.Armor,
                EquipmentItemCategory.Gloves => EquipmentSlot.Gloves,
                EquipmentItemCategory.Shoes => EquipmentSlot.Shoes,
                EquipmentItemCategory.Accessory =>
                    EquipmentSlot.Accessory1,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(_category),
                    _category,
                    "装备类别不在系统定义范围内。")
            };
        }

        /// <summary>将 Unity 装备配置转换为不可变的纯领域定义。</summary>
        public EquipmentItemSpec ToCoreSpec()
        {
            var modifiers = new StatModifier[_modifiers.Count];
            for (var i = 0; i < _modifiers.Count; i++)
            {
                var modifier = _modifiers[i] ??
                    throw new InvalidOperationException(
                        $"装备 '{_itemId}' 的词条列表包含 null。");
                modifiers[i] = modifier.ToCoreModifier();
            }

            var normalizedSetId = string.IsNullOrWhiteSpace(_setId)
                ? null
                : _setId;
            return new EquipmentItemSpec(
                _itemId,
                _displayName,
                GetDefaultSlot(),
                normalizedSetId,
                modifiers);
        }

        /// <summary>根据 Luban 装备行创建运行时装备定义。</summary>
        public static EquipmentItemDefinition CreateRuntime(
            string itemId,
            string displayName,
            string description,
            EquipmentRarity rarity,
            EquipmentItemCategory category,
            IReadOnlyList<EquipmentStatModifierDefinition> modifiers,
            string setId = "",
            string elementId = "NONE",
            string effectTriggerId = "",
            string onHitBuffId = "",
            float effectDuration = 0f,
            float effectMagnitude = 0f,
            int effectStackAmount = 1,
            int effectMaxStacks = 1,
            float effectCooldown = 0f)
        {
            var definition = CreateInstance<EquipmentItemDefinition>();
            definition.name = $"LubanEquipment_{itemId}";
            definition._itemId = itemId;
            definition._displayName = displayName;
            definition._description = description ?? string.Empty;
            definition._rarity = rarity;
            definition._category = category;
            definition._setId = setId ?? string.Empty;
            definition._elementId = elementId ?? "NONE";
            definition._effectTriggerId = effectTriggerId ?? string.Empty;
            definition._onHitBuffId = onHitBuffId ?? string.Empty;
            definition._effectDuration = Mathf.Max(0f, effectDuration);
            definition._effectMagnitude = effectMagnitude;
            definition._effectStackAmount = Mathf.Max(1, effectStackAmount);
            definition._effectMaxStacks = Mathf.Max(1, effectMaxStacks);
            definition._effectCooldown = Mathf.Max(0f, effectCooldown);
            definition._modifiers = modifiers != null
                ? new List<EquipmentStatModifierDefinition>(modifiers)
                : new List<EquipmentStatModifierDefinition>();
            return definition;
        }
    }
}
