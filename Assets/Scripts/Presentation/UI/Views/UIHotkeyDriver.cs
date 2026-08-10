using Train.Presentation.UI.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Train.Presentation.UI.Views
{
    /// <summary>
    /// 监听键盘快捷键，并把主菜单页面意图转交给 UI 服务。
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class UIHotkeyDriver : MonoBehaviour
    {
        private IUIService _ui;

        /// <summary>
        /// 注入负责实际页面切换的 UI 服务。
        /// </summary>
        public void Initialize(IUIService ui)
        {
            _ui = ui;
        }

        private void Update()
        {
            if (_ui == null || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.bKey.wasPressedThisFrame ||
                Keyboard.current.tabKey.wasPressedThisFrame)
            {
                _ui.ToggleInventory();
            }
            else if (Keyboard.current.cKey.wasPressedThisFrame)
            {
                _ui.ToggleEquipment();
            }
            else if (Keyboard.current.escapeKey.wasPressedThisFrame &&
                     _ui.IsMenuOpen)
            {
                _ui.HideMenu();
            }
        }
    }
}
