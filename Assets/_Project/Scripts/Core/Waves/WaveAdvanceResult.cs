#nullable enable

using System;
using System.Collections.Generic;

namespace RandomTowerDefense.Core.Waves
{
    public sealed class WaveAdvanceResult
    {
        internal WaveAdvanceResult(
            IReadOnlyList<EnemySpawnRequest> spawnRequests,
            int? startedWaveIndex,
            int? completedWaveIndex,
            bool stageCompletedThisAdvance)
        {
            if (spawnRequests == null)
            {
                throw new ArgumentNullException(nameof(spawnRequests));
            }

            var requests = new EnemySpawnRequest[spawnRequests.Count];
            for (int index = 0; index < spawnRequests.Count; index++)
            {
                requests[index] = spawnRequests[index]
                    ?? throw new ArgumentException("Spawn requests must not contain null.", nameof(spawnRequests));
            }

            SpawnRequests = Array.AsReadOnly(requests);
            StartedWaveIndex = startedWaveIndex;
            CompletedWaveIndex = completedWaveIndex;
            StageCompletedThisAdvance = stageCompletedThisAdvance;
        }

        public IReadOnlyList<EnemySpawnRequest> SpawnRequests { get; }

        public int? StartedWaveIndex { get; }

        public int? CompletedWaveIndex { get; }

        public bool StageCompletedThisAdvance { get; }

        public bool WaveStartedThisAdvance => StartedWaveIndex.HasValue;

        public bool WaveCompletedThisAdvance => CompletedWaveIndex.HasValue;
    }
}
