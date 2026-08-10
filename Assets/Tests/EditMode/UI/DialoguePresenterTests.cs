using System;
using NUnit.Framework;
using Train.Architecture.Events;
using Train.Architecture.Input;
using Train.Dialogue.Application;
using Train.Dialogue.Data;
using Train.Presentation.UI.Contracts;
using Train.Presentation.UI.Presenters;
using Train.Presentation.UI.Runtime;
using Train.Presentation.UI.ViewModels;
using UnityEditor;

namespace Train.Tests.EditMode.UI
{
    /// <summary>
    /// 验证对话 Presenter 在会话开始、继续、选择和取消时正确驱动浮层与模态输入。
    /// </summary>
    public sealed class DialoguePresenterTests
    {
        private EventBus _events;
        private DialogueService _dialogue;
        private InputModeService _inputMode;
        private FakeDialogueView _view;
        private DialoguePresenter _presenter;

        /// <summary>创建对话服务、输入模式和 Presenter。</summary>
        [SetUp]
        public void SetUp()
        {
            var settings =
                AssetDatabase.LoadAssetAtPath<DialogueSettings>(
                    "Assets/Data/Dialogue/DefaultDialogueSettings.asset");
            Assert.That(settings, Is.Not.Null);

            _events = new EventBus();
            _dialogue = new DialogueService(settings, _events);
            _inputMode = new InputModeService(_events);
            _view = new FakeDialogueView();
            _presenter = new DialoguePresenter(
                _dialogue,
                _events,
                _inputMode,
                _view);
        }

        /// <summary>释放 Presenter、服务和事件中心。</summary>
        [TearDown]
        public void TearDown()
        {
            _presenter?.Dispose();
            _dialogue?.Dispose();
            _inputMode?.Dispose();
            _events?.Dispose();
        }

        /// <summary>验证会话启动后浮层显示、模态租约生效。</summary>
        [Test]
        public void Started_ShowsDialogueAndAcquiresModal()
        {
            _dialogue.Start("dialogue_training_operator");

            Assert.That(_view.LastModel.IsActive, Is.True);
            Assert.That(_view.LastModel.Title, Is.Not.Empty);
            Assert.That(_inputMode.IsModalActive, Is.True);
        }

        /// <summary>验证继续指令推进到选项节点并显示选项文本。</summary>
        [Test]
        public void Continue_AdvancesToChoice()
        {
            _dialogue.Start("dialogue_training_operator");
            _view.RaiseContinueRequested();

            Assert.That(_view.LastModel.AwaitingChoice, Is.True);
            Assert.That(_view.LastModel.ChoiceTexts, Has.Count.GreaterThan(0));
        }

        /// <summary>验证选择选项后继续推进并保持浮层显示。</summary>
        [Test]
        public void Choice_AdvancesDialogueAndKeepsModal()
        {
            _dialogue.Start("dialogue_training_operator");
            _view.RaiseContinueRequested();

            _view.RaiseChoiceRequested(0);

            Assert.That(_dialogue.IsActive, Is.True);
            Assert.That(_inputMode.IsModalActive, Is.True);
        }

        /// <summary>验证取消对话后浮层隐藏且模态租约释放。</summary>
        [Test]
        public void Close_CancelsDialogueAndReleasesModal()
        {
            _dialogue.Start("dialogue_training_operator");

            _view.RaiseCloseRequested();

            Assert.That(_dialogue.IsActive, Is.False);
            Assert.That(_inputMode.IsModalActive, Is.False);
            Assert.That(_view.LastModel.IsActive, Is.False);
        }

        /// <summary>记录 Presenter 输出并模拟玩家意图的假视图。</summary>
        private sealed class FakeDialogueView : IDialogueView
        {
            /// <inheritdoc />
            public event Action ContinueRequested;

            /// <inheritdoc />
            public event Action<int> ChoiceRequested;

            /// <inheritdoc />
            public event Action CloseRequested;

            /// <summary>最近一次完整渲染模型。</summary>
            public DialogueScreenViewModel LastModel { get; private set; }

            /// <inheritdoc />
            public void Render(DialogueScreenViewModel viewModel)
            {
                LastModel = viewModel;
            }

            /// <summary>模拟点击继续。</summary>
            public void RaiseContinueRequested()
            {
                ContinueRequested?.Invoke();
            }

            /// <summary>模拟选择一个选项。</summary>
            public void RaiseChoiceRequested(int index)
            {
                ChoiceRequested?.Invoke(index);
            }

            /// <summary>模拟点击取消。</summary>
            public void RaiseCloseRequested()
            {
                CloseRequested?.Invoke();
            }
        }
    }
}
