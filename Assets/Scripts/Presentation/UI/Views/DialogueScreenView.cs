using System;
using TMPro;
using Train.Presentation.UI.Contracts;
using Train.Presentation.UI.ViewModels;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Train.Presentation.UI.Views
{
    /// <summary>
    /// 对话浮层视图。
    /// 负责显示说话人、台词、选项与继续按钮，并上报玩家意图。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DialogueScreenView :
        MonoBehaviour,
        IDialogueView
    {
        [SerializeField] private GameObject _screenRoot;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _speaker;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button[] _choiceButtons = Array.Empty<Button>();
        [SerializeField] private TMP_Text[] _choiceTexts = Array.Empty<TMP_Text>();
        private bool _awaitingChoice;

        /// <inheritdoc />
        public event Action ContinueRequested;

        /// <inheritdoc />
        public event Action<int> ChoiceRequested;

        /// <inheritdoc />
        public event Action CloseRequested;

        /// <summary>获取对话浮层是否可见。</summary>
        public bool IsVisible =>
            _screenRoot != null && _screenRoot.activeSelf;

        /// <summary>切换对话浮层可见性。</summary>
        public void SetVisible(bool visible)
        {
            if (_screenRoot != null)
            {
                _screenRoot.SetActive(visible);
            }
        }

        /// <summary>
        /// 配置对话浮层的全部控件引用。
        /// </summary>
        public void Configure(
            GameObject screenRoot,
            TMP_Text title,
            TMP_Text speaker,
            TMP_Text text,
            Button continueButton,
            Button closeButton,
            Button[] choiceButtons,
            TMP_Text[] choiceTexts)
        {
            _screenRoot = screenRoot;
            _title = title;
            _speaker = speaker;
            _text = text;
            _continueButton = continueButton;
            _closeButton = closeButton;
            _choiceButtons = choiceButtons;
            _choiceTexts = choiceTexts;
        }

        /// <inheritdoc />
        public void Render(DialogueScreenViewModel viewModel)
        {
            if (viewModel == null)
            {
                return;
            }

            SetVisible(viewModel.IsActive);
            if (!viewModel.IsActive)
            {
                return;
            }

            _title.text = viewModel.Title;
            _speaker.text = viewModel.SpeakerId;
            _text.text = viewModel.Text;
            _continueButton.gameObject.SetActive(
                !viewModel.AwaitingChoice);
            _awaitingChoice = viewModel.AwaitingChoice;
            for (var index = 0; index < _choiceButtons.Length; index++)
            {
                var visible =
                    viewModel.AwaitingChoice &&
                    index < viewModel.ChoiceTexts.Count;
                _choiceButtons[index].gameObject.SetActive(visible);
                if (visible)
                {
                    _choiceTexts[index].text =
                        viewModel.ChoiceTexts[index];
                }
            }
        }

        /// <summary>
        /// 支持空格/回车继续、F1-F4 选择选项，数字键 1-4 保留给主动道具。
        /// </summary>
        private void Update()
        {
            if (!IsVisible || Keyboard.current == null)
            {
                return;
            }

            if (_awaitingChoice)
            {
                var keys = new[]
                {
                    Keyboard.current.f1Key,
                    Keyboard.current.f2Key,
                    Keyboard.current.f3Key,
                    Keyboard.current.f4Key
                };
                for (var index = 0; index < keys.Length; index++)
                {
                    if (keys[index].wasPressedThisFrame)
                    {
                        ChoiceRequested?.Invoke(index);
                        return;
                    }
                }

                return;
            }

            if (Keyboard.current.spaceKey.wasPressedThisFrame ||
                Keyboard.current.enterKey.wasPressedThisFrame)
            {
                ContinueRequested?.Invoke();
            }
        }

        /// <summary>绑定按钮事件。</summary>
        private void Awake()
        {
            _continueButton?.onClick.AddListener(
                () => ContinueRequested?.Invoke());
            _closeButton?.onClick.AddListener(
                () => CloseRequested?.Invoke());

            for (var index = 0; index < _choiceButtons.Length; index++)
            {
                var captured = index;
                _choiceButtons[index]?.onClick.AddListener(
                    () => ChoiceRequested?.Invoke(captured));
            }
        }
    }
}
