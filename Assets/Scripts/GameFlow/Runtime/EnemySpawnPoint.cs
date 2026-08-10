using UnityEngine;

namespace Train.GameFlow.Runtime
{
    /// <summary>
    /// 场景中的敌人出生点。设计者只需要拖动这个预制体并配置敌人 Prefab，
    /// 编辑器同步工具会把它转换成关卡 SO 数据。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemySpawnPoint : MonoBehaviour
    {
        [SerializeField] private string _spawnId = "enemy_01";
        [SerializeField] private string _archetypeId = "enemy";
        [SerializeField] private GameObject _enemyPrefab;
        [SerializeField, HideInInspector] private string _prefabLocation;

        /// <summary>出生点稳定 ID。</summary>
        public string SpawnId => _spawnId;

        /// <summary>敌人类型 ID，用于统计、任务和 Luban 数值表关联。</summary>
        public string ArchetypeId => _archetypeId;

        /// <summary>编辑器中拖入的敌人预制体。</summary>
        public GameObject EnemyPrefab => _enemyPrefab;

        /// <summary>由编辑器同步的 YooAsset 资源地址。</summary>
        public string PrefabLocation => _prefabLocation;

        /// <summary>为旧场景或编辑器工具设置出生点数据。</summary>
        public void Configure(
            string spawnId,
            string archetypeId,
            GameObject enemyPrefab,
            string prefabLocation = null)
        {
            _spawnId = string.IsNullOrWhiteSpace(spawnId)
                ? "enemy_01"
                : spawnId;
            _archetypeId = string.IsNullOrWhiteSpace(archetypeId)
                ? "enemy"
                : archetypeId;
            _enemyPrefab = enemyPrefab;
            _prefabLocation = prefabLocation ?? _prefabLocation;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.28f, 0.25f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, 0.45f);
            Gizmos.DrawLine(
                transform.position,
                transform.position + transform.forward * 1.2f);
        }

    }
}
