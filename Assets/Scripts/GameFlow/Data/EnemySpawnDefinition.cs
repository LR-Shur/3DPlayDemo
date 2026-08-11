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

    }
}
