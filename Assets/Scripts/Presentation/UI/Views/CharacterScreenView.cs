using System;
using TMPro;
using Train.Presentation.UI.Contracts;
using Train.Presentation.UI.ViewModels;
using UnityEngine;
using UnityEngine.UI;

namespace Train.Presentation.UI.Views
{
    /// <summary>
    /// 商业化角色名册页面。
    /// 只渲染角色卡片、详情、切换和解锁按钮，不直接访问名册服务。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterScreenView :
        MonoBehaviour,
        ICharacterView
    {
        [SerializeField] private GameObject _screenRoot;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button[] _entryButtons = Array.Empty<Button>();
        [SerializeField] private Image[] _entryBackgrounds = Array.Empty<Image>();
        [SerializeField] private TMP_Text[] _entryNames = Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text[] _entryRoles = Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text _detailName;
        [SerializeField] private TMP_Text _detailDescription;
        [SerializeField] private TMP_Text _detailFaction;
        [SerializeField] private TMP_Text _detailRole;
        [SerializeField] private TMP_Text _detailStatus;
        [SerializeField] private Button _selectButton;
        [SerializeField] private Button _unlockButton;
        [SerializeField] private TMP_Text _feedback;

        /// <inheritdoc />
        public event Action<int> EntrySelected;

        /// <inheritdoc />
        public event Action SelectRequested;

        /// <inheritdoc />
        public event Action UnlockRequested;

        /// <inheritdoc />
        public event Action CloseRequested;

        /// <summary>获取角色页面当前是否可见。</summary>
        public bool IsVisible =>
            _screenRoot != null && _screenRoot.activeSelf;

        /// <summary>切换角色页面可见性。</summary>
        public void SetVisible(bool visible)
        {
            if (_screenRoot != null)
            {
                _screenRoot.SetActive(visible);
            }
        }

        /// <summary>
        /// 配置角色页面的根节点、列表和详情控件引用。
        /// </summary>
        public void Configure(
            GameObject screenRoot,
            Button closeButton,
            Button[] entryButtons,
            Image[] entryBackgrounds,
            TMP_Text[] entryNames,
            TMP_Text[] entryRoles,
            TMP_Text detailName,
            TMP_Text detailDescription,
            TMP_Text detailFaction,
            TMP_Text detailRole,
            TMP_Text detailStatus,
            Button selectButton,
            Button unlockButton,
            TMP_Text feedback)
        {
            _screenRoot = screenRoot;
            _closeButton = closeButton;
            _entryButtons = entryButtons;
            _entryBackgrounds = entryBackgrounds;
            _entryNames = entryNames;
            _entryRoles = entryRoles;
            _detailName = detailName;
            _detailDescription = detailDescription;
            _detailFaction = detailFaction;
            _detailRole = detailRole;
            _detailStatus = detailStatus;
            _selectButton = selectButton;
            _unlockButton = unlockButton;
            _feedback = feedback;
        }

        /// <inheritdoc />
        public void Render(CharacterScreenViewModel viewModel)
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
                _entryNames[index].text = entry.DisplayName;
                _entryRoles[index].text = entry.IsUnlocked
                    ? entry.CombatRole
                    : "LOCKED // 未解锁";
                _entryBackgrounds[index].color = entry.IsSelected
                    ? new Color32(28, 82, 96, 252)
                    : new Color32(20, 35, 54, 245);
            }

            _detailName.text = viewModel.DisplayName;
            _detailDescription.text = viewModel.Description;
            _detailFaction.text = $"阵营  {viewModel.Faction}";
            _detailRole.text = $"定位  {viewModel.CombatRole}";
            _detailStatus.text = viewModel.IsSelected
                ? "当前代理人"
                : viewModel.IsUnlocked
                    ? "已解锁"
                    : "未解锁";
            _selectButton.interactable =
                viewModel.IsUnlocked && !viewModel.IsSelected;
            _unlockButton.interactable = viewModel.CanUnlock;
            _feedback.text = viewModel.FeedbackText;
        }

        /// <summary>绑定按钮事件。</summary>
        private void Awake()
        {
            _closeButton?.onClick.AddListener(
                () => CloseRequested?.Invoke());
            _selectButton?.onClick.AddListener(
                () => SelectRequested?.Invoke());
            _unlockButton?.onClick.AddListener(
                () => UnlockRequested?.Invoke());

            for (var index = 0; index < _entryButtons.Length; index++)
            {
                var captured = index;
                _entryButtons[index]?.onClick.AddListener(
                    () => EntrySelected?.Invoke(captured));
            }
        }
    }
}
