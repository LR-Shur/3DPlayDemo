using System;
using Train.Equipment.Core;
using UnityEngine;

namespace Train.Equipment.Data
{
    /// <summary>
    /// 描述创建装备服务时自动穿戴的一件装备及其目标槽位。
    /// </summary>
    [Serializable]
    public sealed class StartingEquipmentDefinition
    {
        [SerializeField] private EquipmentItemDefinition _item;
        [SerializeField] private EquipmentSlot _targetSlot;

        /// <summary>获取需要自动穿戴的装备配置。</summary>
        public EquipmentItemDefinition Item => _item;

        /// <summary>获取自动穿戴时使用的目标槽位。</summary>
        public EquipmentSlot TargetSlot => _targetSlot;
    }
}
