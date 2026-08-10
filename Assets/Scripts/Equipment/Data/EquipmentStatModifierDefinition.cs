using System;
using Train.Equipment.Core;
using UnityEngine;

namespace Train.Equipment.Data
{
    /// <summary>
    /// 表示可在 Unity Inspector 中编辑的一条装备属性词条。
    /// </summary>
    [Serializable]
    public sealed class EquipmentStatModifierDefinition
    {
        [SerializeField] private StatType _statType;
        [SerializeField] private StatModifierOperation _operation;
        [SerializeField] private float _value;

        /// <summary>获取词条影响的属性。</summary>
        public StatType StatType => _statType;

        /// <summary>获取词条的运算方式。</summary>
        public StatModifierOperation Operation => _operation;

        /// <summary>
        /// 获取词条数值；百分比使用小数表示，例如 0.2 表示 20%。
        /// </summary>
        public float Value => _value;

        /// <summary>将 Unity 配置转换为纯领域词条。</summary>
        public StatModifier ToCoreModifier()
        {
            return new StatModifier(_statType, _operation, _value);
        }
    }
}
