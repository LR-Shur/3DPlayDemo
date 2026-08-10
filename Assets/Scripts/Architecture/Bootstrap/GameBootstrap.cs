using UnityEngine;
using UnityEngine.SceneManagement;

namespace Train.Architecture.Bootstrap
{
    /// <summary>
    /// 游戏进程级组合根，持有跨场景共享的服务、事件中心与资源上下文。
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class GameBootstrap : MonoBehaviour
    {
        private static GameBootstrap _instance;
        private static bool _isApplicationQuitting;

        /// <summary>获取或创建当前游戏进程唯一的组合根。</summary>
        public static GameBootstrap Instance => EnsureExists();

        /// <summary>
        /// 获取应用是否正在退出。
        /// 退出阶段不再主动释放 YooAsset 租约，避免晚于资源调度器销毁后发起卸载。
        /// </summary>
        public static bool IsApplicationQuitting => _isApplicationQuitting;

        /// <summary>获取跨场景共享的游戏上下文。</summary>
        public GameContext Context { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null;
            _isApplicationQuitting = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            EnsureExists();
        }

        /// <summary>
        /// 返回现有组合根；不存在时创建一个常驻对象。
        /// 此方法幂等，可供任意独立测试场景调用。
        /// </summary>
        public static GameBootstrap EnsureExists()
        {
            if (_instance != null)
            {
                return _instance;
            }

            _instance = FindAnyObjectByType<GameBootstrap>(FindObjectsInactive.Include);
            if (_instance != null)
            {
                _instance.Initialize();
                return _instance;
            }

            var root = new GameObject("[GameBootstrap]");
            _instance = root.AddComponent<GameBootstrap>();
            return _instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            Initialize();
            DontDestroyOnLoad(gameObject);
        }

        private void Initialize()
        {
            if (Context != null)
            {
                return;
            }

            Context = new GameContext();
            SceneManager.sceneLoaded += OnSceneLoaded;

            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                EnsureSceneBootstrap(SceneManager.GetSceneAt(index));
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureSceneBootstrap(scene);
        }

        public SceneBootstrap EnsureSceneBootstrap(Scene scene)
        {
            return SceneBootstrap.EnsureForScene(scene, this);
        }

        private void OnApplicationQuit()
        {
            _isApplicationQuitting = true;
        }

        private void OnDestroy()
        {
            if (_instance != this)
            {
                return;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (!_isApplicationQuitting)
            {
                Context?.Dispose();
            }

            Context = null;
            _instance = null;
        }
    }
}
