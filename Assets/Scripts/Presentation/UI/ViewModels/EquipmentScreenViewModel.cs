using System;
using System.Collections.Generic;
using Train.Equipment.Core;
using Train.Equipment.Data;
using UnityEngine;

namespace Train.Presentation.UI.ViewModels
{
    /// <summary>
    /// 装备页面一次完整渲染所需的不可变数据集合。
    /// </summary>
    public sealed class EquipmentScreenViewModel
    {
        /// <summary>创建装备页面视图模型。</summary>
        public EquipmentScreenViewModel(
            long revision,
            IReadOnlyList<EquipmentSlotViewModel> slots,
            IReadOnlyList<EquipmentItemViewModel> items,
            IReadOnlyList<EquipmentStatViewModel> stats,
            EquipmentSlot selectedSlot,
            string selectedName,
            string selectedDescription,
            EquipmentRarity selectedRarity,
            Sprite selectedIcon,
            string modifiersText,
            string activeSetsText,
            bool canEquip,
            bool canUnequip)
        {
            Revision = revision;
            Slots = slots ?? throw new ArgumentNullException(nameof(slots));
            Items = items ?? throw new ArgumentNullException(nameof(items));
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
            SelectedSlot = selectedSlot;
            SelectedName = selectedName ?? string.Empty;
            SelectedDescription = selectedDescription ?? string.Empty;
            SelectedRarity = selectedRarity;
            SelectedIcon = selectedIcon;
            ModifiersText = modifiersText ?? string.Empty;
            ActiveSetsText = activeSetsText ?? string.Empty;
            CanEquip = canEquip;
            CanUnequip = canUnequip;
        }

        /// <summary>装备领域快照修订号。</summary>
        public long Revision { get; }

        /// <summary>十个装备与饰品槽位。</summary>
        public IReadOnlyList<EquipmentSlotViewModel> Slots { get; }

        /// <summary>背包拥有的候选装备目录。</summary>
        public IReadOnlyList<EquipmentItemViewModel> Items { get; }

        /// <summary>最终属性对照行。</summary>
        public IReadOnlyList<EquipmentStatViewModel> Stats { get; }

        /// <summary>当前目标槽位。</summary>
        public EquipmentSlot SelectedSlot { get; }

        /// <summary>当前候选装备名称。</summary>
        public string SelectedName { get; }

        /// <summary>当前候选装备说明。</summary>
        public string SelectedDescription { get; }

        /// <summary>当前候选装备品质。</summary>
        public EquipmentRarity SelectedRarity { get; }

        /// <summary>当前候选装备图标。</summary>
        public Sprite SelectedIcon { get; }

        /// <summary>当前候选装备词条说明。</summary>
        public string ModifiersText { get; }

        /// <summary>当前激活套装说明。</summary>
        public string ActiveSetsText { get; }

        /// <summary>当前候选能否装备到目标槽。</summary>
        public bool CanEquip { get; }

        /// <summary>目标槽当前能否卸下。</summary>
        public bool CanUnequip { get; }
    }
}
