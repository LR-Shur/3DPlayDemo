using System;
using TMPro;
using Train.Presentation.UI.Contracts;
using Train.Presentation.UI.ViewModels;
using UnityEngine;
using UnityEngine.UI;

namespace Train.Presentation.UI.Views
{
    /// <summary>
    /// 商业化任务委托页面。
    /// 只渲染任务列表、详情、追踪和领取按钮，不直接访问任务服务。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QuestScreenView :
        MonoBehaviour,
        IQuestView
    {
        [SerializeField] private GameObject _screenRoot;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button[] _entryButtons = Array.Empty<Button>();
        [SerializeField] private Image[] _entryBackgrounds = Array.Empty<Image>();
        [SerializeField] private TMP_Text[] _entryTitles = Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text[] _entryStatuses = Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text[] _entryProgresses = Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text _detailTitle;
        [SerializeField] private TMP_Text _detailDescription;
        [SerializeField] private TMP_Text _detailObjectives;
        [SerializeField] private TMP_Text _detailRewards;
        [SerializeField] private TMP_Text _detailStatus;
        [SerializeField] private Button _trackButton;
        [SerializeField] private Button _claimButton;
        [SerializeField] private TMP_Text _feedback;

        /// <inheritdoc />
        public event Action<int> EntrySelected;

        /// <inheritdoc />
        public event Action TrackRequested;

        /// <inheritdoc />
        public event Action ClaimRequested;

        /// <inheritdoc />
        public event Action CloseRequested;

        /// <summary>获取任务页面当前是否可见。</summary>
        public bool IsVisible =>
            _screenRoot != null && _screenRoot.activeSelf;

        /// <summary>切换任务页面可见性。</summary>
        public void SetVisible(bool visible)
        {
            if (_screenRoot != null)
            {
                _screenRoot.SetActive(visible);
            }
        }

        /// <summary>
        /// 配置任务页面的根节点、列表和详情控件引用。
        /// </summary>
        public void Configure(
            GameObject screenRoot,
            Button closeButton,
            Button[] entryButtons,
            Image[] entryBackgrounds,
            TMP_Text[] entryTitles,
            TMP_Text[] entryStatuses,
            TMP_Text[] entryProgresses,
            TMP_Text detailTitle,
            TMP_Text detailDescription,
            TMP_Text detailObjectives,
            TMP_Text detailRewards,
            TMP_Text detailStatus,
            Button trackButton,
            Button claimButton,
            TMP_Text feedback)
        {
            _screenRoot = screenRoot;
            _closeButton = closeButton;
            _entryButtons = entryButtons;
            _entryBackgrounds = entryBackgrounds;
            _entryTitles = entryTitles;
            _entryStatuses = entryStatuses;
            _entryProgresses = entryProgresses;
            _detailTitle = detailTitle;
            _detailDescription = detailDescription;
            _detailObjectives = detailObjectives;
            _detailRewards = detailRewards;
            _detailStatus = detailStatus;
            _trackButton = trackButton;
            _claimButton = claimButton;
            _feedback = feedback;
        }

        /// <inheritdoc />
        public void Render(QuestScreenViewModel viewModel)
        {
            if (viewModel == null)
            {
                return;
            }

            for (var index = 0; index < _entryButtons.Length; index++)
            {
                var visible = index < viewModel.Entries.Count;
                _entryButtons[index].gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                var entry = viewModel.Entries[index];
                _entryTitles[index].text = entry.Title;
                _entryStatuses[index].text = entry.StatusText;
                _entryProgresses[index].text = entry.ProgressText;
                _entryBackgrounds[index].color = entry.IsSelected
                    ? new Color32(28, 82, 96, 252)
                    : new Color32(20, 35, 54, 245);
            }

            _detailTitle.text = viewModel.Title;
            _detailDescription.text = viewModel.Description;
            _detailObjectives.text = viewModel.ObjectivesText;
            _detailRewards.text = viewModel.RewardsText;
            _detailStatus.text = viewModel.StatusText;
            _trackButton.interactable = viewModel.CanTrack;
            _claimButton.interactable = viewModel.CanClaim;
            _feedback.text = viewModel.FeedbackText;
        }

        /// <summary>绑定按钮事件。</summary>
        private void Awake()
        {
            _closeButton?.onClick.AddListener(
                () => CloseRequested?.Invoke());
            _trackButton?.onClick.AddListener(
                () => TrackRequested?.Invoke());
            _claimButton?.onClick.AddListener(
                () => ClaimRequested?.Invoke());

            for (var index = 0; index < _entryButtons.Length; index++)
            {
                var captured = index;
                _entryButtons[index]?.onClick.AddListener(
                    () => EntrySelected?.Invoke(captured));
            }
        }
    }
}
