using System;
using System.Collections.Generic;
using TMPro;
using Train.Inventory.Data;
using Train.Presentation.UI.Contracts;
using Train.Presentation.UI.ViewModels;
using UnityEngine;
using UnityEngine.UI;

namespace Train.Presentation.UI.Views
{
    /// <summary>
    /// 渲染固定槽位背包页面，并将槽位选择和关闭操作上报给 Presenter。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InventoryScreenView :
        MonoBehaviour,
        IInventoryView
    {
        [SerializeField] private GameObject _screenRoot;
        [SerializeField] private Button _closeButton;
        [SerializeField] private TMP_Text _capacityText;
        [SerializeField] private RectTransform _slotContent;
        [SerializeField] private Button _slotTemplate;
        [SerializeField] private Button[] _categoryButtons = Array.Empty<Button>();
        [SerializeField] private TMP_Text[] _categoryLabels = Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text _detailCategory;
        [SerializeField] private TMP_Text _detailName;
        [SerializeField] private TMP_Text _detailRarity;
        [SerializeField] private TMP_Text _detailDescription;
        [SerializeField] private TMP_Text _detailQuantity;
        [SerializeField] private Image _detailIcon;
        [SerializeField] private Button _discardButton;
        private TMP_Text _discardLabel;
        private UIScreenMotion _screenMotion;
        private UIInteractionMotion _detailMotion;
        private string _lastDetailKey;
        private bool _hasDetailKey;
        private readonly List<SlotControls> _slots = new();

        /// <inheritdoc />
        public event Action<int> SlotSelected;

        /// <summary>当玩家切换左侧物品分类时触发，0 表示全部。</summary>
        public event Action<int> CategorySelected;

        /// <summary>丢弃当前选中物品的请求。</summary>
        public event Action DiscardRequested;

        /// <summary>背包详情区域的主动道具槽按钮事件。</summary>

        /// <inheritdoc />
        public event Action CloseRequested;

        /// <summary>获取背包页面根节点当前是否处于显示状态。</summary>
        public bool IsVisible =>
            _screenRoot != null && _screenRoot.activeSelf;

        /// <summary>
        /// 配置背包页面的根节点、槽位控件和详情区域引用。
        /// </summary>
        public void Configure(
            GameObject screenRoot,
            Button closeButton,
            TMP_Text capacityText,
            RectTransform slotContent,
            Button slotTemplate,
            Button[] categoryButtons,
            TMP_Text[] categoryLabels,
            TMP_Text detailCategory,
            TMP_Text detailName,
            TMP_Text detailRarity,
            TMP_Text detailDescription,
            TMP_Text detailQuantity,
            Image detailIcon,
            Button discardButton)
        {
            _screenRoot = screenRoot;
            _closeButton = closeButton;
            _capacityText = capacityText;
            _slotContent = slotContent;
            _slotTemplate = slotTemplate;
            _categoryButtons = categoryButtons;
            _categoryLabels = categoryLabels;
            _detailCategory = detailCategory;
            _detailName = detailName;
            _detailRarity = detailRarity;
            _detailDescription = detailDescription;
            _detailQuantity = detailQuantity;
            _detailIcon = detailIcon;
            _discardButton = discardButton;

            ValidateSlotLayout();

            ConfigureDiscardButton();
            AttachDiscardListener();
        }

        /// <summary>设置背包页面根节点的显示状态。</summary>
        public void SetVisible(bool visible)
        {
            EnsureScreenMotion().SetVisible(visible);
        }

        /// <inheritdoc />
        public void Render(InventoryScreenViewModel viewModel)
        {
            if (viewModel == null)
            {
                return;
            }

            _capacityText.text =
                $"容量  {viewModel.OccupiedCount:00} / {viewModel.Capacity:00}";

            for (var index = 0; index < _categoryButtons.Length; index++)
            {
                var selected = index == viewModel.SelectedCategoryIndex;
                if (_categoryButtons[index] != null &&
                    _categoryButtons[index].targetGraphic != null)
                {
                    _categoryButtons[index].targetGraphic.color = selected
                        ? new Color32(29, 83, 96, 255)
                        : new Color32(31, 49, 70, 255);
                }

                if (index < _categoryLabels.Length &&
                    _categoryLabels[index] != null)
                {
                    _categoryLabels[index].color = selected
                        ? new Color32(88, 224, 231, 255)
                        : new Color32(170, 184, 200, 255);
                }
            }

            EnsureSlotCount(viewModel.Slots.Count);
            for (var index = 0; index < viewModel.Slots.Count; index++)
            {
                var slot = viewModel.Slots[index];
                var controls = _slots[index];
                var displayName = slot.IsEquipped
                    ? $"已装备 · {slot.DisplayName}"
                    : slot.DisplayName;
                controls.Name.text = slot.IsOccupied
                    ? displayName
                    : "空槽位";
                controls.Name.color = slot.IsOccupied
                    ? new Color32(244, 247, 251, 255)
                    : new Color32(112, 132, 154, 180);
                controls.Quantity.text = slot.IsOccupied
                    ? $"×{slot.Quantity}"
                    : $"{index + 1:00}";
                controls.Accent.color = slot.IsOccupied
                    ? RarityColor(slot.Rarity)
                    : new Color32(50, 70, 92, 150);
                if (controls.Icon != null)
                {
                    controls.Icon.sprite = slot.IsOccupied
                        ? LoadItemIcon(slot.ItemId)
                        : null;
                    controls.Icon.gameObject.SetActive(
                        controls.Icon.sprite != null);
                }
                controls.Background.color = slot.IsSelected
                    ? new Color32(28, 66, 83, 248)
                    : slot.IsEquipped
                        ? new Color32(66, 54, 31, 245)
                    : new Color32(23, 40, 61, 238);
            }

            RenderDetail(viewModel.Detail);
        }

        private void Awake()
        {
            EnsureScreenMotion();
            ValidateSlotLayout();
            _closeButton?.onClick.AddListener(HandleClose);
            BindButtonFeedback(_closeButton);

            for (var index = 0; index < _categoryButtons.Length; index++)
            {
                var captured = index;
                _categoryButtons[index]?.onClick.AddListener(
                    () => CategorySelected?.Invoke(captured));
                BindButtonFeedback(_categoryButtons[index]);
            }

            ConfigureDiscardButton();
            AttachDiscardListener();
            BindButtonFeedback(_discardButton);
            EnsureDetailMotion();
        }

        private void ValidateSlotLayout()
        {
            if (_slotContent == null || _slotTemplate == null)
            {
                throw new InvalidOperationException(
                    "InventoryScreenView requires ItemGrid/ScrollContent and SlotTemplate.");
            }

            var viewport = _slotContent.parent as RectTransform;
            if (viewport == null)
            {
                throw new InvalidOperationException(
                    "InventoryScreenView requires ScrollContent to be a child of ItemGrid.");
            }

            var grid = _slotContent.GetComponent<GridLayoutGroup>();
            if (grid == null)
            {
                throw new InvalidOperationException(
                    "InventoryScreenView requires GridLayoutGroup on ScrollContent.");
            }

            var fitter = _slotContent.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                throw new InvalidOperationException(
                    "InventoryScreenView requires ContentSizeFitter on ScrollContent.");
            }

            var scroll = viewport.GetComponent<ScrollRect>();
            if (scroll == null)
            {
                throw new InvalidOperationException(
                    "InventoryScreenView requires ScrollRect on ItemGrid.");
            }

            if (viewport.GetComponent<RectMask2D>() == null)
            {
                throw new InvalidOperationException(
                    "InventoryScreenView requires RectMask2D on ItemGrid.");
            }

            scroll.content = _slotContent;
            scroll.viewport = viewport;

            _slotTemplate.gameObject.SetActive(false);
        }

        private void EnsureSlotCount(int count)
        {
            ValidateSlotLayout();
            while (_slots.Count < count)
            {
                var index = _slots.Count;
                var instance = Instantiate(_slotTemplate.gameObject, _slotContent);
                instance.name = $"Slot_{index:00}";
                instance.SetActive(true);
                _slots.Add(CreateSlotControls(instance, index));
            }

            while (_slots.Count > count)
            {
                var last = _slots.Count - 1;
                Destroy(_slots[last].Button.gameObject);
                _slots.RemoveAt(last);
            }
        }

        private SlotControls CreateSlotControls(GameObject instance, int index)
        {
            var controls = new SlotControls(
                instance.GetComponent<Button>(),
                instance.GetComponent<Image>(),
                instance.transform.Find("Rarity")?.GetComponent<Image>(),
                instance.transform.Find("Icon")?.GetComponent<Image>(),
                instance.transform.Find("Name")?.GetComponent<TMP_Text>(),
                instance.transform.Find("Quantity")?.GetComponent<TMP_Text>());
            if (controls.Button == null ||
                controls.Background == null ||
                controls.Accent == null ||
                controls.Name == null ||
                controls.Quantity == null)
            {
                throw new InvalidOperationException(
                    "SlotTemplate must contain Button, Image, Rarity, Name, and Quantity components.");
            }

            controls.Button.onClick.AddListener(() => SlotSelected?.Invoke(index));
            BindButtonFeedback(controls.Button);
            return controls;
        }

        private sealed class SlotControls
        {
            public SlotControls(
                Button button,
                Image background,
                Image accent,
                Image icon,
                TMP_Text name,
                TMP_Text quantity)
            {
                Button = button;
                Background = background;
                Accent = accent;
                Icon = icon;
                Name = name;
                Quantity = quantity;
            }

            public Button Button { get; }
            public Image Background { get; }
            public Image Accent { get; }
            public Image Icon { get; }
            public TMP_Text Name { get; }
            public TMP_Text Quantity { get; }
        }

        private void OnDestroy()
        {
            _closeButton?.onClick.RemoveListener(HandleClose);
            _discardButton?.onClick.RemoveListener(HandleDiscard);
        }

        private void HandleClose()
        {
            CloseRequested?.Invoke();
        }

        private void HandleDiscard()
        {
            DiscardRequested?.Invoke();
        }

        /// <summary>纭繚涓㈠純鎸夐挳鍙湁涓€涓湁鏁堢殑鐐瑰嚮鐩戝惉銆?/summary>
        private void AttachDiscardListener()
        {
            _discardButton.onClick.RemoveListener(HandleDiscard);
            _discardButton.onClick.AddListener(HandleDiscard);
        }

        private void ConfigureDiscardButton()
        {
            if (_discardButton == null || _detailName == null)
            {
                throw new InvalidOperationException(
                    "InventoryScreenView requires Detail/DiscardButton in the prefab.");
            }

            _discardLabel = _discardButton.GetComponentInChildren<TMP_Text>(true);
            if (_discardLabel == null)
            {
                throw new InvalidOperationException(
                    "InventoryScreenView requires a label under Detail/DiscardButton.");
            }

            _discardLabel.font = _detailName.font;
            _discardLabel.fontSize = 18f;
            _discardLabel.alignment = TextAlignmentOptions.Center;
            _discardLabel.color = Color.white;
            _discardLabel.raycastTarget = false;
        }

        private void RenderDetail(InventoryItemDetailViewModel detail)
        {
            var detailKey = DetailKey(detail);
            var detailChanged = !_hasDetailKey ||
                                !string.Equals(
                                    _lastDetailKey,
                                    detailKey,
                                    StringComparison.Ordinal);
            _lastDetailKey = detailKey;
            _hasDetailKey = true;

            if (detail == null || !detail.HasItem)
            {
                SetDiscardState(false, false, false);
            }

            if (detail == null || !detail.HasItem)
            {
                if (_detailIcon != null)
                {
                    _detailIcon.sprite = null;
                    _detailIcon.gameObject.SetActive(false);
                }
                _detailCategory.text = "物品详情";
                _detailName.text = "未选择物品";
                _detailRarity.text = "--";
                _detailDescription.text =
                    "选择左侧槽位以查看物品信息。";
                _detailQuantity.text = "持有  --";
                if (detailChanged)
                {
                    _detailMotion?.PlayRefresh();
                }
                return;
            }

            _detailCategory.text = FormatCategory(detail.Category);
            _detailName.text = detail.DisplayName;
            _detailRarity.text = FormatRarity(detail.Rarity);
            _detailRarity.color = RarityColor(detail.Rarity);
            _detailDescription.text = detail.Description;
            SetDiscardState(true, detail.CanDiscard, detail.IsEquipped);
            _detailQuantity.text =
                $"持有  {detail.Quantity}    堆叠上限  {detail.MaxStack}";
            if (_detailIcon != null)
            {
                _detailIcon.sprite = LoadItemIcon(detail.ItemId);
                _detailIcon.gameObject.SetActive(_detailIcon.sprite != null);
            }

            if (detailChanged)
            {
                _detailMotion?.PlayRefresh();
            }
        }

        private UIScreenMotion EnsureScreenMotion()
        {
            var target = _screenRoot ?? gameObject;
            if (_screenMotion == null)
            {
                _screenMotion = target.GetComponent<UIScreenMotion>() ??
                                target.AddComponent<UIScreenMotion>();
            }

            _screenMotion.Bind(target);
            return _screenMotion;
        }

        private void EnsureDetailMotion()
        {
            if (_detailMotion != null || _detailName == null)
            {
                return;
            }

            var detailRoot = _detailName.transform.parent;
            if (detailRoot == null)
            {
                return;
            }

            _detailMotion = detailRoot.GetComponent<UIInteractionMotion>() ??
                            detailRoot.gameObject.AddComponent<UIInteractionMotion>();
        }

        private static void BindButtonFeedback(Button button)
        {
            if (button != null &&
                button.GetComponent<UIInteractionMotion>() == null)
            {
                button.gameObject.AddComponent<UIInteractionMotion>();
            }
        }

        private static string DetailKey(InventoryItemDetailViewModel detail)
        {
            return detail == null || !detail.HasItem
                ? "empty"
                : $"{detail.SlotIndex}:{detail.ItemId}";
        }


        /// <summary>按稳定物品 ID 从 Resources 读取本地化图标。</summary>
        private void SetDiscardState(bool visible, bool interactable, bool equipped)
        {
            _discardButton.gameObject.SetActive(visible);
            _discardButton.interactable = interactable;
            _discardButton.image.color = interactable
                ? new Color32(132, 43, 58, 255)
                : new Color32(74, 74, 74, 220);
            if (_discardLabel != null)
            {
                _discardLabel.text = equipped ? "已装备" : "丢弃物品";
            }
        }

        private static Sprite LoadItemIcon(string itemId)
        {
            return string.IsNullOrWhiteSpace(itemId)
                ? null
                : Resources.Load<Sprite>($"UI/Icons/{itemId}");
        }

        private static string FormatCategory(ItemCategory category)
        {
            return category switch
            {
                ItemCategory.Consumable => "消耗品",
                ItemCategory.Currency => "货币",
                ItemCategory.Quest => "任务物品",
                ItemCategory.Equipment => "装备",
                _ => "养成素材"
            };
        }

        private static string FormatRarity(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Legendary => "传奇",
                ItemRarity.Epic => "史诗",
                ItemRarity.Elite => "精英",
                ItemRarity.Rare => "稀有",
                _ => "普通"
            };
        }

        private static Color RarityColor(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Legendary =>
                    new Color32(255, 177, 76, 255),
                ItemRarity.Epic =>
                    new Color32(190, 111, 255, 255),
                ItemRarity.Elite =>
                    new Color32(107, 140, 255, 255),
                ItemRarity.Rare =>
                    new Color32(88, 224, 231, 255),
                _ => new Color32(170, 184, 200, 255)
            };
        }
    }
}
