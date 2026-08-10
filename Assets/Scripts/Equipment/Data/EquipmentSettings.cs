using System.Collections.Generic;
using UnityEngine;

namespace Train.Equipment.Data
{
    /// <summary>
    /// 汇总装备系统所需的基础属性、装备目录、套装目录和初始穿戴配置。
    /// </summary>
    [CreateAssetMenu(
        menuName = "Train/Equipment/Equipment Settings",
        fileName = "EquipmentSettings")]
    public sealed class EquipmentSettings : ScriptableObject
    {
        [SerializeField]
        private List<EquipmentBaseStatDefinition> _baseStats = new();
        [SerializeField]
        private List<EquipmentItemDefinition> _items = new();
        [SerializeField]
        private List<EquipmentSetDefinition> _sets = new();
        [SerializeField]
        private List<StartingEquipmentDefinition> _startingEquipment = new();

        /// <summary>获取只读角色基础属性配置列表。</summary>
        public IReadOnlyList<EquipmentBaseStatDefinition> BaseStats =>
            _baseStats;

        /// <summary>获取只读装备目录。</summary>
        public IReadOnlyList<EquipmentItemDefinition> Items => _items;

        /// <summary>获取只读套装目录。</summary>
        public IReadOnlyList<EquipmentSetDefinition> Sets => _sets;

        /// <summary>获取只读初始穿戴配置列表。</summary>
        public IReadOnlyList<StartingEquipmentDefinition>
            StartingEquipment => _startingEquipment;
    }
}
