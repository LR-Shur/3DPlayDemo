using System;
using System.Collections.Generic;
using Train.Equipment.Core;
using UnityEngine;

namespace Train.Equipment.Data
{
    /// <summary>
    /// 描述一套装备的稳定标识、显示名称和按件数激活的奖励。
    /// </summary>
    [CreateAssetMenu(
        menuName = "Train/Equipment/Set Definition",
        fileName = "EquipmentSetDefinition")]
    public sealed class EquipmentSetDefinition : ScriptableObject
    {
        [SerializeField] private string _setId;
        [SerializeField] private string _displayName;
        [SerializeField]
        private List<EquipmentSetBonusDefinition> _bonuses = new();

        /// <summary>获取套装稳定标识。</summary>
        public string SetId => _setId;

        /// <summary>获取玩家可见的套装名称。</summary>
        public string DisplayName => _displayName;

        /// <summary>获取只读套装奖励档位列表。</summary>
        public IReadOnlyList<EquipmentSetBonusDefinition> Bonuses => _bonuses;

        /// <summary>将 Unity 套装配置转换为不可变的纯领域定义。</summary>
        public EquipmentSetSpec ToCoreSpec()
        {
            var bonuses = new EquipmentSetBonusSpec[_bonuses.Count];
            for (var i = 0; i < _bonuses.Count; i++)
            {
                var bonus = _bonuses[i] ??
                    throw new InvalidOperationException(
                        $"套装 '{_setId}' 的奖励列表包含 null。");
                bonuses[i] = bonus.ToCoreSpec();
            }

            return new EquipmentSetSpec(
                _setId,
                _displayName,
                bonuses);
        }
    }
}
