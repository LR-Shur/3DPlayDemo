using Train.Gameplay.Player.Input;
using Unity.Cinemachine;
using UnityEngine;

namespace Train.Gameplay.Camera
{
    /// <summary>
    /// 将项目现有的玩家 Look 输入转换为 Cinemachine 轨道相机轴输入。
    /// 此类只负责输入适配与鼠标锁定，不参与相机位置、旋转或避障计算。
    /// </summary>
    [RequireComponent(typeof(CinemachineOrbitalFollow))]
    public sealed class CinemachineLookInputAdapter : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader _input;
        [SerializeField, Min(0f)] private float _lookSensitivity = 0.12f;
        [SerializeField] private bool _lockCursorOnStart = true;

        private CinemachineOrbitalFollow _orbitalFollow;

        /// <summary>
        /// 缓存 Cinemachine 轨道组件，并自动查找 Player 上的输入读取器。
        /// </summary>
        private void Awake()
        {
            _orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
            _input ??= GameObject.Find("Player")?.GetComponent<PlayerInputReader>();
        }

        /// <summary>
        /// 在启用时锁定鼠标光标，提供第三人称镜头常用的自由观察操作。
        /// </summary>
        private void OnEnable()
        {
            if (_lockCursorOnStart)
            {
                SetCursorLocked(true);
            }
        }

        /// <summary>
        /// 在应用重新获得焦点后恢复鼠标锁定。
        /// </summary>
        /// <param name="hasFocus">应用是否获得输入焦点。</param>
        private void OnApplicationFocus(bool hasFocus)
        {
            if (_lockCursorOnStart && hasFocus)
            {
                SetCursorLocked(true);
            }
        }

        /// <summary>
        /// 将本帧 Look 输入写入 Cinemachine 的水平与垂直轨道轴。
        /// </summary>
        private void Update()
        {
            if (_input == null || _orbitalFollow == null)
            {
                return;
            }

            var look = _input.Look;
            var horizontalAxis = _orbitalFollow.HorizontalAxis;
            horizontalAxis.Value += look.x * _lookSensitivity;
            _orbitalFollow.HorizontalAxis = horizontalAxis;

            var verticalAxis = _orbitalFollow.VerticalAxis;
            verticalAxis.Value -= look.y * _lookSensitivity;
            _orbitalFollow.VerticalAxis = verticalAxis;
        }

        /// <summary>
        /// 锁定或释放系统鼠标光标。
        /// </summary>
        /// <param name="locked">是否锁定到游戏窗口中心。</param>
        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
