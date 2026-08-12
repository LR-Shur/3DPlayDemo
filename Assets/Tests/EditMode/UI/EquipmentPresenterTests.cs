using System;
using NUnit.Framework;
using Train.Architecture.Events;
using Train.Equipment.Application;
using Train.Equipment.Core;
using Train.Equipment.Data;
using Train.Inventory.Application;
using Train.Inventory.Data;
using Train.Presentation.UI.Contracts;
using Train.Presentation.UI.Presenters;
using Train.Presentation.UI.ViewModels;
using UnityEditor;

namespace Train.Tests.EditMode.UI
{
    /// <summary>
    /// 验证装备 Presenter 的首次渲染、普通换装、饰品目标槽和关闭意图。
    /// </summary>
    public sealed class EquipmentPresenterTests
    {
        private EventBus _events;
        private EquipmentService _equipment;
        private InventoryService _inventory;
        private FakeEquipmentView _view;
        private EquipmentPresenter _presenter;
        private bool _closed;

        /// <summary>使用内容构建器生成的真实设置资源创建测试服务。</summary>
        [SetUp]
        public void SetUp()
        {
            var equipmentSettings =
                AssetDatabase.LoadAssetAtPath<EquipmentSettings>(
                    "Assets/Data/Equipment/DefaultEquipmentSettings.asset");
            var inventorySettings =
                AssetDatabase.LoadAssetAtPath<InventorySettings>(
                    "Assets/Data/Inventory/DefaultInventorySettings.asset");
            Assert.That(equipmentSettings, Is.Not.Null);
            Assert.That(inventorySettings, Is.Not.Null);

            _events = new EventBus();
            _equipment = new EquipmentService(equipmentSettings, _events);
            _inventory = new InventoryService(inventorySettings, _events);
            _view = new FakeEquipmentView();
            _presenter = new EquipmentPresenter(
                _equipment,
                _inventory,
                _events,
                _view,
                () => _closed = true);
        }

        /// <summary>释放 Presenter 与服务，避免事件订阅跨用例残留。</summary>
        [TearDown]
        public void TearDown()
        {
            _presenter?.Dispose();
            _equipment?.Dispose();
            _inventory?.Dispose();
            _events?.Dispose();
        }

        /// <summary>验证页面首次展示十个槽、十八件候选和六项属性。</summary>
        [Test]
        public void Constructor_RendersCompleteCommercialEquipmentPage()
        {
            Assert.That(_view.LastModel, Is.Not.Null);
            Assert.That(_view.LastModel.Slots, Has.Count.EqualTo(10));
            Assert.That(_view.LastModel.Items, Has.Count.EqualTo(18));
            Assert.That(_view.LastModel.Stats, Has.Count.EqualTo(6));
            Assert.That(_view.LastModel.ActiveSetsText, Is.Not.Empty);
        }

        /// <summary>验证选择另一把武器后可以替换当前武器。</summary>
        [Test]
        public void EquipRequested_SelectedWeapon_ReplacesWeaponSlot()
        {
            var index = FindItemIndex("streetbreaker_blade");
            _view.RaiseItemSelected(index);
            _view.RaiseEquipRequested();

            Assert.That(
                _equipment.Snapshot
                    .GetEquippedItem(EquipmentSlot.Weapon)
                    .ItemId,
                Is.EqualTo("streetbreaker_blade"));
        }

        /// <summary>验证通用饰品可以被玩家明确装备到第五饰品槽。</summary>
        [Test]
        public void EquipRequested_Accessory_CanTargetFifthSlot()
        {
            var index = FindItemIndex("guard_badge");
            _view.RaiseItemSelected(index);
            _view.RaiseSlotSelected(EquipmentSlot.Accessory5);
            _view.RaiseEquipRequested();

            Assert.That(
                _equipment.Snapshot
                    .GetEquippedItem(EquipmentSlot.Accessory5)
                    .ItemId,
                Is.EqualTo("guard_badge"));
        }

        /// <summary>验证关闭按钮只上报意图，不由 View 自行操作服务。</summary>
        [Test]
        public void CloseRequested_InvokesInjectedMenuClose()
        {
            _view.RaiseCloseRequested();

            Assert.That(_closed, Is.True);
        }

        /// <summary>在当前渲染目录中按稳定 ID 查找卡片索引。</summary>
        private int FindItemIndex(string itemId)
        {
            for (var index = 0;
                 index < _view.LastModel.Items.Count;
                 index++)
            {
                if (_view.LastModel.Items[index].ItemId == itemId)
                {
                    return index;
                }
            }

            Assert.Fail($"未在装备页面找到测试物品 '{itemId}'。");
            return -1;
        }

        /// <summary>
        /// 记录 Presenter 输出并向其模拟玩家按钮意图的假视图。
        /// </summary>
        private sealed class FakeEquipmentView : IEquipmentView
        {
            /// <inheritdoc />
            public event Action<int> ItemSelected;

            /// <inheritdoc />
            public event Action<EquipmentSlot> SlotSelected;

            public event Action<int> ActiveItemSlotSelected;

            /// <inheritdoc />
            public event Action EquipRequested;

            /// <inheritdoc />
            public event Action UnequipRequested;

            /// <inheritdoc />
            public event Action CloseRequested;

            /// <summary>最近一次完整渲染模型。</summary>
            public EquipmentScreenViewModel LastModel { get; private set; }

            /// <inheritdoc />
            public void Render(EquipmentScreenViewModel viewModel)
            {
                LastModel = viewModel;
            }

            /// <summary>模拟选择候选装备。</summary>
            public void RaiseItemSelected(int index)
            {
                ItemSelected?.Invoke(index);
            }

            /// <summary>模拟选择目标槽位。</summary>
            public void RaiseSlotSelected(EquipmentSlot slot)
            {
                SlotSelected?.Invoke(slot);
            }

            /// <summary>模拟点击装备。</summary>
            public void RaiseEquipRequested()
            {
                EquipRequested?.Invoke();
            }

            /// <summary>模拟点击关闭。</summary>
            public void RaiseCloseRequested()
            {
                CloseRequested?.Invoke();
            }

            /// <summary>保持卸下事件被接口实现并避免编译器误报未使用。</summary>
            public void RaiseUnequipRequested()
            {
                UnequipRequested?.Invoke();
            }
        }
    }
}
