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
    }
}
