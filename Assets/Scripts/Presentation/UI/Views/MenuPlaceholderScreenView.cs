using TMPro;
using UnityEngine;

namespace Train.Presentation.UI.Views
{
    /// <summary>
    /// 在任务、角色和档案功能接入真实 Presenter 前提供统一页面骨架。
    /// 后续替换页面时不会影响主菜单导航与输入锁。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MenuPlaceholderScreenView : MonoBehaviour
    {
        [SerializeField] private GameObject _screenRoot;
        [SerializeField] private TMP_Text _eyebrow;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _description;

        /// <summary>配置页面根节点和文本引用。</summary>
        public void Configure(
            GameObject screenRoot,
            TMP_Text eyebrow,
            TMP_Text title,
            TMP_Text description)
        {
            _screenRoot = screenRoot;
            _eyebrow = eyebrow;
            _title = title;
            _description = description;
        }

        /// <summary>设置页面内容并显示。</summary>
        public void Show(
            string eyebrow,
            string title,
            string description)
        {
            if (_screenRoot == null)
            {
                return;
            }

            _eyebrow.text = eyebrow;
            _title.text = title;
            _description.text = description;
            _screenRoot.SetActive(true);
        }

        /// <summary>隐藏占位页面。</summary>
        public void Hide()
        {
            if (_screenRoot != null)
            {
                _screenRoot.SetActive(false);
            }
        }
    }
}
