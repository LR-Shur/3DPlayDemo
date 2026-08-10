using UnityEngine;
using UnityEngine.UI;

namespace Train.Presentation.UI.Views
{
    /// <summary>
    /// 汇集常驻 UI 根节点中的画布、HUD、主菜单页面和全部导航按钮引用。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameUIRootView : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private HudView _hud;
        [SerializeField] private InventoryScreenView _inventory;
        [SerializeField] private EquipmentScreenView _equipment;
        [SerializeField] private QuestScreenView _quests;
        [SerializeField] private CharacterScreenView _characters;
        [SerializeField] private DialogueScreenView _dialogue;
        [SerializeField] private MenuPlaceholderScreenView _placeholder;
        [SerializeField] private Button _inventoryButton;
        [SerializeField] private Button _equipmentButton;
        [SerializeField] private GameObject _menuNavigationRoot;
        [SerializeField] private Button _inventoryTabButton;
        [SerializeField] private Button _equipmentTabButton;
        [SerializeField] private Button _questTabButton;
        [SerializeField] private Button _charactersTabButton;
        [SerializeField] private Button _archiveTabButton;
        [SerializeField] private Button _menuCloseButton;

        /// <summary>获取常驻 UI 使用的根画布。</summary>
        public Canvas Canvas => _canvas;

        /// <summary>获取战斗 HUD 视图。</summary>
        public HudView Hud => _hud;

        /// <summary>获取背包页面视图。</summary>
        public InventoryScreenView Inventory => _inventory;

        /// <summary>获取装备与饰品页面视图。</summary>
        public EquipmentScreenView Equipment => _equipment;

        /// <summary>获取任务委托页面视图。</summary>
        public QuestScreenView Quests => _quests;

        /// <summary>获取角色名册页面视图。</summary>
        public CharacterScreenView Characters => _characters;

        /// <summary>获取对话浮层视图。</summary>
        public DialogueScreenView Dialogue => _dialogue;

        /// <summary>获取未完成功能使用的通用页面骨架。</summary>
        public MenuPlaceholderScreenView Placeholder => _placeholder;

        /// <summary>获取 HUD 上的背包入口按钮。</summary>
        public Button InventoryButton => _inventoryButton;

        /// <summary>获取 HUD 上的装备入口按钮。</summary>
        public Button EquipmentButton => _equipmentButton;

        /// <summary>获取主菜单导航根节点。</summary>
        public GameObject MenuNavigationRoot => _menuNavigationRoot;

        /// <summary>获取背包导航按钮。</summary>
        public Button InventoryTabButton => _inventoryTabButton;

        /// <summary>获取装备导航按钮。</summary>
        public Button EquipmentTabButton => _equipmentTabButton;

        /// <summary>获取任务导航按钮。</summary>
        public Button QuestTabButton => _questTabButton;

        /// <summary>获取角色导航按钮。</summary>
        public Button CharactersTabButton => _charactersTabButton;

        /// <summary>获取档案导航按钮。</summary>
        public Button ArchiveTabButton => _archiveTabButton;

        /// <summary>获取主菜单关闭按钮。</summary>
        public Button MenuCloseButton => _menuCloseButton;

        /// <summary>
        /// 配置 UI 根节点所管理的视图与入口引用。
        /// </summary>
        public void Configure(
            Canvas canvas,
            HudView hud,
            InventoryScreenView inventory,
            EquipmentScreenView equipment,
            QuestScreenView quests,
            CharacterScreenView characters,
            DialogueScreenView dialogue,
            MenuPlaceholderScreenView placeholder,
            Button inventoryButton,
            Button equipmentButton,
            GameObject menuNavigationRoot,
            Button inventoryTabButton,
            Button equipmentTabButton,
            Button questTabButton,
            Button charactersTabButton,
            Button archiveTabButton,
            Button menuCloseButton)
        {
            _canvas = canvas;
            _hud = hud;
            _inventory = inventory;
            _equipment = equipment;
            _quests = quests;
            _characters = characters;
            _dialogue = dialogue;
            _placeholder = placeholder;
            _inventoryButton = inventoryButton;
            _equipmentButton = equipmentButton;
            _menuNavigationRoot = menuNavigationRoot;
            _inventoryTabButton = inventoryTabButton;
            _equipmentTabButton = equipmentTabButton;
            _questTabButton = questTabButton;
            _charactersTabButton = charactersTabButton;
            _archiveTabButton = archiveTabButton;
            _menuCloseButton = menuCloseButton;
        }

        /// <summary>获取运行 UI 服务所需的引用是否均已配置。</summary>
        public bool IsValid =>
            _canvas != null &&
            _hud != null &&
            _inventory != null &&
            _equipment != null &&
            _quests != null &&
            _characters != null &&
            _dialogue != null &&
            _placeholder != null &&
            _inventoryButton != null &&
            _equipmentButton != null &&
            _menuNavigationRoot != null &&
            _inventoryTabButton != null &&
            _equipmentTabButton != null &&
            _menuCloseButton != null;
    }
}
