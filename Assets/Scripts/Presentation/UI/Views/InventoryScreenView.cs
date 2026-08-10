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
        [SerializeField] private TMP_Text[] _slotNames = Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text[] _slotQuantities =
            Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text _detailCategory;
        [SerializeField] private TMP_Text _detailName;
        [SerializeField] private TMP_Text _detailRarity;
        [SerializeField] private TMP_Text _detailDescription;
        [SerializeField] private TMP_Text _detailQuantity;

        /// <inheritdoc />
        public event Action<int> SlotSelected;

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
            TMP_Text[] slotNames,
            TMP_Text[] slotQuantities,
            TMP_Text detailCategory,
            TMP_Text detailName,
            TMP_Text detailRarity,
            TMP_Text detailDescription,
            TMP_Text detailQuantity)
        {
            _screenRoot = screenRoot;
            _closeButton = closeButton;
            _capacityText = capacityText;
            _slotButtons = slotButtons;
            _slotBackgrounds = slotBackgrounds;
            _slotAccents = slotAccents;
            _slotNames = slotNames;
            _slotQuantities = slotQuantities;
            _detailCategory = detailCategory;
            _detailName = detailName;
            _detailRarity = detailRarity;
            _detailDescription = detailDescription;
            _detailQuantity = detailQuantity;
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

            var count = Mathf.Min(
                viewModel.Slots.Count,
                _slotButtons.Length);
            for (var index = 0; index < count; index++)
            {
                var slot = viewModel.Slots[index];
                _slotNames[index].text = slot.IsOccupied
                    ? slot.DisplayName
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
                _slotBackgrounds[index].color = slot.IsSelected
                    ? new Color32(28, 66, 83, 248)
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
        }

        private void OnDestroy()
        {
            _closeButton?.onClick.RemoveListener(HandleClose);
        }

        private void HandleClose()
        {
            CloseRequested?.Invoke();
        }

        private void RenderDetail(InventoryItemDetailViewModel detail)
        {
            if (detail == null || !detail.HasItem)
            {
                _detailCategory.text = "ITEM DATA";
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
            _detailQuantity.text =
                $"持有  {detail.Quantity}    堆叠上限  {detail.MaxStack}";
        }

        private static string FormatCategory(ItemCategory category)
        {
            return category switch
            {
                ItemCategory.Consumable => "消耗品 // CONSUMABLE",
                ItemCategory.Currency => "货币 // CURRENCY",
                ItemCategory.Quest => "任务物品 // QUEST",
                ItemCategory.Equipment => "装备 // EQUIPMENT",
                _ => "养成素材 // MATERIAL"
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
