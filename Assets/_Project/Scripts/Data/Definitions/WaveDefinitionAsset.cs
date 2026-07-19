#nullable enable

using UnityEngine;

namespace RandomTowerDefense.Data.Definitions
{
    [CreateAssetMenu(menuName = "Random Tower Defense/Wave Definition", fileName = "WaveDefinition")]
    public sealed class WaveDefinitionAsset : DefinitionAsset
    {
        [SerializeField]
        private EnemyDefinitionAsset? _enemy;

        [SerializeField]
        private int _enemyCount;

        [SerializeField]
        private float _spawnIntervalSeconds;

        public EnemyDefinitionAsset? Enemy => _enemy;

        public int EnemyCount => _enemyCount;

        public float SpawnIntervalSeconds => _spawnIntervalSeconds;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string id,
            EnemyDefinitionAsset enemy,
            int enemyCount,
            float spawnIntervalSeconds)
        {
            SetIdForEditor(id);
            _enemy = enemy;
            _enemyCount = enemyCount;
            _spawnIntervalSeconds = spawnIntervalSeconds;
        }
#endif
    }
}
