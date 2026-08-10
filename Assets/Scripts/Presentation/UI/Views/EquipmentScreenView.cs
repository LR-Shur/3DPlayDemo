using System;
using TMPro;
using Train.Equipment.Core;
using Train.Equipment.Data;
using Train.Presentation.UI.Contracts;
using Train.Presentation.UI.ViewModels;
using UnityEngine;
using UnityEngine.UI;

namespace Train.Presentation.UI.Views
{
    /// <summary>
    /// 商业化装备整备页面。
    /// 它显示十个槽位、背包装备目录、套装状态和最终属性，但不直接访问任何服务。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EquipmentScreenView :
        MonoBehaviour,
        IEquipmentView
    {
        [SerializeField] private GameObject _screenRoot;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _equipButton;
        [SerializeField] private Button _unequipButton;
        [SerializeField] private Button[] _slotButtons = Array.Empty<Button>();
        [SerializeField] private Image[] _slotBackgrounds = Array.Empty<Image>();
        [SerializeField] private Image[] _slotIcons = Array.Empty<Image>();
        [SerializeField] private TMP_Text[] _slotLabels = Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text[] _slotItemNames = Array.Empty<TMP_Text>();
        [SerializeField] private Button[] _itemButtons = Array.Empty<Button>();
        [SerializeField] private Image[] _itemBackgrounds = Array.Empty<Image>();
        [SerializeField] private Image[] _itemIcons = Array.Empty<Image>();
        [SerializeField] private TMP_Text[] _itemNames = Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text[] _itemStates = Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text _revisionText;
        [SerializeField] private Image _detailIcon;
        [SerializeField] private TMP_Text _detailRarity;
        [SerializeField] private TMP_Text _detailName;
        [SerializeField] private TMP_Text _detailDescription;
        [SerializeField] private TMP_Text _detailModifiers;
        [SerializeField] private TMP_Text _activeSets;
        [SerializeField] private TMP_Text[] _statNames = Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text[] _statBaseValues = Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text[] _statFinalValues = Array.Empty<TMP_Text>();

        /// <inheritdoc />
        public event Action<int> ItemSelected;

        /// <inheritdoc />
        public event Action<EquipmentSlot> SlotSelected;

        /// <inheritdoc />
        public event Action EquipRequested;

        /// <inheritdoc />
        public event Action UnequipRequested;

        /// <inheritdoc />
        public event Action CloseRequested;

        /// <summary>页面当前是否可见。</summary>
        public bool IsVisible =>
            _screenRoot != null && _screenRoot.activeSelf;

        /// <summary>
        /// 配置由编辑器内容构建器生成的全部控件引用。
        /// </summary>
        public void Configure(
            GameObject screenRoot,
            Button closeButton,
            Button equipButton,
            Button unequipButton,
            Button[] slotButtons,
            Image[] slotBackgrounds,
            Image[] slotIcons,
            TMP_Text[] slotLabels,
            TMP_Text[] slotItemNames,
            Button[] itemButtons,
            Image[] itemBackgrounds,
            Image[] itemIcons,
            TMP_Text[] itemNames,
            TMP_Text[] itemStates,
            TMP_Text revisionText,
            Image detailIcon,
            TMP_Text detailRarity,
            TMP_Text detailName,
            TMP_Text detailDescription,
            TMP_Text detailModifiers,
            TMP_Text activeSets,
            TMP_Text[] statNames,
            TMP_Text[] statBaseValues,
            TMP_Text[] statFinalValues)
        {
            _screenRoot = screenRoot;
            _closeButton = closeButton;
            _equipButton = equipButton;
            _unequipButton = unequipButton;
            _slotButtons = slotButtons;
            _slotBackgrounds = slotBackgrounds;
            _slotIcons = slotIcons;
            _slotLabels = slotLabels;
            _slotItemNames = slotItemNames;
            _itemButtons = itemButtons;
            _itemBackgrounds = itemBackgrounds;
            _itemIcons = itemIcons;
            _itemNames = itemNames;
            _itemStates = itemStates;
            _revisionText = revisionText;
            _detailIcon = detailIcon;
            _detailRarity = detailRarity;
            _detailName = detailName;
            _detailDescription = detailDescription;
            _detailModifiers = detailModifiers;
            _activeSets = activeSets;
            _statNames = statNames;
            _statBaseValues = statBaseValues;
            _statFinalValues = statFinalValues;
        }

        /// <summary>切换装备页面可见性。</summary>
        public void SetVisible(bool visible)
        {
            if (_screenRoot != null)
            {
                _screenRoot.SetActive(visible);
            }
        }

        /// <inheritdoc />
        public void Render(EquipmentScreenViewModel viewModel)
        {
            if (viewModel == null)
            {
                return;
            }

            _revisionText.text =
                $"LOADOUT REV. {viewModel.Revision:0000}";
            RenderSlots(viewModel);
            RenderItems(viewModel);
            RenderDetail(viewModel);
            RenderStats(viewModel);

            _equipButton.interactable = viewModel.CanEquip;
            _unequipButton.interactable = viewModel.CanUnequip;
        }

        /// <summary>
        /// 绑定十个槽位、候选卡片和操作按钮。
        /// </summary>
        private void Awake()
        {
            _closeButton?.onClick.AddListener(
                () => CloseRequested?.Invoke());
            _equipButton?.onClick.AddListener(
                () => EquipRequested?.Invoke());
            _unequipButton?.onClick.AddListener(
                () => UnequipRequested?.Invoke());

            for (var index = 0; index < _slotButtons.Length; index++)
            {
                var captured = index;
                _slotButtons[index]?.onClick.AddListener(
                    () => SlotSelected?.Invoke((EquipmentSlot)captured));
            }

            for (var index = 0; index < _itemButtons.Length; index++)
            {
                var captured = index;
                _itemButtons[index]?.onClick.AddListener(
                    () => ItemSelected?.Invoke(captured));
            }
        }

        /// <summary>渲染十个穿戴槽位。</summary>
        private void RenderSlots(EquipmentScreenViewModel viewModel)
        {
            var count = Mathf.Min(viewModel.Slots.Count, _slotButtons.Length);
            for (var index = 0; index < _slotButtons.Length; index++)
            {
                var visible = index < count;
                _slotButtons[index].gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                var slot = viewModel.Slots[index];
                _slotLabels[index].text = slot.SlotName;
                _slotItemNames[index].text = slot.ItemName;
                _slotItemNames[index].color = slot.IsOccupied
                    ? new Color32(244, 247, 251, 255)
                    : new Color32(112, 132, 154, 255);
                _slotIcons[index].sprite = slot.Icon;
                _slotIcons[index].enabled = slot.Icon != null;
                _slotBackgrounds[index].color = slot.IsSelected
                    ? new Color32(28, 76, 92, 252)
                    : new Color32(23, 40, 61, 245);
            }
        }

        /// <summary>渲染背包拥有的候选装备卡片。</summary>
        private void RenderItems(EquipmentScreenViewModel viewModel)
        {
            for (var index = 0; index < _itemButtons.Length; index++)
            {
                var visible = index < viewModel.Items.Count;
                _itemButtons[index].gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                var item = viewModel.Items[index];
                _itemNames[index].text = item.DisplayName;
                _itemStates[index].text = item.IsEquipped
                    ? "EQUIPPED"
                    : $"持有 ×{item.OwnedQuantity}";
                _itemStates[index].color = item.IsEquipped
                    ? new Color32(242, 201, 107, 255)
                    : new Color32(170, 184, 200, 255);
                _itemIcons[index].sprite = item.Icon;
                _itemIcons[index].enabled = item.Icon != null;
                _itemBackgrounds[index].color = item.IsSelected
                    ? new Color32(29, 82, 96, 252)
                    : new Color32(20, 35, 54, 245);
                var colors = _itemButtons[index].colors;
                colors.selectedColor = RarityColor(item.Rarity);
                _itemButtons[index].colors = colors;
            }
        }

        /// <summary>渲染当前候选装备详情与套装状态。</summary>
        private void RenderDetail(EquipmentScreenViewModel viewModel)
        {
            _detailIcon.sprite = viewModel.SelectedIcon;
            _detailIcon.enabled = viewModel.SelectedIcon != null;
            _detailRarity.text = FormatRarity(viewModel.SelectedRarity);
            _detailRarity.color = RarityColor(viewModel.SelectedRarity);
            _detailName.text = viewModel.SelectedName;
            _detailDescription.text = viewModel.SelectedDescription;
            _detailModifiers.text = viewModel.ModifiersText;
            _activeSets.text = viewModel.ActiveSetsText;
        }

        /// <summary>渲染基础属性与最终属性对照。</summary>
        private void RenderStats(EquipmentScreenViewModel viewModel)
        {
            var count = Mathf.Min(viewModel.Stats.Count, _statNames.Length);
            for (var index = 0; index < count; index++)
            {
                var stat = viewModel.Stats[index];
                _statNames[index].text = stat.Name;
                _statBaseValues[index].text = stat.BaseValue;
                _statFinalValues[index].text = stat.FinalValue;
                _statFinalValues[index].color = stat.IsIncreased
                    ? new Color32(88, 224, 231, 255)
                    : new Color32(244, 247, 251, 255);
            }
        }

        /// <summary>将装备品质映射为 UI 强调色。</summary>
        private static Color RarityColor(EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Legendary =>
                    new Color32(255, 177, 76, 255),
                EquipmentRarity.Epic =>
                    new Color32(190, 111, 255, 255),
                EquipmentRarity.Elite =>
                    new Color32(107, 140, 255, 255),
                EquipmentRarity.Rare =>
                    new Color32(88, 224, 231, 255),
                _ => new Color32(170, 184, 200, 255)
            };
        }

        /// <summary>将品质转换为中英双语标签。</summary>
        private static string FormatRarity(EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Legendary => "传奇 // S",
                EquipmentRarity.Epic => "史诗 // A",
                EquipmentRarity.Elite => "精英 // B",
                EquipmentRarity.Rare => "稀有 // C",
                _ => "普通 // D"
            };
        }
    }
}
