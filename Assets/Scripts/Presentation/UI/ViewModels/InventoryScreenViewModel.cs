using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Train.Presentation.UI.ViewModels
{
    /// <summary>
    /// 汇总背包页面一次完整渲染所需的槽位和详情数据。
    /// </summary>
    public sealed class InventoryScreenViewModel
    {
        private readonly ReadOnlyCollection<InventorySlotViewModel> _slots;

        /// <summary>创建一份背包页面视图模型。</summary>
        public InventoryScreenViewModel(
            long revision,
            int capacity,
            int occupiedCount,
            int selectedSlotIndex,
            InventorySlotViewModel[] slots,
            InventoryItemDetailViewModel detail)
        {
            Revision = revision;
            Capacity = capacity;
            OccupiedCount = occupiedCount;
            SelectedSlotIndex = selectedSlotIndex;
            _slots = Array.AsReadOnly(
                slots ?? throw new ArgumentNullException(nameof(slots)));
            Detail = detail ?? InventoryItemDetailViewModel.Empty;
        }

        /// <summary>获取此视图模型对应的背包修订号。</summary>
        public long Revision { get; }

        /// <summary>获取界面显示的总槽位容量。</summary>
        public int Capacity { get; }

        /// <summary>获取已经占用的槽位数量。</summary>
        public int OccupiedCount { get; }

        /// <summary>获取当前选中的槽位索引。</summary>
        public int SelectedSlotIndex { get; }

        /// <summary>获取按索引排列的只读槽位视图模型。</summary>
        public IReadOnlyList<InventorySlotViewModel> Slots => _slots;

        /// <summary>获取当前选中物品的详情数据。</summary>
        public InventoryItemDetailViewModel Detail { get; }
    }
}
