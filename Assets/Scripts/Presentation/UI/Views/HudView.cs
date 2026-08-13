using System;
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
    /// 灏?HUD 瑙嗗浘妯″瀷娓叉煋鍒扮敓鍛芥潯銆佸叧鍗＄洰鏍囧拰涓ぎ鎻愮ず妯箙銆?    /// </summary>
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

        /// <summary>纭繚鎴樻枟 HUD 濮嬬粓鏈夊洓涓富鍔ㄩ亾鍏峰揩鎹锋Ы銆?/summary>
        /// <summary>
        /// 閰嶇疆 HUD 涓渶瑕佸啓鍏ョ殑鏂囧瓧銆佸浘鐗囧拰妯箙寮曠敤銆?        /// </summary>
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
            ConfigureHealthValue();
        }

        /// <inheritdoc />
        public void Render(HudViewModel viewModel)
        {
            if (viewModel == null ||
                _levelName == null ||
                _phase == null ||
                _objective == null ||
                _healthValue == null ||
                _healthFill == null)
            {
                return;
            }

            _levelName.text = viewModel.HasLevel
                ? viewModel.LevelName
                : "未进入关卡";
            _phase.text = FormatPhase(viewModel.Phase);
            _objective.text = viewModel.HasLevel
                ? viewModel.ObjectiveText
                : "等待任务";
            _healthFill.fillAmount = viewModel.Health01;
            _healthValue.text = viewModel.PlayerMaxHealth > 0f
                ? $"HP {Mathf.Max(0f, viewModel.PlayerHealth):0.#} / " +
                  $"{Mathf.Max(0f, viewModel.PlayerMaxHealth):0.#}"
                : "HP 100 / 100";

            RenderBanner(viewModel.Banner);
        }

        /// <inheritdoc />
        public void RenderInteractionPrompt(
            InteractionPromptViewModel viewModel)
        {
            if (_interactionGroup == null ||
                _interactionName == null ||
                _interactionHint == null ||
                _interactionAccent == null)
            {
                return;
            }

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

        /// <summary>
        /// 寮哄埗鐢熷懡鏁板瓧澶勪簬鍙鐘舵€侊紝骞跺浐瀹氬湪鐢熷懡鏉′笅鏂癸紝閬垮厤鏃ч鍒朵綋甯冨眬鎶婂畠瑁佹帀銆?        /// </summary>
        private void ConfigureHealthValue()
        {
            if (_healthValue == null)
            {
                throw new InvalidOperationException(
                    "HudView requires a configured PlayerStatus/HealthValue reference.");
            }

            _healthValue.gameObject.SetActive(true);
            _healthValue.color = new Color32(220, 235, 242, 255);
            _healthValue.fontSize = Mathf.Max(18f, _healthValue.fontSize);
            _healthValue.raycastTarget = false;
            _healthValue.alignment = TextAlignmentOptions.Right;
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
        /// 灏嗙墿鍝佸搧璐ㄦ槧灏勪负椤圭洰缁熶竴寮鸿皟鑹层€?        /// </summary>
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
                LevelPhase.Preparing => "准备中",
                LevelPhase.Intro => "简报",
                LevelPhase.Combat => "战斗",
                LevelPhase.Cleared => "已完成",
                LevelPhase.Failed => "失败",
                LevelPhase.Exiting => "离开中",
                _ => "等待中"
            };
        }
    }
}
