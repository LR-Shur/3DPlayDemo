using System;
using System.Collections.Generic;
using Train.Equipment.Core;
using UnityEngine;

namespace Train.Equipment.Data
{
    /// <summary>
    /// 表示可在 Inspector 中编辑的一档套装件数奖励。
    /// </summary>
    [Serializable]
    public sealed class EquipmentSetBonusDefinition
    {
        [SerializeField, Min(2)] private int _requiredPieceCount = 2;
        [SerializeField]
        private List<EquipmentStatModifierDefinition> _modifiers = new();

        /// <summary>获取激活本档奖励需要的装备件数。</summary>
        public int RequiredPieceCount => _requiredPieceCount;

        /// <summary>获取本档奖励包含的只读属性词条列表。</summary>
        public IReadOnlyList<EquipmentStatModifierDefinition> Modifiers =>
            _modifiers;

        /// <summary>将 Unity 配置转换为不可变的纯领域套装奖励。</summary>
        public EquipmentSetBonusSpec ToCoreSpec()
        {
            var modifiers = new StatModifier[_modifiers.Count];
            for (var i = 0; i < _modifiers.Count; i++)
            {
                var modifier = _modifiers[i] ??
                    throw new InvalidOperationException(
                        $"套装 {_requiredPieceCount} 件奖励包含 null 词条。");
                modifiers[i] = modifier.ToCoreModifier();
            }

            return new EquipmentSetBonusSpec(
                _requiredPieceCount,
                modifiers);
        }
    }
}
