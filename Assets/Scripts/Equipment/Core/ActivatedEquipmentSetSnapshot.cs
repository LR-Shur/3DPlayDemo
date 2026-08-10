using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Train.Equipment.Core
{
    /// <summary>
    /// 表示某套装在特定装备快照中已激活的件数档位。
    /// </summary>
    public sealed class ActivatedEquipmentSetSnapshot
    {
        private readonly ReadOnlyCollection<int> _activeBonusPieceCounts;

        internal ActivatedEquipmentSetSnapshot(
            string setId,
            string displayName,
            int equippedPieceCount,
            int[] activeBonusPieceCounts)
        {
            SetId = setId ??
                throw new ArgumentNullException(nameof(setId));
            DisplayName = displayName ??
                throw new ArgumentNullException(nameof(displayName));
            EquippedPieceCount = equippedPieceCount;
            _activeBonusPieceCounts = Array.AsReadOnly(
                activeBonusPieceCounts ??
                throw new ArgumentNullException(
                    nameof(activeBonusPieceCounts)));
        }

        public string SetId { get; }

        public string DisplayName { get; }

        public int EquippedPieceCount { get; }

        public IReadOnlyList<int> ActiveBonusPieceCounts =>
            _activeBonusPieceCounts;
    }
}
