using System;
using System.Threading;
using System.Threading.Tasks;
using Train.Architecture.Assets;
using Train.Architecture.Bootstrap;
using Train.Composition;
using Train.Gameplay.Camera;
using Train.Gameplay.Player.Core;
using Train.Gameplay.Player.Input;
using Train.WorldInteraction.Runtime;
using UnityEngine;

namespace Train.Shop.Runtime
{
    /// <summary>
    /// 商店场景的轻量组合根：生成玩家、相机、地面和商店 NPC。
    /// 场景本身只保存这个入口，便于复制场景后独立测试。
    /// </summary>
    [DefaultExecutionOrder(-7000)]
    [DisallowMultipleComponent]
    public sealed class ShopSceneController : MonoBehaviour
    {
        private const string PlayerLocation =
            AssetLocations.PlayerPrefab;
        private const string CameraLocation =
            AssetLocations.PlayerCameraPrefab;
        private const string NpcVisualLocation = "NPC/Shopkeeper_Casual2";

        private IInstanceLease _playerLease;
        private IInstanceLease _cameraLease;
        private CancellationTokenSource _lifetime;

        /// <summary>当前商店场景是否已经完成基础对象生成。</summary>
        public bool IsReady { get; private set; }

        private void Awake()
        {
            _lifetime = new CancellationTokenSource();
            SceneBootstrap.EnsureForScene(gameObject.scene);
            BuildStaticScene();
        }

        private void Start()
        {
            _ = PrepareAsync(_lifetime.Token);
        }

        /// <summary>生成商店地面、灯光和可交互 NPC。</summary>
        private void BuildStaticScene()
        {
            if (GameObject.Find("Shopkeeper") == null)
            {
                var npc = new GameObject("Shopkeeper");
                npc.transform.position = new Vector3(0f, 0f, 2f);
                var collider = npc.AddComponent<SphereCollider>();
                collider.isTrigger = true;
                collider.center = new Vector3(0f, 1f, 0f);
                collider.radius = 1.35f;

                var interactionPoint = new GameObject("InteractionPoint");
                interactionPoint.transform.SetParent(npc.transform, false);
                interactionPoint.transform.localPosition = new Vector3(0f, 1.1f, 0f);

                var terminal = npc.AddComponent<WorldDialogueTerminal>();
                terminal.Configure(
                    "shopkeeper.main",
                    "dialogue_shopkeeper",
                    "补给商人",
                    40,
                    interactionPoint.transform,
                    1.6f);
                var bridge = npc.AddComponent<ShopkeeperDialogueBridge>();
                bridge.Configure("dialogue_shopkeeper");

                var visualPrefab = Resources.Load<GameObject>(NpcVisualLocation);
                if (visualPrefab != null)
                {
                    var visual = Instantiate(visualPrefab, npc.transform, false);
                    visual.name = "ShopkeeperVisual";
                    visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                }
            }

            if (GameObject.Find("ShopFloor") == null)
            {
                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "ShopFloor";
                floor.transform.position = new Vector3(0f, -0.15f, 2f);
                floor.transform.localScale = new Vector3(16f, 0.3f, 12f);
            }

            if (FindFirstObjectByType<Light>() == null)
            {
                var lightObject = new GameObject("ShopLight");
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.1f;
                lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
        }

        /// <summary>使用统一资源服务生成玩家和跟随相机。</summary>
        private async Task PrepareAsync(CancellationToken cancellationToken)
        {
            try
            {
                var game = GameBootstrap.EnsureExists();
                var assets = await WaitForApplicationAsync(
                    game,
                    cancellationToken);
                await assets.InitializeAsync(cancellationToken);

                var player = GameObject.FindGameObjectWithTag("Player");
                if (player == null)
                {
                    _playerLease = await assets.InstantiateAsync(
                        PlayerLocation,
                        position: new Vector3(0f, 0f, -2f),
                        rotation: Quaternion.identity,
                        cancellationToken: cancellationToken);
                    player = _playerLease.Instance;
                    player.name = "Player";
                    player.tag = "Player";
                }

                PlayerCameraRuntimeBinder.EnsureMainCamera();
                var camera = PlayerCameraRuntimeBinder.FindVirtualCamera(gameObject.scene);
                if (camera == null)
                {
                    _cameraLease = await assets.InstantiateAsync(
                        CameraLocation,
                        cancellationToken: cancellationToken);
                    camera = _cameraLease.Instance.GetComponent("CinemachineCamera");
                }

                var input = player.GetComponent<PlayerInputReader>();
                var target = player.transform.Find("PlayerCameraTarget") ?? player.transform;
                if (!PlayerCameraRuntimeBinder.TryBind(camera, target, input))
                {
                    throw new InvalidOperationException(
                        "商店场景玩家相机绑定失败，请检查 CM_PlayerCamera 预制体。 ");
                }

                // 让控制器在相机绑定完成后的下一帧自动建立玩家状态机。
                var playerController = player.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerController.enabled = true;
                    playerController.InitializeRuntime();
                }
                IsReady = true;
            }
            catch (OperationCanceledException)
            {
                // 退出 PlayMode 或切换场景时正常取消。
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        /// <summary>
        /// 等待 YooAsset 与应用层服务完成安装，确保独立打开场景时也具备输入、动画、对话和商店服务。
        /// </summary>
        private static async Task<IAssetService> WaitForApplicationAsync(
            GameBootstrap game,
            CancellationToken cancellationToken)
        {
            for (var frame = 0; frame < 600; frame++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (game.Context.Assets != null)
                {
                    var startup = game.GetComponent<GameApplicationStartup>();
                    if (startup != null && startup.IsReady)
                    {
                        return game.Context.Assets;
                    }
                }

                await Task.Yield();
            }

            throw new InvalidOperationException(
                "商店场景等待应用服务超时，请确认 YooAssetRuntimeInstaller 和 GameApplicationStartup 已执行。 ");
        }

        private void OnDestroy()
        {
            _lifetime?.Cancel();
            _lifetime?.Dispose();
            _lifetime = null;
            _cameraLease?.Dispose();
            _playerLease?.Dispose();
        }
    }
}
