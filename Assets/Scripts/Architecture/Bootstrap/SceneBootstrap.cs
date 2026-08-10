using Train.Architecture.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Train.Architecture.Bootstrap
{
    /// <summary>
    /// 场景级组合根，让任意场景都能单独运行并持有自己的事件总线。
    /// </summary>
    [DefaultExecutionOrder(-9000)]
    [DisallowMultipleComponent]
    public sealed class SceneBootstrap : MonoBehaviour
    {
        private GameBootstrap _game;

        public SceneContext Context { get; private set; }

        private void Awake()
        {
            Initialize(GameBootstrap.EnsureExists());
        }

        internal void Initialize(GameBootstrap game)
        {
            if (Context != null)
            {
                return;
            }

            _game = game;
            Context = new SceneContext(game.Context);
        }

        public static SceneBootstrap EnsureForScene(Scene scene, GameBootstrap game = null)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                var existing = root.GetComponentInChildren<SceneBootstrap>(true);
                if (existing != null)
                {
                    existing.Initialize(game ?? GameBootstrap.EnsureExists());
                    return existing;
                }
            }

            var bootstrapRoot = new GameObject("[SceneBootstrap]");
            SceneManager.MoveGameObjectToScene(bootstrapRoot, scene);
            var bootstrap = bootstrapRoot.AddComponent<SceneBootstrap>();
            bootstrap.Initialize(game ?? GameBootstrap.EnsureExists());
            return bootstrap;
        }

        public static IEventBus ResolveEvents(Component owner)
        {
            if (owner != null && owner.gameObject.scene.IsValid())
            {
                var sceneBootstrap = EnsureForScene(owner.gameObject.scene);
                if (sceneBootstrap != null)
                {
                    return sceneBootstrap.Context.Events;
                }
            }

            return GameBootstrap.Instance.Context.Events;
        }

        private void OnDestroy()
        {
            Context?.Dispose();
            Context = null;
            _game = null;
        }
    }
}
