using System;
using Train.Equipment.Core;
using UnityEngine;

namespace Train.Equipment.Data
{
    /// <summary>
    /// 表示角色在未计算任何装备词条前的一项基础属性配置。
    /// </summary>
    [Serializable]
    public sealed class EquipmentBaseStatDefinition
    {
        [SerializeField] private StatType _statType;
        [SerializeField] private float _value;

        /// <summary>获取基础属性类型。</summary>
        public StatType StatType => _statType;

        /// <summary>获取基础属性数值。</summary>
        public float Value => _value;

        /// <summary>创建运行时基础属性定义。</summary>
        public static EquipmentBaseStatDefinition CreateRuntime(
            StatType statType,
            float value)
        {
            return new EquipmentBaseStatDefinition
            {
                _statType = statType,
                _value = value
            };
        }
    }
}
