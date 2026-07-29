using Train.Gameplay.Camera;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Train.EditorTools
{
    /// <summary>
    /// 将当前场景的玩家镜头迁移到 Cinemachine 3。
    /// 创建观察目标、Cinemachine Brain、轨道虚拟相机和输入适配器，并移除旧的自写相机控制器。
    /// </summary>
    public static class CinemachinePlayerCameraSetup
    {
        private const string PlayerName = "Player";
        private const string CameraTargetName = "PlayerCameraTarget";
        private const string VirtualCameraName = "CM_PlayerCamera";
        private const string PlayerScenePath = "Assets/Scenes/New Scene.unity";
        private const float TargetHeight = 1.45f;

        /// <summary>
        /// 创建并配置玩家使用的 Cinemachine 第三人称自由观察镜头。
        /// </summary>
        [MenuItem("Train/Camera/配置玩家 Cinemachine 镜头")]
        public static void ConfigurePlayerCamera()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != PlayerScenePath)
            {
                EditorSceneManager.OpenScene(PlayerScenePath, OpenSceneMode.Single);
            }

            var player = GameObject.Find(PlayerName);
            var mainCamera = Camera.main != null
                ? Camera.main
                : Object.FindFirstObjectByType<Camera>();
            if (player == null || mainCamera == null)
            {
                Debug.LogError("配置 Cinemachine 镜头失败：未找到 Player 或 Main Camera。");
                return;
            }

            // 防遮挡组件按标签忽略玩家自身，避免镜头误判角色的 CharacterController 为障碍物。
            player.tag = "Player";

            var oldController = mainCamera.GetComponent("ThirdPersonCameraController");
            if (oldController != null)
            {
                Object.DestroyImmediate(oldController);
            }

            if (mainCamera.GetComponent<CinemachineBrain>() == null)
            {
                mainCamera.gameObject.AddComponent<CinemachineBrain>();
            }

            var cameraTarget = GetOrCreateCameraTarget(player.transform);
            var virtualCameraObject = GameObject.Find(VirtualCameraName) ?? new GameObject(VirtualCameraName);
            var virtualCamera = GetOrAddComponent<CinemachineCamera>(virtualCameraObject);
            virtualCamera.Follow = cameraTarget;
            virtualCamera.LookAt = cameraTarget;
            virtualCamera.Lens = LensSettings.FromCamera(mainCamera);

            var orbitalFollow = GetOrAddComponent<CinemachineOrbitalFollow>(virtualCameraObject);
            ConfigureOrbit(orbitalFollow, mainCamera.transform.eulerAngles);

            var composer = GetOrAddComponent<CinemachineRotationComposer>(virtualCameraObject);
            composer.Damping = Vector2.zero;
            composer.CenterOnActivate = true;

            var deoccluder = GetOrAddComponent<CinemachineDeoccluder>(virtualCameraObject);
            deoccluder.CollideAgainst = 1;
            deoccluder.IgnoreTag = player.tag;
            deoccluder.MinimumDistanceFromTarget = 0.3f;

            GetOrAddComponent<CinemachineLookInputAdapter>(virtualCameraObject);

            EditorSceneManager.MarkSceneDirty(player.scene);
            EditorSceneManager.SaveScene(player.scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AssetDatabase.DeleteAsset("Assets/Scripts/Gameplay/Camera/ThirdPersonCameraController.cs");
            Debug.Log("已切换为 Cinemachine 玩家镜头。", virtualCameraObject);
        }

        /// <summary>
        /// 获取或创建位于角色观察高度的镜头目标。
        /// </summary>
        /// <param name="playerTransform">Player 根物体的 Transform。</param>
        /// <returns>可供 Cinemachine 跟随和观察的目标 Transform。</returns>
        private static Transform GetOrCreateCameraTarget(Transform playerTransform)
        {
            var cameraTarget = playerTransform.Find(CameraTargetName);
            if (cameraTarget == null)
            {
                var targetObject = new GameObject(CameraTargetName);
                cameraTarget = targetObject.transform;
                cameraTarget.SetParent(playerTransform, false);
            }

            cameraTarget.localPosition = Vector3.up * TargetHeight;
            cameraTarget.localRotation = Quaternion.identity;
            return cameraTarget;
        }

        /// <summary>
        /// 配置自由观察轨道的距离、平滑参数和初始水平垂直角度。
        /// </summary>
        /// <param name="orbitalFollow">需要配置的 Cinemachine 轨道组件。</param>
        /// <param name="cameraEulerAngles">切换前 Main Camera 的欧拉角，用于避免镜头跳变。</param>
        private static void ConfigureOrbit(CinemachineOrbitalFollow orbitalFollow, Vector3 cameraEulerAngles)
        {
            orbitalFollow.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            orbitalFollow.Radius = 5f;
            orbitalFollow.RecenteringTarget = CinemachineOrbitalFollow.ReferenceFrames.AxisCenter;
            orbitalFollow.TrackerSettings = new Unity.Cinemachine.TargetTracking.TrackerSettings
            {
                BindingMode = Unity.Cinemachine.TargetTracking.BindingMode.WorldSpace,
                PositionDamping = new Vector3(0.08f, 0.08f, 0.08f),
            };

            var horizontalAxis = orbitalFollow.HorizontalAxis;
            horizontalAxis.Value = NormalizeAngle(cameraEulerAngles.y);
            orbitalFollow.HorizontalAxis = horizontalAxis;

            var verticalAxis = orbitalFollow.VerticalAxis;
            verticalAxis.Range = new Vector2(-35f, 70f);
            verticalAxis.Value = Mathf.Clamp(NormalizeAngle(cameraEulerAngles.x), verticalAxis.Range.x, verticalAxis.Range.y);
            orbitalFollow.VerticalAxis = verticalAxis;
        }

        /// <summary>
        /// 获取对象已有的组件，缺失时自动添加。
        /// </summary>
        /// <typeparam name="T">需要获取或添加的组件类型。</typeparam>
        /// <param name="gameObject">组件所属对象。</param>
        /// <returns>可用的目标组件。</returns>
        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            return gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();
        }

        /// <summary>
        /// 将 Unity 的 0 至 360 度欧拉角转换为 -180 至 180 度范围。
        /// </summary>
        /// <param name="angle">需要转换的角度。</param>
        /// <returns>适合轨道轴使用的有符号角度。</returns>
        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }
    }
}
