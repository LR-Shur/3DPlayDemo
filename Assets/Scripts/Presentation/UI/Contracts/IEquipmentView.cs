using System;
using Train.Equipment.Core;
using Train.Presentation.UI.ViewModels;

namespace Train.Presentation.UI.Contracts
{
    /// <summary>
    /// 装备页面的被动视图接口。
    /// View 只上报按钮意图并渲染 Presenter 提供的完整快照。
    /// </summary>
    public interface IEquipmentView
    {
        /// <summary>玩家选择目录卡片时触发。</summary>
        event Action<int> ItemSelected;

        /// <summary>玩家选择一个装备槽位时触发。</summary>
        event Action<EquipmentSlot> SlotSelected;

        /// <summary>点击主动道具槽位时请求绑定下一个可用消耗品。</summary>
        event Action<int> ActiveItemSlotSelected;

        /// <summary>玩家点击装备按钮时触发。</summary>
        event Action EquipRequested;

        /// <summary>玩家点击卸下按钮时触发。</summary>
        event Action UnequipRequested;

        /// <summary>玩家请求关闭主菜单时触发。</summary>
        event Action CloseRequested;

        /// <summary>渲染装备页面完整视图模型。</summary>
        void Render(EquipmentScreenViewModel viewModel);
    }
}
