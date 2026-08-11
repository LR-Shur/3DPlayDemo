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

        /// <summary>创建由 Luban 装备表驱动的运行时配置，默认不穿戴任何装备。</summary>
        public static EquipmentSettings CreateRuntime(
            IReadOnlyList<EquipmentBaseStatDefinition> baseStats,
            IReadOnlyList<EquipmentItemDefinition> items)
        {
            var settings = CreateInstance<EquipmentSettings>();
            settings.name = "LubanEquipmentSettings";
            settings._baseStats = baseStats != null
                ? new List<EquipmentBaseStatDefinition>(baseStats)
                : new List<EquipmentBaseStatDefinition>();
            settings._items = items != null
                ? new List<EquipmentItemDefinition>(items)
                : new List<EquipmentItemDefinition>();
            settings._sets = new List<EquipmentSetDefinition>();
            settings._startingEquipment = new List<StartingEquipmentDefinition>();
            return settings;
        }
    }
}
