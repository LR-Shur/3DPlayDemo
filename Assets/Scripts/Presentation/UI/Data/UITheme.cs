using UnityEngine;

namespace Train.Presentation.UI.Data
{
    /// <summary>
    /// 集中配置游戏 UI 的表面色、强调色和文字色。
    /// </summary>
    [CreateAssetMenu(
        menuName = "Train/UI/UI Theme",
        fileName = "UITheme")]
    public sealed class UITheme : ScriptableObject
    {
        [Header("Surfaces")]
        [SerializeField] private Color _background =
            new Color32(11, 17, 32, 255);
        [SerializeField] private Color _panel =
            new Color32(17, 28, 44, 232);
        [SerializeField] private Color _raised =
            new Color32(23, 40, 61, 255);
        [SerializeField] private Color _outline =
            new Color32(50, 70, 92, 255);

        [Header("Accents")]
        [SerializeField] private Color _cyan =
            new Color32(88, 224, 231, 255);
        [SerializeField] private Color _blue =
            new Color32(107, 140, 255, 255);
        [SerializeField] private Color _gold =
            new Color32(242, 201, 107, 255);
        [SerializeField] private Color _danger =
            new Color32(255, 99, 121, 255);

        [Header("Typography")]
        [SerializeField] private Color _textPrimary =
            new Color32(244, 247, 251, 255);
        [SerializeField] private Color _textSecondary =
            new Color32(170, 184, 200, 255);
        [SerializeField] private Color _textMuted =
            new Color32(112, 132, 154, 255);

        /// <summary>获取全屏背景色。</summary>
        public Color Background => _background;

        /// <summary>获取主要面板底色。</summary>
        public Color Panel => _panel;

        /// <summary>获取抬升层级的表面色。</summary>
        public Color Raised => _raised;

        /// <summary>获取面板与控件描边色。</summary>
        public Color Outline => _outline;

        /// <summary>获取青色主强调色。</summary>
        public Color Cyan => _cyan;

        /// <summary>获取蓝色辅助强调色。</summary>
        public Color Blue => _blue;

        /// <summary>获取金色高价值强调色。</summary>
        public Color Gold => _gold;

        /// <summary>获取危险或失败状态色。</summary>
        public Color Danger => _danger;

        /// <summary>获取主要文字颜色。</summary>
        public Color TextPrimary => _textPrimary;

        /// <summary>获取次要文字颜色。</summary>
        public Color TextSecondary => _textSecondary;

        /// <summary>获取弱化文字颜色。</summary>
        public Color TextMuted => _textMuted;
    }
}
