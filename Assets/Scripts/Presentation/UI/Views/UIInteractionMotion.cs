using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Train.Presentation.UI.Views
{
    /// <summary>
    /// 为运行时绑定的按钮提供轻量 hover/按下缩放，也可播放详情内容刷新反馈。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIInteractionMotion :
        MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        public const float HoverScale = 1.025f;
        public const float PressScale = 0.975f;
        public const float RefreshDuration = 0.16f;

        private RectTransform _rectTransform;
        private CanvasGroup _refreshGroup;
        private Vector3 _baseScale;
        private Vector2 _basePosition;
        private float _baseAlpha = 1f;
        private float _refreshElapsed;
        private float _refreshStartAlpha;
        private Vector2 _refreshStartPosition;
        private Button _button;
        private bool _hovered;
        private bool _pressed;
        private bool _refreshing;

        /// <summary>根据指针状态返回目标缩放，供 EditMode 纯逻辑测试使用。</summary>
        public static float TargetScale(bool hovered, bool pressed)
        {
            return pressed ? PressScale : hovered ? HoverScale : 1f;
        }

        /// <summary>不可交互的按钮始终保持基础缩放。</summary>
        public static float TargetScale(
            bool hovered,
            bool pressed,
            bool interactable)
        {
            return interactable ? TargetScale(hovered, pressed) : 1f;
        }

        /// <summary>播放一次短暂的详情透明度与上移刷新。</summary>
        public void PlayRefresh()
        {
            EnsureBaseTransform();
            EnsureRefreshGroup();
            _refreshStartAlpha = _refreshing
                ? _refreshGroup.alpha
                : _baseAlpha * 0.45f;
            _refreshStartPosition = _refreshing
                ? _rectTransform.anchoredPosition
                : _basePosition + Vector2.up * 8f;
            _refreshElapsed = 0f;
            _refreshing = true;
            _refreshGroup.alpha = _refreshStartAlpha;
            _rectTransform.anchoredPosition = _refreshStartPosition;
        }

        private void Awake()
        {
            EnsureBaseTransform();
            _button = GetComponent<Button>();
        }

        private void Update()
        {
            EnsureBaseTransform();
            if (_button != null)
            {
                if (!_button.interactable)
                {
                    _hovered = false;
                    _pressed = false;
                    transform.localScale = _baseScale;
                }
                else
                {
                    var targetScale = _baseScale * TargetScale(
                        _hovered,
                        _pressed,
                        _button.interactable);
                    var blend = 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime);
                    transform.localScale = Vector3.Lerp(
                        transform.localScale,
                        targetScale,
                        blend);
                }
            }

            if (!_refreshing)
            {
                return;
            }

            _refreshElapsed = Mathf.Min(
                RefreshDuration,
                _refreshElapsed + Time.unscaledDeltaTime);
            var progress = RefreshDuration <= 0f
                ? 1f
                : _refreshElapsed / RefreshDuration;
            var eased = UIScreenMotion.EaseOutCubic(progress);
            _refreshGroup.alpha = Mathf.Lerp(
                _refreshStartAlpha,
                _baseAlpha,
                eased);
            _rectTransform.anchoredPosition = Vector2.Lerp(
                _refreshStartPosition,
                _basePosition,
                eased);
            if (progress >= 1f)
            {
                _refreshing = false;
                _refreshGroup.alpha = _baseAlpha;
                _rectTransform.anchoredPosition = _basePosition;
            }
        }

        private void OnDisable()
        {
            _hovered = false;
            _pressed = false;
            _refreshing = false;
            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = _basePosition;
            }

            transform.localScale = _baseScale;
            if (_refreshGroup != null)
            {
                _refreshGroup.alpha = _baseAlpha;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_button != null && _button.interactable)
            {
                _hovered = true;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_button != null && _button.interactable)
            {
                _hovered = false;
                _pressed = false;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_button != null && _button.interactable)
            {
                _pressed = true;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_button != null && _button.interactable)
            {
                _pressed = false;
            }
        }

        private void EnsureBaseTransform()
        {
            if (_rectTransform != null)
            {
                return;
            }

            _rectTransform = transform as RectTransform;
            _baseScale = transform.localScale;
            _basePosition = _rectTransform != null
                ? _rectTransform.anchoredPosition
                : Vector2.zero;
        }

        private void EnsureRefreshGroup()
        {
            if (_refreshGroup != null)
            {
                return;
            }

            _refreshGroup = GetComponent<CanvasGroup>();
            if (_refreshGroup == null)
            {
                gameObject.AddComponent<CanvasGroup>();
                _refreshGroup = GetComponent<CanvasGroup>();
            }
            _baseAlpha = _refreshGroup.alpha;
        }
    }
}
