using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Train.Equipment.Core
{
    /// <summary>
    /// 描述套装达到指定件数时激活的一档不可变奖励。
    /// </summary>
    public sealed class EquipmentSetBonusSpec
    {
        private readonly ReadOnlyCollection<StatModifier> _modifiers;

        public EquipmentSetBonusSpec(
            int requiredPieceCount,
            IEnumerable<StatModifier> modifiers)
        {
            if (requiredPieceCount < 2)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredPieceCount),
                    requiredPieceCount,
                    "套装奖励至少需要两件装备才可激活。");
            }

            if (modifiers == null)
            {
                throw new ArgumentNullException(nameof(modifiers));
            }

            var copiedModifiers = modifiers.ToArray();
            if (copiedModifiers.Length == 0)
            {
                throw new ArgumentException(
                    "套装奖励至少需要包含一条属性词条。",
                    nameof(modifiers));
            }

            RequiredPieceCount = requiredPieceCount;
            _modifiers = Array.AsReadOnly(copiedModifiers);
        }

        public int RequiredPieceCount { get; }

        public IReadOnlyList<StatModifier> Modifiers => _modifiers;
    }
}
