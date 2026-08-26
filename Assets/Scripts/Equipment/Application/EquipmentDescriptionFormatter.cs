using System;
using System.Text;
using Train.Equipment.Core;
using Train.Equipment.Data;

namespace Train.Equipment.Application
{
    /// <summary>统一生成装备详情中的描述和套装归属。</summary>
    public static class EquipmentDescriptionFormatter
    {
        public static string Format(
            EquipmentItemDefinition definition,
            IEquipmentService equipment)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            var description = definition.Description ?? string.Empty;
            if (string.IsNullOrWhiteSpace(definition.SetId))
            {
                return description;
            }

            var setName = definition.SetId;
            EquipmentSetDefinition set = null;
            if (equipment != null &&
                equipment.TryGetSetDefinition(
                    definition.SetId,
                    out set) &&
                set != null &&
                !string.IsNullOrWhiteSpace(set.DisplayName))
            {
                setName = set.DisplayName;
            }

            var builder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(description))
            {
                builder.Append(description).Append("\n\n");
            }

            builder.Append("所属套装：").Append(setName);
            if (set == null)
            {
                return builder.ToString();
            }

            var equippedPieceCount = CountEquippedPieces(
                equipment != null ? equipment.Snapshot : null,
                definition.SetId);
            builder.Append("\n当前已装备：")
                .Append(equippedPieceCount)
                .Append('件');

            foreach (var bonus in set.Bonuses)
            {
                if (bonus == null)
                {
                    continue;
                }

                var isActive = equippedPieceCount >= bonus.RequiredPieceCount;
                builder.Append('\n')
                    .Append(bonus.RequiredPieceCount)
                    .Append("件奖励：")
                    .Append(isActive ? "已激活" : "未激活");

                var summary = FormatBonusSummary(bonus);
                if (!string.IsNullOrWhiteSpace(summary))
                {
                    builder.Append('（').Append(summary).Append('）');
                }
            }

            return builder.ToString();
        }

        private static int CountEquippedPieces(
            EquipmentSnapshot snapshot,
            string setId)
        {
            if (snapshot == null)
            {
                return 0;
            }

            var count = 0;
            foreach (var slot in snapshot.Slots)
            {
                if (slot.Item != null &&
                    string.Equals(
                        slot.Item.SetId,
                        setId,
                        StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static string FormatBonusSummary(
            EquipmentSetBonusDefinition bonus)
        {
            if (bonus.Modifiers == null || bonus.Modifiers.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            foreach (var modifier in bonus.Modifiers)
            {
                if (modifier == null)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append("、");
                }

                builder.Append(FormatStat(modifier.StatType))
                    .Append(' ')
                    .Append(FormatModifierValue(modifier));
            }

            return builder.ToString();
        }

        private static string FormatModifierValue(
            EquipmentStatModifierDefinition modifier)
        {
            return modifier.Operation == StatModifierOperation.Flat
                ? $"{(modifier.Value >= 0f ? "+" : string.Empty)}{modifier.Value:0.##}"
                : $"{(modifier.Value >= 0f ? "+" : string.Empty)}{modifier.Value * 100f:0.#}%";
        }

        private static string FormatStat(StatType statType)
        {
            return statType switch
            {
                StatType.MaxHealth => "生命上限",
                StatType.Attack => "攻击力",
                StatType.Defense => "防御力",
                StatType.CritRate => "暴击率",
                StatType.CritDamage => "暴击伤害",
                StatType.ElectricDamageBonus => "雷属性伤害",
                _ => statType.ToString()
            };
        }
    }
}
