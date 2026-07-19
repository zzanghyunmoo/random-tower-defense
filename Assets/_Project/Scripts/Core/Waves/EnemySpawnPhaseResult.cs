#nullable enable

using System;
using System.Collections.Generic;
using RandomTowerDefense.Core.Enemies;

namespace RandomTowerDefense.Core.Waves
{
    public sealed class EnemySpawnPhaseResult
    {
        internal EnemySpawnPhaseResult(
            WaveAdvanceResult? waveResult,
            IReadOnlyList<EnemyState> spawnedEnemies,
            bool victoryThisPhase)
        {
            if (spawnedEnemies == null)
            {
                throw new ArgumentNullException(nameof(spawnedEnemies));
            }

            var enemies = new EnemyState[spawnedEnemies.Count];
            for (int index = 0; index < spawnedEnemies.Count; index++)
            {
                enemies[index] = spawnedEnemies[index]
                    ?? throw new ArgumentException("Spawned enemies must not contain null.", nameof(spawnedEnemies));
            }

            WaveResult = waveResult;
            SpawnedEnemies = Array.AsReadOnly(enemies);
            VictoryThisPhase = victoryThisPhase;
        }

        public WaveAdvanceResult? WaveResult { get; }

        public IReadOnlyList<EnemyState> SpawnedEnemies { get; }

        public bool VictoryThisPhase { get; }
    }
}
