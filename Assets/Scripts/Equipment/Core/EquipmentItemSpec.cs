using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Train.Equipment.Core
{
    /// <summary>
    /// 描述一件不可变的装备定义。
    /// 稳定标识用于存档和去重，套装标识为空时表示该装备不属于任何套装。
    /// 普通装备的槽位是唯一合法槽位；饰品声明的槽位是默认槽位，
    /// 实际装备时可在 Accessory1 至 Accessory5 之间自由选择。
    /// </summary>
    public sealed class EquipmentItemSpec
    {
        private readonly ReadOnlyCollection<StatModifier> _modifiers;

        public EquipmentItemSpec(
            string itemId,
            string displayName,
            EquipmentSlot slot,
            string setId,
            IEnumerable<StatModifier> modifiers)
        {
            ItemId = ValidateRequiredText(itemId, nameof(itemId));
            DisplayName = ValidateRequiredText(
                displayName,
                nameof(displayName));
            ValidateSlot(slot);
            Slot = slot;

            if (setId != null && string.IsNullOrWhiteSpace(setId))
            {
                throw new ArgumentException(
                    "套装标识只能为 null 或非空白文本。",
                    nameof(setId));
            }

            SetId = setId;
            var copiedModifiers = modifiers == null
                ? Array.Empty<StatModifier>()
                : modifiers.ToArray();
            _modifiers = Array.AsReadOnly(copiedModifiers);
        }

        public string ItemId { get; }

        public string DisplayName { get; }

        public EquipmentSlot Slot { get; }

        public string SetId { get; }

        public IReadOnlyList<StatModifier> Modifiers => _modifiers;

        private static string ValidateRequiredText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "标识和显示名称不能为 null、空字符串或纯空白。",
                    parameterName);
            }

            return value;
        }

        private static void ValidateSlot(EquipmentSlot slot)
        {
            if (!Enum.IsDefined(typeof(EquipmentSlot), slot))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(slot),
                    slot,
                    "装备槽位不在系统定义范围内。");
            }
        }
    }
}
