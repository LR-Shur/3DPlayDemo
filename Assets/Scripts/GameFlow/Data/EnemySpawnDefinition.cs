using System;
using UnityEngine;

namespace Train.GameFlow.Data
{
    /// <summary>
    /// 关卡配置中一个敌人出生点的序列化定义。
    /// </summary>
    [Serializable]
    public sealed class EnemySpawnDefinition
    {
        [SerializeField] private string _spawnId = "enemy_01";
        [SerializeField] private string _archetypeId = "kaykit_knight";
        [SerializeField] private string _prefabLocation;
        [SerializeField] private Vector3 _position;
        [SerializeField] private Vector3 _eulerAngles;

        public string SpawnId => _spawnId;
        public string ArchetypeId => _archetypeId;
        public string PrefabLocation => _prefabLocation;
        public Vector3 Position => _position;
        public Quaternion Rotation => Quaternion.Euler(_eulerAngles);

        /// <summary>由 Luban 行创建运行时刷怪点，避免运行时修改场景资产。</summary>
        public static EnemySpawnDefinition CreateRuntime(
            string spawnId,
            string archetypeId,
            string prefabLocation,
            Vector3 position,
            float rotationY)
        {
            return new EnemySpawnDefinition
            {
                _spawnId = string.IsNullOrWhiteSpace(spawnId) ? "enemy_01" : spawnId,
                _archetypeId = string.IsNullOrWhiteSpace(archetypeId) ? "enemy" : archetypeId,
                _prefabLocation = prefabLocation,
                _position = position,
                _eulerAngles = new Vector3(0f, rotationY, 0f)
            };
        }
    }
}
