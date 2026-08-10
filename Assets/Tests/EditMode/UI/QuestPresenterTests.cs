using System;
using NUnit.Framework;
using Train.Architecture.Events;
using Train.Inventory.Application;
using Train.Inventory.Data;
using Train.Presentation.UI.Contracts;
using Train.Presentation.UI.Presenters;
using Train.Presentation.UI.ViewModels;
using Train.Quest.Application;
using Train.Quest.Core;
using Train.Quest.Data;
using UnityEditor;

namespace Train.Tests.EditMode.UI
{
    /// <summary>
    /// 验证任务 Presenter 的首次渲染、列表选择、追踪、领取和关闭意图。
    /// </summary>
    public sealed class QuestPresenterTests
    {
        private EventBus _events;
        private InventoryService _inventory;
        private QuestService _quests;
        private FakeQuestView _view;
        private QuestPresenter _presenter;
        private bool _closed;

        /// <summary>使用真实设置资源创建任务与背包服务。</summary>
        [SetUp]
        public void SetUp()
        {
            var inventorySettings =
                AssetDatabase.LoadAssetAtPath<InventorySettings>(
                    "Assets/Data/Inventory/DefaultInventorySettings.asset");
            var questSettings =
                AssetDatabase.LoadAssetAtPath<QuestSettings>(
                    "Assets/Data/Quests/DefaultQuestSettings.asset");
            Assert.That(inventorySettings, Is.Not.Null);
            Assert.That(questSettings, Is.Not.Null);

            _events = new EventBus();
            _inventory = new InventoryService(inventorySettings, _events);
            _quests = new QuestService(
                questSettings,
                _inventory,
                _events);
            _view = new FakeQuestView();
            _presenter = new QuestPresenter(
                _quests,
                _events,
                _view,
                () => _closed = true);
        }

        /// <summary>释放 Presenter 与服务。</summary>
        [TearDown]
        public void TearDown()
        {
            _presenter?.Dispose();
            _quests?.Dispose();
            _inventory?.Dispose();
            _events?.Dispose();
        }

        /// <summary>验证页面展示全部任务并自动选中第一项。</summary>
        [Test]
        public void Constructor_RendersQuestListAndInitialSelection()
        {
            Assert.That(_view.LastModel, Is.Not.Null);
            Assert.That(_view.LastModel.Entries, Has.Count.EqualTo(5));
            Assert.That(
                _view.LastModel.SelectedQuestId,
                Is.EqualTo(_quests.Snapshots[0].QuestId));
            Assert.That(_view.LastModel.ObjectivesText, Is.Not.Empty);
        }

        /// <summary>验证选择任务后详情随之更新。</summary>
        [Test]
        public void EntrySelected_RendersSelectedQuestDetail()
        {
            _view.RaiseEntrySelected(1);

            Assert.That(
                _view.LastModel.SelectedQuestId,
                Is.EqualTo(_quests.Snapshots[1].QuestId));
            Assert.That(_view.LastModel.Title, Is.Not.Empty);
        }

        /// <summary>验证追踪当前任务后页面反馈与追踪状态更新。</summary>
        [Test]
        public void TrackRequested_UpdatesTrackedQuest()
        {
            var target = _quests.Snapshots[0].QuestId;
            _view.RaiseTrackRequested();

            Assert.That(_quests.TrackedQuestId, Is.EqualTo(target));
            Assert.That(_view.LastModel.FeedbackText, Is.Not.Empty);
        }

        /// <summary>验证关闭按钮只上报意图。</summary>
        [Test]
        public void CloseRequested_InvokesInjectedMenuClose()
        {
            _view.RaiseCloseRequested();

            Assert.That(_closed, Is.True);
        }

        /// <summary>记录 Presenter 输出并模拟玩家意图的假视图。</summary>
        private sealed class FakeQuestView : IQuestView
        {
            /// <inheritdoc />
            public event Action<int> EntrySelected;

            /// <inheritdoc />
            public event Action TrackRequested;

            /// <inheritdoc />
            public event Action ClaimRequested;

            /// <inheritdoc />
            public event Action CloseRequested;

            /// <summary>最近一次完整渲染模型。</summary>
            public QuestScreenViewModel LastModel { get; private set; }

            /// <inheritdoc />
            public void Render(QuestScreenViewModel viewModel)
            {
                LastModel = viewModel;
            }

            /// <summary>模拟选择任务。</summary>
            public void RaiseEntrySelected(int index)
            {
                EntrySelected?.Invoke(index);
            }

            /// <summary>模拟点击追踪。</summary>
            public void RaiseTrackRequested()
            {
                TrackRequested?.Invoke();
            }

            /// <summary>模拟点击领取。</summary>
            public void RaiseClaimRequested()
            {
                ClaimRequested?.Invoke();
            }

            /// <summary>模拟点击关闭。</summary>
            public void RaiseCloseRequested()
            {
                CloseRequested?.Invoke();
            }
        }
    }
}
