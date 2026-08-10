using System;
using NUnit.Framework;
using Train.Architecture.Events;
using Train.Characters.Application;
using Train.Characters.Data;
using Train.Presentation.UI.Contracts;
using Train.Presentation.UI.Presenters;
using Train.Presentation.UI.ViewModels;
using UnityEditor;

namespace Train.Tests.EditMode.UI
{
    /// <summary>
    /// 验证角色 Presenter 的首次渲染、卡片选择、切换、解锁和关闭意图。
    /// </summary>
    public sealed class CharacterPresenterTests
    {
        private EventBus _events;
        private CharacterRosterService _roster;
        private FakeCharacterView _view;
        private CharacterPresenter _presenter;
        private bool _closed;

        /// <summary>使用真实角色设置资源创建名册与 Presenter。</summary>
        [SetUp]
        public void SetUp()
        {
            var settings =
                AssetDatabase.LoadAssetAtPath<CharacterSettings>(
                    "Assets/Data/Characters/DefaultCharacterSettings.asset");
            Assert.That(settings, Is.Not.Null);

            _events = new EventBus();
            _roster = new CharacterRosterService(settings, _events);
            _view = new FakeCharacterView();
            _presenter = new CharacterPresenter(
                _roster,
                _events,
                _view,
                () => _closed = true);
        }

        /// <summary>释放 Presenter 与服务。</summary>
        [TearDown]
        public void TearDown()
        {
            _presenter?.Dispose();
            _roster?.Dispose();
            _events?.Dispose();
        }

        /// <summary>验证页面展示三名角色并默认选中艾莲。</summary>
        [Test]
        public void Constructor_RendersRosterAndCurrentAgent()
        {
            Assert.That(_view.LastModel, Is.Not.Null);
            Assert.That(_view.LastModel.Entries, Has.Count.EqualTo(3));
            Assert.That(
                _view.LastModel.SelectedCharacterId,
                Is.EqualTo("character.ellen"));
            Assert.That(_view.LastModel.IsSelected, Is.True);
        }

        /// <summary>验证选择未解锁角色后可以解锁并切换。</summary>
        [Test]
        public void SelectAndUnlock_UpdateRosterAndFeedback()
        {
            _view.RaiseEntrySelected(1);
            _view.RaiseUnlockRequested();

            Assert.That(_roster.Snapshot.UnlockedCount, Is.EqualTo(2));
            Assert.That(_view.LastModel.FeedbackText, Is.Not.Empty);

            _view.RaiseSelectRequested();

            Assert.That(
                _roster.Snapshot.SelectedCharacterId,
                Is.EqualTo("character.belle"));
        }

        /// <summary>验证关闭按钮只上报意图。</summary>
        [Test]
        public void CloseRequested_InvokesInjectedMenuClose()
        {
            _view.RaiseCloseRequested();

            Assert.That(_closed, Is.True);
        }

        /// <summary>记录 Presenter 输出并模拟玩家意图的假视图。</summary>
        private sealed class FakeCharacterView : ICharacterView
        {
            /// <inheritdoc />
            public event Action<int> EntrySelected;

            /// <inheritdoc />
            public event Action SelectRequested;

            /// <inheritdoc />
            public event Action UnlockRequested;

            /// <inheritdoc />
            public event Action CloseRequested;

            /// <summary>最近一次完整渲染模型。</summary>
            public CharacterScreenViewModel LastModel { get; private set; }

            /// <inheritdoc />
            public void Render(CharacterScreenViewModel viewModel)
            {
                LastModel = viewModel;
            }

            /// <summary>模拟选择角色卡片。</summary>
            public void RaiseEntrySelected(int index)
            {
                EntrySelected?.Invoke(index);
            }

            /// <summary>模拟点击切换。</summary>
            public void RaiseSelectRequested()
            {
                SelectRequested?.Invoke();
            }

            /// <summary>模拟点击解锁。</summary>
            public void RaiseUnlockRequested()
            {
                UnlockRequested?.Invoke();
            }

            /// <summary>模拟点击关闭。</summary>
            public void RaiseCloseRequested()
            {
                CloseRequested?.Invoke();
            }
        }
    }
}
