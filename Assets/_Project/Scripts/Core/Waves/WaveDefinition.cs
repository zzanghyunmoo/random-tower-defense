using System;
using RandomTowerDefense.Core.Combat;
using RandomTowerDefense.Core.Enemies;

namespace RandomTowerDefense.Core.Waves
{
    public sealed class WaveDefinition
    {
        public WaveDefinition(
            string id,
            EnemyDefinition enemy,
            int enemyCount,
            float spawnIntervalSeconds)
        {
            Id = Guard.DefinitionId(id, nameof(id));
            Enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
            EnemyCount = Guard.Positive(enemyCount, nameof(enemyCount));
            SpawnIntervalSeconds = Guard.Positive(spawnIntervalSeconds, nameof(spawnIntervalSeconds));
        }

        public string Id { get; }

        public EnemyDefinition Enemy { get; }

        public int EnemyCount { get; }

        public float SpawnIntervalSeconds { get; }
    }
}
