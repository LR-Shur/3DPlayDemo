using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Train.Equipment.Core
{
    /// <summary>
    /// 描述一组不可变的装备套装定义及其按件数递增的奖励档位。
    /// </summary>
    public sealed class EquipmentSetSpec
    {
        private readonly ReadOnlyCollection<EquipmentSetBonusSpec> _bonuses;

        public EquipmentSetSpec(
            string setId,
            string displayName,
            IEnumerable<EquipmentSetBonusSpec> bonuses)
        {
            SetId = ValidateRequiredText(setId, nameof(setId));
            DisplayName = ValidateRequiredText(
                displayName,
                nameof(displayName));

            if (bonuses == null)
            {
                throw new ArgumentNullException(nameof(bonuses));
            }

            var orderedBonuses = bonuses
                .OrderBy(bonus => bonus?.RequiredPieceCount ?? int.MinValue)
                .ToArray();
            if (orderedBonuses.Length == 0)
            {
                throw new ArgumentException(
                    "套装至少需要包含一档奖励。",
                    nameof(bonuses));
            }

            var lastRequiredCount = -1;
            for (var i = 0; i < orderedBonuses.Length; i++)
            {
                var bonus = orderedBonuses[i] ??
                    throw new ArgumentException(
                        "套装奖励列表不能包含 null。",
                        nameof(bonuses));
                if (bonus.RequiredPieceCount == lastRequiredCount)
                {
                    throw new ArgumentException(
                        $"套装奖励件数 {bonus.RequiredPieceCount} 重复。",
                        nameof(bonuses));
                }

                lastRequiredCount = bonus.RequiredPieceCount;
            }

            _bonuses = Array.AsReadOnly(orderedBonuses);
        }

        public string SetId { get; }

        public string DisplayName { get; }

        public IReadOnlyList<EquipmentSetBonusSpec> Bonuses => _bonuses;

        private static string ValidateRequiredText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "套装标识和显示名称不能为 null、空字符串或纯空白。",
                    parameterName);
            }

            return value;
        }
    }
}
