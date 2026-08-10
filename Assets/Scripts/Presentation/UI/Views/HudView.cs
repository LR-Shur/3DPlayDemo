using TMPro;
using Train.GameFlow.Core;
using Train.Inventory.Data;
using Train.Presentation.UI.Contracts;
using Train.Presentation.UI.ViewModels;
using UnityEngine;
using UnityEngine.UI;

namespace Train.Presentation.UI.Views
{
    /// <summary>
    /// 将 HUD 视图模型渲染到生命条、关卡目标和中央提示横幅。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HudView : MonoBehaviour, IHudView
    {
        [SerializeField] private TMP_Text _levelName;
        [SerializeField] private TMP_Text _phase;
        [SerializeField] private TMP_Text _objective;
        [SerializeField] private TMP_Text _healthValue;
        [SerializeField] private Image _healthFill;
        [SerializeField] private CanvasGroup _bannerGroup;
        [SerializeField] private Image _bannerAccent;
        [SerializeField] private TMP_Text _bannerTitle;
        [SerializeField] private TMP_Text _bannerSubtitle;
        [SerializeField] private CanvasGroup _interactionGroup;
        [SerializeField] private Image _interactionAccent;
        [SerializeField] private TMP_Text _interactionName;
        [SerializeField] private TMP_Text _interactionHint;
        [SerializeField] private CanvasGroup _pickupToastGroup;
        [SerializeField] private Image _pickupToastAccent;
        [SerializeField] private TMP_Text _pickupToastTitle;
        [SerializeField] private TMP_Text _pickupToastSubtitle;

        private float _hideBannerAt = -1f;
        private float _hidePickupToastAt = -1f;

        /// <summary>
        /// 配置 HUD 中需要写入的文字、图片和横幅引用。
        /// </summary>
        public void Configure(
            TMP_Text levelName,
            TMP_Text phase,
            TMP_Text objective,
            TMP_Text healthValue,
            Image healthFill,
            CanvasGroup bannerGroup,
            Image bannerAccent,
            TMP_Text bannerTitle,
            TMP_Text bannerSubtitle,
            CanvasGroup interactionGroup,
            Image interactionAccent,
            TMP_Text interactionName,
            TMP_Text interactionHint,
            CanvasGroup pickupToastGroup,
            Image pickupToastAccent,
            TMP_Text pickupToastTitle,
            TMP_Text pickupToastSubtitle)
        {
            _levelName = levelName;
            _phase = phase;
            _objective = objective;
            _healthValue = healthValue;
            _healthFill = healthFill;
            _bannerGroup = bannerGroup;
            _bannerAccent = bannerAccent;
            _bannerTitle = bannerTitle;
            _bannerSubtitle = bannerSubtitle;
            _interactionGroup = interactionGroup;
            _interactionAccent = interactionAccent;
            _interactionName = interactionName;
            _interactionHint = interactionHint;
            _pickupToastGroup = pickupToastGroup;
            _pickupToastAccent = pickupToastAccent;
            _pickupToastTitle = pickupToastTitle;
            _pickupToastSubtitle = pickupToastSubtitle;
        }

        /// <inheritdoc />
        public void Render(HudViewModel viewModel)
        {
            if (viewModel == null)
            {
                return;
            }

            _levelName.text = viewModel.HasLevel
                ? viewModel.LevelName
                : "训练区域";
            _phase.text = FormatPhase(viewModel.Phase);
            _objective.text = viewModel.HasLevel
                ? viewModel.ObjectiveText
                : "等待关卡数据";
            _healthFill.fillAmount = viewModel.Health01;
            _healthValue.text = viewModel.PlayerMaxHealth > 0f
                ? $"{Mathf.CeilToInt(viewModel.PlayerHealth)} / " +
                  $"{Mathf.CeilToInt(viewModel.PlayerMaxHealth)}"
                : "-- / --";

            RenderBanner(viewModel.Banner);
        }

        /// <inheritdoc />
        public void RenderInteractionPrompt(
            InteractionPromptViewModel viewModel)
        {
            var visible = viewModel != null && viewModel.IsVisible;
            _interactionGroup.alpha = visible ? 1f : 0f;
            _interactionGroup.blocksRaycasts = false;
            if (!visible)
            {
                return;
            }

            _interactionName.text = viewModel.DisplayName;
            _interactionHint.text = viewModel.Quantity > 0
                ? $"[ E ]  {viewModel.ActionLabel}    ×{viewModel.Quantity}"
                : $"[ E ]  {viewModel.ActionLabel}";
            _interactionAccent.color = RarityColor(viewModel.Rarity);
        }

        /// <inheritdoc />
        public void RenderPickupToast(PickupToastViewModel viewModel)
        {
            if (viewModel == null)
            {
                return;
            }

            _pickupToastGroup.alpha = 1f;
            _pickupToastGroup.blocksRaycasts = false;
            _pickupToastTitle.text = viewModel.Title;
            _pickupToastSubtitle.text = viewModel.Message;
            _pickupToastAccent.color = viewModel.IsSuccess
                ? RarityColor(viewModel.Rarity)
                : new Color32(255, 99, 121, 255);
            _hidePickupToastAt = Time.unscaledTime + 2.6f;
        }

        private void Update()
        {
            if (_hideBannerAt > 0f &&
                Time.unscaledTime >= _hideBannerAt)
            {
                _hideBannerAt = -1f;
                _bannerGroup.alpha = 0f;
                _bannerGroup.blocksRaycasts = false;
            }

            if (_hidePickupToastAt > 0f &&
                Time.unscaledTime >= _hidePickupToastAt)
            {
                _hidePickupToastAt = -1f;
                _pickupToastGroup.alpha = 0f;
                _pickupToastGroup.blocksRaycasts = false;
            }
        }

        private void RenderBanner(HudBannerViewModel banner)
        {
            var visible = banner != null && banner.IsVisible;
            _bannerGroup.alpha = visible ? 1f : 0f;
            _bannerGroup.blocksRaycasts = false;
            if (!visible)
            {
                _hideBannerAt = -1f;
                return;
            }

            _bannerTitle.text = banner.Title;
            _bannerSubtitle.text = banner.Subtitle;
            _bannerAccent.color = BannerColor(banner.Kind);

            _hideBannerAt = banner.Kind is HudBannerKind.Cleared
                or HudBannerKind.Failed
                or HudBannerKind.Respawning
                ? -1f
                : Time.unscaledTime + 2.4f;
        }

        private static Color BannerColor(HudBannerKind kind)
        {
            return kind switch
            {
                HudBannerKind.Cleared =>
                    new Color32(242, 201, 107, 255),
                HudBannerKind.Failed =>
                    new Color32(255, 99, 121, 255),
                HudBannerKind.Respawning =>
                    new Color32(107, 140, 255, 255),
                _ => new Color32(88, 224, 231, 255)
            };
        }

        /// <summary>
        /// 将物品品质映射为项目统一强调色。
        /// </summary>
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

        private static string FormatPhase(LevelPhase phase)
        {
            return phase switch
            {
                LevelPhase.Preparing => "PREPARING",
                LevelPhase.Intro => "MISSION BRIEF",
                LevelPhase.Combat => "IN COMBAT",
                LevelPhase.Cleared => "CLEARED",
                LevelPhase.Failed => "FAILED",
                LevelPhase.Exiting => "EXITING",
                _ => "STANDBY"
            };
        }
    }
}
