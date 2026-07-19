#nullable enable

using System;
using System.Collections.Generic;
using RandomTowerDefense.Core.Combat;

namespace RandomTowerDefense.Core.Waves
{
    public sealed class WaveSystem
    {
        private readonly WaveDefinition[] _waves;
        private readonly HashSet<long> _activeSpawnOrders = new HashSet<long>();

        private int _spawnedInCurrentWave;
        private double _timeUntilNextSpawn;
        private long _nextSpawnOrder;
        private bool _waveStartPending = true;

        public WaveSystem(IReadOnlyList<WaveDefinition> waves, long initialSpawnOrder = 0)
        {
            if (waves == null)
            {
                throw new ArgumentNullException(nameof(waves));
            }

            if (waves.Count == 0)
            {
                throw new ArgumentException("At least one wave is required.", nameof(waves));
            }

            _nextSpawnOrder = Guard.NonNegative(initialSpawnOrder, nameof(initialSpawnOrder));
            _waves = new WaveDefinition[waves.Count];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            long totalEnemyCount = 0;
            for (int index = 0; index < waves.Count; index++)
            {
                WaveDefinition wave = waves[index]
                    ?? throw new ArgumentException("Wave collection must not contain null.", nameof(waves));
                if (!ids.Add(wave.Id))
                {
                    throw new ArgumentException($"Duplicate wave ID '{wave.Id}'.", nameof(waves));
                }

                totalEnemyCount = checked(totalEnemyCount + wave.EnemyCount);
                _waves[index] = wave;
            }

            if (initialSpawnOrder > long.MaxValue - totalEnemyCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialSpawnOrder),
                    initialSpawnOrder,
                    "Spawn order range is too small for all configured enemies.");
            }
        }

        public int WaveCount => _waves.Length;

        public int CurrentWaveIndex { get; private set; }

        public int SpawnedInCurrentWave => _spawnedInCurrentWave;

        public int OutstandingEnemyCount => _activeSpawnOrders.Count;

        public long NextSpawnOrder => _nextSpawnOrder;

        public WaveSystemStatus Status { get; private set; } = WaveSystemStatus.Running;

        public WaveAdvanceResult Advance(float deltaSeconds)
        {
            Guard.NonNegative(deltaSeconds, nameof(deltaSeconds));
            if (Status == WaveSystemStatus.Completed)
            {
                return CreateResult(Array.Empty<EnemySpawnRequest>(), null, null, false);
            }

            int? completedWaveIndex = null;
            if (CurrentWaveIsClear())
            {
                completedWaveIndex = CurrentWaveIndex;
                if (CurrentWaveIndex == _waves.Length - 1)
                {
                    Status = WaveSystemStatus.Completed;
                    return CreateResult(
                        Array.Empty<EnemySpawnRequest>(),
                        null,
                        completedWaveIndex,
                        true);
                }

                CurrentWaveIndex++;
                _spawnedInCurrentWave = 0;
                _timeUntilNextSpawn = 0d;
                _waveStartPending = true;
            }

            int? startedWaveIndex = null;
            if (_waveStartPending)
            {
                startedWaveIndex = CurrentWaveIndex;
                _waveStartPending = false;
            }

            var spawnRequests = new List<EnemySpawnRequest>();
            double remainingTime = deltaSeconds;
            WaveDefinition currentWave = _waves[CurrentWaveIndex];
            while (_spawnedInCurrentWave < currentWave.EnemyCount)
            {
                if (_timeUntilNextSpawn > remainingTime)
                {
                    _timeUntilNextSpawn -= remainingTime;
                    break;
                }

                remainingTime -= _timeUntilNextSpawn;
                int spawnIndex = _spawnedInCurrentWave;
                var request = new EnemySpawnRequest(
                    CurrentWaveIndex,
                    currentWave.Id,
                    spawnIndex,
                    _nextSpawnOrder,
                    currentWave.Enemy);
                spawnRequests.Add(request);
                _activeSpawnOrders.Add(_nextSpawnOrder);
                _nextSpawnOrder++;
                _spawnedInCurrentWave++;

                _timeUntilNextSpawn = _spawnedInCurrentWave < currentWave.EnemyCount
                    ? currentWave.SpawnIntervalSeconds
                    : 0d;
            }

            return CreateResult(spawnRequests, startedWaveIndex, completedWaveIndex, false);
        }

        public void MarkEnemyRemoved(long spawnOrder)
        {
            Guard.NonNegative(spawnOrder, nameof(spawnOrder));
            if (!_activeSpawnOrders.Remove(spawnOrder))
            {
                throw new InvalidOperationException(
                    $"Spawn order '{spawnOrder}' is not an active enemy in this wave system.");
            }
        }

        private bool CurrentWaveIsClear()
        {
            return _spawnedInCurrentWave == _waves[CurrentWaveIndex].EnemyCount
                && _activeSpawnOrders.Count == 0;
        }

        private static WaveAdvanceResult CreateResult(
            IReadOnlyList<EnemySpawnRequest> spawnRequests,
            int? startedWaveIndex,
            int? completedWaveIndex,
            bool stageCompletedThisAdvance)
        {
            return new WaveAdvanceResult(
                spawnRequests,
                startedWaveIndex,
                completedWaveIndex,
                stageCompletedThisAdvance);
        }
    }
}
