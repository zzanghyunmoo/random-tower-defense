using System;
using RandomTowerDefense.Core.Enemies;

namespace RandomTowerDefense.Core.Waves
{
    public sealed class EnemySpawnRequest
    {
        internal EnemySpawnRequest(
            int waveIndex,
            string waveId,
            int spawnIndex,
            long spawnOrder,
            EnemyDefinition enemy)
        {
            WaveIndex = waveIndex;
            WaveId = waveId ?? throw new ArgumentNullException(nameof(waveId));
            SpawnIndex = spawnIndex;
            SpawnOrder = spawnOrder;
            Enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
        }

        public int WaveIndex { get; }

        public string WaveId { get; }

        public int SpawnIndex { get; }

        public long SpawnOrder { get; }

        public EnemyDefinition Enemy { get; }
    }
}
