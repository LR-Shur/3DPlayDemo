using UnityEngine;

namespace Train.GameFlow.Runtime
{
    /// <summary>
    /// 场景中的玩家出生点预制体标记，供玩家生成和复活系统复用。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerSpawnPoint : MonoBehaviour
    {
        [SerializeField] private string _spawnId = "player_spawn";

        /// <summary>出生点稳定 ID。</summary>
        public string SpawnId => _spawnId;

        /// <summary>设置出生点 ID。</summary>
        public void Configure(string spawnId)
        {
            _spawnId = string.IsNullOrWhiteSpace(spawnId)
                ? "player_spawn"
                : spawnId;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.25f, 0.9f, 1f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, 0.35f);
            Gizmos.DrawLine(
                transform.position,
                transform.position + transform.forward * 1.2f);
        }
    }
}
