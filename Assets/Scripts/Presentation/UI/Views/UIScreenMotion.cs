using UnityEngine;

namespace Train.Presentation.UI.Views
{
    /// <summary>
    /// 为常驻页面提供不受 Time.timeScale 影响的淡入淡出、位移和缩放转场。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIScreenMotion : MonoBehaviour
    {
        public const float ShowDuration = 0.22f;
        public const float HideDuration = 0.12f;
        public const float ShowOffset = 18f;
        public const float ShowScale = 0.985f;

        /// <summary>页面转场状态。</summary>
        public enum VisibilityState
        {
            Hidden,
            Showing,
            Visible,
            Hiding
        }

        private GameObject _target;
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Vector2 _basePosition;
        private Vector3 _baseScale;
        private Vector2 _startPosition;
        private Vector3 _startScale;
        private float _startAlpha;
        private float _elapsed;
        private bool _targetVisible;

        /// <summary>获取当前转场状态。</summary>
        public VisibilityState State { get; private set; } =
            VisibilityState.Hidden;

        /// <summary>
        /// 将组件绑定到页面根节点并记录布局基准，重复绑定不会积累位移或缩放。
        /// </summary>
        public void Bind(GameObject target)
        {
            target = target == null ? gameObject : target;
            if (_target == target && _canvasGroup != null)
            {
                return;
            }

            _target = target;
            _rectTransform = target.GetComponent<RectTransform>();
            _canvasGroup = target.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                target.AddComponent<CanvasGroup>();
                _canvasGroup = target.GetComponent<CanvasGroup>();
            }
            _basePosition = _rectTransform != null
                ? _rectTransform.anchoredPosition
                : Vector2.zero;
            _baseScale = target.transform.localScale;

            if (target.activeSelf)
            {
                State = VisibilityState.Visible;
                _targetVisible = true;
                ApplyVisual(1f, _basePosition, _baseScale);
                SetInteraction(true, true);
            }
            else
            {
                State = VisibilityState.Hidden;
                _targetVisible = false;
                ApplyVisual(0f, _basePosition, _baseScale);
                SetInteraction(false, false);
            }
        }

        /// <summary>开始显示或隐藏页面；重复调用会从当前画面状态平滑反向。</summary>
        public void SetVisible(bool visible)
        {
            Bind(_target ?? gameObject);

            if (visible)
            {
                BeginShow();
            }
            else
            {
                BeginHide();
            }
        }

        /// <summary>
        /// 立即设置页面状态，不播放转场；用于初始化、销毁和全局菜单状态同步。
        /// </summary>
        public void SetVisibleImmediate(bool visible)
        {
            Bind(_target ?? gameObject);

            _targetVisible = visible;
            _elapsed = visible ? ShowDuration : HideDuration;
            State = visible
                ? VisibilityState.Visible
                : VisibilityState.Hidden;
            SetInteraction(visible, visible);
            ApplyVisual(
                visible ? 1f : 0f,
                _basePosition,
                _baseScale);
            _target.SetActive(visible);
        }

        /// <summary>纯逻辑的缓动状态转换，供运行时代码和 EditMode 测试复用。</summary>
        public static VisibilityState ResolveTransitionState(
            VisibilityState current,
            bool visible,
            bool complete)
        {
            if (complete)
            {
                return visible ? VisibilityState.Visible : VisibilityState.Hidden;
            }

            if (visible)
            {
                return current == VisibilityState.Visible
                    ? VisibilityState.Visible
                    : VisibilityState.Showing;
            }

            return current == VisibilityState.Hidden
                ? VisibilityState.Hidden
                : VisibilityState.Hiding;
        }

        /// <summary>三次方缓出。</summary>
        public static float EaseOutCubic(float progress)
        {
            var inverse = 1f - Mathf.Clamp01(progress);
            return 1f - inverse * inverse * inverse;
        }

        /// <summary>三次方缓入。</summary>
        public static float EaseInCubic(float progress)
        {
            var value = Mathf.Clamp01(progress);
            return value * value * value;
        }

        private void Awake()
        {
            Bind(gameObject);
        }

        private void Update()
        {
            if (_target == null ||
                (State != VisibilityState.Showing &&
                 State != VisibilityState.Hiding))
            {
                return;
            }

            var duration = _targetVisible ? ShowDuration : HideDuration;
            _elapsed = Mathf.Min(
                duration,
                _elapsed + Time.unscaledDeltaTime);
            var progress = duration <= 0f ? 1f : _elapsed / duration;
            var eased = _targetVisible
                ? EaseOutCubic(progress)
                : EaseInCubic(progress);
            ApplyVisual(
                Mathf.Lerp(_startAlpha, _targetVisible ? 1f : 0f, eased),
                Vector2.Lerp(
                    _startPosition,
                    _basePosition,
                    eased),
                Vector3.Lerp(
                    _startScale,
                    _baseScale,
                    eased));

            if (progress >= 1f)
            {
                CompleteTransition();
            }
        }

        private void BeginShow()
        {
            var wasActive = _target.activeSelf;
            _target.SetActive(true);
            if (State == VisibilityState.Visible && wasActive)
            {
                return;
            }

            _targetVisible = true;
            _elapsed = 0f;
            if (!wasActive || State == VisibilityState.Hidden)
            {
                _startAlpha = 0f;
                _startPosition = _basePosition + Vector2.up * ShowOffset;
                _startScale = _baseScale * ShowScale;
                ApplyVisual(_startAlpha, _startPosition, _startScale);
            }
            else
            {
                ReadCurrentVisual();
            }

            State = ResolveTransitionState(State, true, false);
            SetInteraction(true, false);
        }

        private void BeginHide()
        {
            if (!_target.activeSelf)
            {
                State = VisibilityState.Hidden;
                _targetVisible = false;
                SetInteraction(false, false);
                return;
            }

            if (State == VisibilityState.Hiding && !_targetVisible)
            {
                return;
            }

            _targetVisible = false;
            _elapsed = 0f;
            ReadCurrentVisual();
            State = ResolveTransitionState(State, false, false);
            SetInteraction(false, false);
        }

        private void CompleteTransition()
        {
            ApplyVisual(
                _targetVisible ? 1f : 0f,
                _basePosition,
                _baseScale);
            State = ResolveTransitionState(State, _targetVisible, true);
            if (_targetVisible)
            {
                SetInteraction(true, true);
                return;
            }

            SetInteraction(false, false);
            _target.SetActive(false);
        }

        private void ReadCurrentVisual()
        {
            _startAlpha = _canvasGroup.alpha;
            _startPosition = _rectTransform != null
                ? _rectTransform.anchoredPosition
                : _basePosition;
            _startScale = _target.transform.localScale;
        }

        private void ApplyVisual(
            float alpha,
            Vector2 position,
            Vector3 scale)
        {
            _canvasGroup.alpha = alpha;
            _target.transform.localScale = scale;
            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = position;
            }
        }

        private void SetInteraction(bool blocksRaycasts, bool interactable)
        {
            _canvasGroup.blocksRaycasts = blocksRaycasts;
            _canvasGroup.interactable = interactable;
        }
    }
}
