using System;
using Train.Architecture.Assets;
using Train.Architecture.Events;
using Train.Architecture.Input;
using Train.Characters.Application;
using Train.Dialogue.Application;
using Train.Equipment.Application;
using Train.GameFlow.Application;
using Train.Inventory.Application;
using Train.Presentation.UI.Presenters;
using Train.Presentation.UI.Views;
using Train.Quest.Application;
using UnityEngine;

namespace Train.Presentation.UI.Runtime
{
    /// <summary>
    /// 管理常驻 UI 根节点、Presenter 生命周期和模态输入租约。
    /// View 不自行解析服务或加载资源。
    /// </summary>
    public sealed class UIService : IUIService, IDisposable
    {
        private readonly IInstanceLease _rootLease;
        private readonly GameUIRootView _root;
        private readonly IInputModeService _inputMode;
        private readonly InventoryPresenter _inventoryPresenter;
        private readonly EquipmentPresenter _equipmentPresenter;
        private readonly QuestPresenter _questPresenter;
        private readonly CharacterPresenter _characterPresenter;
        private readonly DialoguePresenter _dialoguePresenter;
        private readonly HudPresenter _hudPresenter;
        private readonly UIScreenMotion _menuNavigationMotion;
        private IDisposable _menuModalLease;
        private bool _disposed;

        /// <summary>
        /// 创建 UI 服务，连接常驻根节点、Presenter、按钮和快捷键。
        /// </summary>
        public UIService(
            IInstanceLease rootLease,
            IInventoryService inventory,
            IEquipmentService equipment,
            IQuestService quests,
            ICharacterRosterService characters,
            IDialogueService dialogue,
            ILevelSessionRegistry levels,
            IEventBus events,
            IInputModeService inputMode)
        {
            _rootLease =
                rootLease ?? throw new ArgumentNullException(nameof(rootLease));
            _inputMode =
                inputMode ?? throw new ArgumentNullException(nameof(inputMode));
            if (inventory == null)
            {
                throw new ArgumentNullException(nameof(inventory));
            }

            if (equipment == null)
            {
                throw new ArgumentNullException(nameof(equipment));
            }

            if (quests == null)
            {
                throw new ArgumentNullException(nameof(quests));
            }

            if (characters == null)
            {
                throw new ArgumentNullException(nameof(characters));
            }

            if (dialogue == null)
            {
                throw new ArgumentNullException(nameof(dialogue));
            }

            if (levels == null)
            {
                throw new ArgumentNullException(nameof(levels));
            }

            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            var instance = rootLease.Instance;
            if (instance == null)
            {
                throw new InvalidOperationException(
                    "The GameUIRoot asset lease has no instantiated object.");
            }

            _root = instance.GetComponent<GameUIRootView>();
            if (_root == null || !_root.IsValid)
            {
                throw new InvalidOperationException(
                    "GameUIRoot prefab requires a fully configured " +
                    "GameUIRootView component.");
            }

            UnityEngine.Object.DontDestroyOnLoad(instance);
            _menuNavigationMotion = EnsureMenuNavigationMotion();
            SetPageMotionImmediate(_root.Inventory, false);
            SetPageMotionImmediate(_root.Equipment, false);
            _root.Quests.SetVisible(false);
            _root.Characters.SetVisible(false);
            _root.Dialogue.SetVisible(false);
            _root.Placeholder.Hide();
            _menuNavigationMotion.SetVisibleImmediate(false);
            _inventoryPresenter = new InventoryPresenter(
                inventory,
                equipment,
                events,
                _root.Inventory,
                HideMenu);
            _equipmentPresenter = new EquipmentPresenter(
                equipment,
                inventory,
                events,
                _root.Equipment,
                HideMenu);
            _questPresenter = new QuestPresenter(
                quests,
                events,
                _root.Quests,
                HideMenu,
                inventory);
            _characterPresenter = new CharacterPresenter(
                characters,
                events,
                _root.Characters,
                HideMenu);
            _dialoguePresenter = new DialoguePresenter(
                dialogue,
                events,
                inputMode,
                _root.Dialogue);
            _hudPresenter = new HudPresenter(
                levels,
                events,
                _root.Hud);

            _root.InventoryButton.onClick.AddListener(ShowInventory);
            _root.EquipmentButton.onClick.AddListener(ShowEquipment);
            _root.InventoryTabButton.onClick.AddListener(ShowInventory);
            _root.EquipmentTabButton.onClick.AddListener(ShowEquipment);
            _root.QuestTabButton?.onClick.AddListener(
                () => ShowPage(GameMenuPage.Quests));
            _root.CharactersTabButton?.onClick.AddListener(
                () => ShowPage(GameMenuPage.Characters));
            _root.ArchiveTabButton?.onClick.AddListener(ShowArchive);
            _root.MenuCloseButton.onClick.AddListener(HideMenu);
            var hotkeys = instance.GetComponent<UIHotkeyDriver>();
            if (hotkeys == null)
            {
                hotkeys = instance.AddComponent<UIHotkeyDriver>();
            }

            hotkeys.Initialize(this);
        }

        /// <inheritdoc />
        public bool IsMenuOpen =>
            !_disposed && _menuModalLease != null;

        /// <inheritdoc />
        public GameMenuPage CurrentPage { get; private set; } =
            GameMenuPage.Inventory;

        /// <inheritdoc />
        public bool IsInventoryOpen =>
            IsMenuOpen &&
            CurrentPage == GameMenuPage.Inventory &&
            _root.Inventory.IsVisible;

        /// <inheritdoc />
        public bool IsEquipmentOpen =>
            IsMenuOpen &&
            CurrentPage == GameMenuPage.Equipment &&
            _root.Equipment.IsVisible;

        /// <inheritdoc />
        public bool IsQuestOpen =>
            IsMenuOpen &&
            CurrentPage == GameMenuPage.Quests &&
            _root.Quests.IsVisible;

        /// <inheritdoc />
        public void ShowPage(GameMenuPage page)
        {
            ThrowIfDisposed();
            if (!Enum.IsDefined(typeof(GameMenuPage), page))
            {
                throw new ArgumentOutOfRangeException(nameof(page), page, null);
            }

            var opening = !IsMenuOpen;
            _menuModalLease ??=
                _inputMode.AcquireModal("GameMainMenu");
            CurrentPage = page;
            _menuNavigationMotion.SetVisible(true);
            if (opening)
            {
                SetPageVisibilityImmediate(page);
            }
            else
            {
                SetPageVisibility(page);
            }

            switch (page)
            {
                case GameMenuPage.Archive:
                    _root.Placeholder.Show(
                        "档案",
                        "城市资料库",
                        "用于展示敌人图鉴、收集物、对话回顾和战斗教学。");
                    break;
            }

            UpdateNavigationSelection(page);
        }

        /// <inheritdoc />
        public void HideMenu()
        {
            if (_disposed || !IsMenuOpen)
            {
                return;
            }

            _root.Inventory.SetVisible(false);
            _root.Equipment.SetVisible(false);
            _root.Quests.SetVisible(false);
            _root.Characters.SetVisible(false);
            _root.Placeholder.Hide();
            _menuNavigationMotion.SetVisible(false);
            _menuModalLease?.Dispose();
            _menuModalLease = null;
        }

        /// <inheritdoc />
        public void ShowInventory()
        {
            ShowPage(GameMenuPage.Inventory);
        }

        /// <inheritdoc />
        public void HideInventory()
        {
            if (_disposed || !IsInventoryOpen)
            {
                return;
            }

            HideMenu();
        }

        /// <inheritdoc />
        public void ToggleInventory()
        {
            if (IsInventoryOpen)
            {
                HideInventory();
            }
            else
            {
                ShowInventory();
            }
        }

        /// <inheritdoc />
        public void ShowEquipment()
        {
            ShowPage(GameMenuPage.Equipment);
        }

        /// <inheritdoc />
        public void ToggleEquipment()
        {
            if (IsEquipmentOpen)
            {
                HideMenu();
            }
            else
            {
                ShowEquipment();
            }
        }

        /// <summary>
        /// 关闭页面、释放 Presenter 和 UI 根节点资源租约。
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _root.InventoryButton.onClick.RemoveListener(ShowInventory);
            _root.EquipmentButton.onClick.RemoveListener(ShowEquipment);
            _root.InventoryTabButton.onClick.RemoveListener(ShowInventory);
            _root.EquipmentTabButton.onClick.RemoveListener(ShowEquipment);
            _root.QuestTabButton?.onClick.RemoveAllListeners();
            _root.CharactersTabButton?.onClick.RemoveAllListeners();
            _root.ArchiveTabButton?.onClick.RemoveListener(ShowArchive);
            _root.MenuCloseButton.onClick.RemoveListener(HideMenu);
            SetPageMotionImmediate(_root.Inventory, false);
            SetPageMotionImmediate(_root.Equipment, false);
            _root.Quests.SetVisible(false);
            _root.Characters.SetVisible(false);
            _root.Dialogue.SetVisible(false);
            _root.Placeholder.Hide();
            _menuNavigationMotion.SetVisibleImmediate(false);
            _menuModalLease?.Dispose();
            _menuModalLease = null;
            _inventoryPresenter.Dispose();
            _equipmentPresenter.Dispose();
            _questPresenter.Dispose();
            _characterPresenter.Dispose();
            _dialoguePresenter.Dispose();
            _hudPresenter.Dispose();
            _rootLease.Dispose();
            _disposed = true;
        }

        /// <summary>打开档案页面骨架。</summary>
        private void ShowArchive()
        {
            ShowPage(GameMenuPage.Archive);
        }

        /// <summary>更新主导航按钮的选中可用状态。</summary>
        private void UpdateNavigationSelection(GameMenuPage page)
        {
            _root.InventoryTabButton.interactable =
                page != GameMenuPage.Inventory;
            _root.EquipmentTabButton.interactable =
                page != GameMenuPage.Equipment;
            if (_root.QuestTabButton != null)
            {
                _root.QuestTabButton.interactable =
                    page != GameMenuPage.Quests;
            }

            if (_root.CharactersTabButton != null)
            {
                _root.CharactersTabButton.interactable =
                    page != GameMenuPage.Characters;
            }

            if (_root.ArchiveTabButton != null)
            {
                _root.ArchiveTabButton.interactable =
                    page != GameMenuPage.Archive;
            }
        }

        private UIScreenMotion EnsureMenuNavigationMotion()
        {
            var target = _root.MenuNavigationRoot;
            var motion = target.GetComponent<UIScreenMotion>() ??
                          target.AddComponent<UIScreenMotion>();
            motion.Bind(target);
            return motion;
        }

        private void SetPageVisibility(GameMenuPage page)
        {
            _root.Inventory.SetVisible(page == GameMenuPage.Inventory);
            _root.Equipment.SetVisible(page == GameMenuPage.Equipment);
            _root.Quests.SetVisible(page == GameMenuPage.Quests);
            _root.Characters.SetVisible(page == GameMenuPage.Characters);
            _root.Placeholder.Hide();
        }

        private void SetPageVisibilityImmediate(GameMenuPage page)
        {
            SetPageMotionImmediate(
                _root.Inventory,
                page == GameMenuPage.Inventory);
            SetPageMotionImmediate(
                _root.Equipment,
                page == GameMenuPage.Equipment);
            _root.Quests.SetVisible(page == GameMenuPage.Quests);
            _root.Characters.SetVisible(page == GameMenuPage.Characters);
            _root.Placeholder.Hide();
        }

        private static void SetPageMotionImmediate(
            MonoBehaviour page,
            bool visible)
        {
            var target = page.gameObject;
            var motion = target.GetComponent<UIScreenMotion>() ??
                         target.AddComponent<UIScreenMotion>();
            motion.Bind(target);
            motion.SetVisibleImmediate(visible);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(UIService));
            }
        }
    }
}
