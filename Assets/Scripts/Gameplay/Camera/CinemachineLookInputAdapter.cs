using Train.Gameplay.Player.Input;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        private bool _externalLookBlocked;

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
            if (_lockCursorOnStart && !_externalLookBlocked)
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
            if (_lockCursorOnStart && hasFocus && !_externalLookBlocked)
            {
                SetCursorLocked(true);
            }
        }

        /// <summary>
        /// 将本帧 Look 输入写入 Cinemachine 的水平与垂直轨道轴。
        /// </summary>
        private void Update()
        {
            if (_externalLookBlocked ||
                _input == null ||
                _orbitalFollow == null)
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

        public void SetExternalLookBlocked(bool blocked)
        {
            _externalLookBlocked = blocked;
            if (blocked)
            {
                SetCursorLocked(false);
            }
            else if (_lockCursorOnStart && isActiveAndEnabled)
            {
                SetCursorLocked(true);
            }
        }

        /// <summary>运行时玩家由 YooAsset 生成后，绑定新的输入读取器。</summary>
        public void BindInput(PlayerInputReader input)
        {
            _input = input;
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

    /// <summary>
    /// Cinemachine 运行时绑定器。GameFlow 只依赖 Component，不直接依赖相机包类型。
    /// </summary>
    public static class PlayerCameraRuntimeBinder
    {
        /// <summary>确保场景中存在带 CinemachineBrain 的主相机。</summary>
        public static void EnsureMainCamera()
        {
            var mainCamera = UnityEngine.Camera.main ??
                Object.FindFirstObjectByType<UnityEngine.Camera>(
                    FindObjectsInactive.Include);
            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                mainCamera = cameraObject.AddComponent<UnityEngine.Camera>();
            }

            if (mainCamera.GetComponent<CinemachineBrain>() == null)
            {
                mainCamera.gameObject.AddComponent<CinemachineBrain>();
            }
        }

        /// <summary>查找指定场景中的 CinemachineCamera。</summary>
        public static Component FindVirtualCamera(Scene scene)
        {
            foreach (var camera in Object.FindObjectsByType<CinemachineCamera>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (camera.gameObject.scene == scene)
                {
                    return camera;
                }
            }

            return null;
        }

        /// <summary>把虚拟相机绑定到玩家观察点和输入读取器。</summary>
        public static bool TryBind(
            Component cameraComponent,
            Transform target,
            PlayerInputReader input)
        {
            var camera = cameraComponent as CinemachineCamera;
            if (camera == null || target == null)
            {
                return false;
            }

            var cameraTarget = camera.Target;
            cameraTarget.TrackingTarget = target;
            cameraTarget.LookAtTarget = target;
            cameraTarget.CustomLookAtTarget = true;
            camera.Target = cameraTarget;
            camera.Priority = 100;

            camera.GetComponent<CinemachineLookInputAdapter>()?.BindInput(input);
            return true;
        }
    }
}
