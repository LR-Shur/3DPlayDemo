using System.Collections.Generic;
using UnityEngine;

namespace Train.GameFlow.Data
{
    /// <summary>
    /// 描述一个可游玩关卡的稳定标识、场景位置和敌人生成配置。
    /// </summary>
    [CreateAssetMenu(
        menuName = "Train/Game Flow/Level Definition",
        fileName = "LevelDefinition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _levelId = "combat_001";
        [SerializeField] private string _displayName = "第 1 训练区";

        [Header("Scene")]
        [SerializeField] private string _sceneLocation;
        [SerializeField, Min(0f)] private float _introSeconds = 1.2f;

        [Header("Rules")]
        [Tooltip("0 表示无限复活。")]
        [SerializeField, Min(0)] private int _maxPlayerDeaths;
        [SerializeField] private List<EnemySpawnDefinition> _enemySpawns = new();

        public string LevelId => _levelId;
        public string DisplayName => _displayName;
        public string SceneLocation => _sceneLocation;
        public float IntroSeconds => _introSeconds;
        public int MaxPlayerDeaths => _maxPlayerDeaths;
        public IReadOnlyList<EnemySpawnDefinition> EnemySpawns => _enemySpawns;

    }
}
