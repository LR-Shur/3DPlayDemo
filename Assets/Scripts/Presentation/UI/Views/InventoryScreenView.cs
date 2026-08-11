using System;
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
        [SerializeField] private Button[] _slotButtons = Array.Empty<Button>();
        [SerializeField] private Image[] _slotBackgrounds = Array.Empty<Image>();
        [SerializeField] private Image[] _slotAccents = Array.Empty<Image>();
        [SerializeField] private Image[] _slotIcons = Array.Empty<Image>();
        [SerializeField] private TMP_Text[] _slotNames = Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text[] _slotQuantities =
            Array.Empty<TMP_Text>();
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

        /// <inheritdoc />
        public event Action<int> SlotSelected;

        /// <summary>当玩家切换左侧物品分类时触发，0 表示全部。</summary>
        public event Action<int> CategorySelected;

        /// <summary>丢弃当前选中物品的请求。</summary>
        public event Action DiscardRequested;

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
            Button[] slotButtons,
            Image[] slotBackgrounds,
            Image[] slotAccents,
            Image[] slotIcons,
            TMP_Text[] slotNames,
            TMP_Text[] slotQuantities,
            Button[] categoryButtons,
            TMP_Text[] categoryLabels,
            TMP_Text detailCategory,
            TMP_Text detailName,
            TMP_Text detailRarity,
            TMP_Text detailDescription,
            TMP_Text detailQuantity,
            Image detailIcon)
        {
            _screenRoot = screenRoot;
            _closeButton = closeButton;
            _capacityText = capacityText;
            _slotButtons = slotButtons;
            _slotBackgrounds = slotBackgrounds;
            _slotAccents = slotAccents;
            _slotIcons = slotIcons;
            _slotNames = slotNames;
            _slotQuantities = slotQuantities;
            _categoryButtons = categoryButtons;
            _categoryLabels = categoryLabels;
            _detailCategory = detailCategory;
            _detailName = detailName;
            _detailRarity = detailRarity;
            _detailDescription = detailDescription;
            _detailQuantity = detailQuantity;
            _detailIcon = detailIcon;

            // 鏃у叾瀹氭湇浠剁殑 UI 鍦ㄦ暟鎹垵濮嬪寲鍚庢墠鑳芥壘鍒拌鎯呭瓧浣撱€傚湪 Configure 鍚庡啀纭繚涓㈠純鎸夐挳鍜屽瓧浣撳瓨鍦ㄣ€?
            EnsureDiscardButton();
            AttachDiscardListener();
        }

        /// <summary>设置背包页面根节点的显示状态。</summary>
        public void SetVisible(bool visible)
        {
            if (_screenRoot != null)
            {
                _screenRoot.SetActive(visible);
            }
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

            var count = Mathf.Min(
                viewModel.Slots.Count,
                _slotButtons.Length);
            for (var index = 0; index < count; index++)
            {
                var slot = viewModel.Slots[index];
                var displayName = slot.IsEquipped
                    ? $"已装备 · {slot.DisplayName}"
                    : slot.DisplayName;
                _slotNames[index].text = slot.IsOccupied
                    ? displayName
                    : "空槽位";
                _slotNames[index].color = slot.IsOccupied
                    ? new Color32(244, 247, 251, 255)
                    : new Color32(112, 132, 154, 180);
                _slotQuantities[index].text = slot.IsOccupied
                    ? $"×{slot.Quantity}"
                    : $"{index + 1:00}";
                _slotAccents[index].color = slot.IsOccupied
                    ? RarityColor(slot.Rarity)
                    : new Color32(50, 70, 92, 150);
                if (index < _slotIcons.Length && _slotIcons[index] != null)
                {
                    _slotIcons[index].sprite = slot.IsOccupied
                        ? LoadItemIcon(slot.ItemId)
                        : null;
                    _slotIcons[index].gameObject.SetActive(
                        _slotIcons[index].sprite != null);
                }
                _slotBackgrounds[index].color = slot.IsSelected
                    ? new Color32(28, 66, 83, 248)
                    : slot.IsEquipped
                        ? new Color32(66, 54, 31, 245)
                    : new Color32(23, 40, 61, 238);
            }

            RenderDetail(viewModel.Detail);
        }

        private void Awake()
        {
            _closeButton?.onClick.AddListener(HandleClose);
            for (var index = 0; index < _slotButtons.Length; index++)
            {
                var captured = index;
                _slotButtons[index]?.onClick.AddListener(
                    () => SlotSelected?.Invoke(captured));
            }

            for (var index = 0; index < _categoryButtons.Length; index++)
            {
                var captured = index;
                _categoryButtons[index]?.onClick.AddListener(
                    () => CategorySelected?.Invoke(captured));
            }

            EnsureDiscardButton();
            AttachDiscardListener();
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
            if (_discardButton == null)
            {
                return;
            }

            _discardButton.onClick.RemoveListener(HandleDiscard);
            _discardButton.onClick.AddListener(HandleDiscard);
        }

        /// <summary>为旧版 UI 预制体补建丢弃按钮。</summary>
        private void EnsureDiscardButton()
        {
            if (_detailName == null)
            {
                return;
            }

            if (_discardButton == null)
            {
                var detail = _detailName.transform.parent as RectTransform;
                if (detail == null)
                {
                    return;
                }

                var buttonObject = new GameObject(
                    "DiscardButton",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button));
                buttonObject.transform.SetParent(detail, false);
                var rect = buttonObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                rect.sizeDelta = new Vector2(150f, 48f);
                rect.anchoredPosition = new Vector2(-30f, 28f);

                var image = buttonObject.GetComponent<Image>();
                image.color = new Color32(132, 43, 58, 255);
                _discardButton = buttonObject.GetComponent<Button>();
                _discardButton.targetGraphic = image;
            }

            _discardLabel = _discardButton.GetComponentInChildren<TMP_Text>(true);
            if (_discardLabel == null)
            {
                var labelObject = new GameObject(
                    "Label",
                    typeof(RectTransform),
                    typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(_discardButton.transform, false);
                var labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                _discardLabel = labelObject.GetComponent<TMP_Text>();
            }

            _discardLabel.font = _detailName.font;
            _discardLabel.fontSize = 18f;
            _discardLabel.alignment = TextAlignmentOptions.Center;
            _discardLabel.color = Color.white;
            _discardLabel.raycastTarget = false;
        }

        private void RenderDetail(InventoryItemDetailViewModel detail)
        {
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
        }

        /// <summary>按稳定物品 ID 从 Resources 读取本地化图标。</summary>
        private void SetDiscardState(bool visible, bool interactable, bool equipped)
        {
            if (_discardButton == null)
            {
                return;
            }

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
