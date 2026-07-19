#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RandomTowerDefense.Core.Combat;
using RandomTowerDefense.Core.Enemies;

namespace RandomTowerDefense.Core.Waves
{
    public sealed class EnemyWaveSession
    {
        private readonly Path2D _path;
        private readonly int _startingHealth;
        private readonly WaveDefinition[] _waveDefinitions;
        private readonly long _initialSpawnOrder;
        private readonly List<EnemyState> _activeEnemies = new List<EnemyState>();
        private readonly ReadOnlyCollection<EnemyState> _activeEnemiesView;

        private WaveSystem _waveSystem = null!;

        public EnemyWaveSession(
            Path2D path,
            int startingHealth,
            IReadOnlyList<WaveDefinition> waves,
            long initialSpawnOrder = 0)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
            _startingHealth = Guard.Positive(startingHealth, nameof(startingHealth));
            if (waves == null)
            {
                throw new ArgumentNullException(nameof(waves));
            }

            _waveDefinitions = new WaveDefinition[waves.Count];
            for (int index = 0; index < waves.Count; index++)
            {
                _waveDefinitions[index] = waves[index]
                    ?? throw new ArgumentException("Wave collection must not contain null.", nameof(waves));
            }

            _initialSpawnOrder = Guard.NonNegative(initialSpawnOrder, nameof(initialSpawnOrder));
            _activeEnemiesView = _activeEnemies.AsReadOnly();
            Restart();
        }

        public int StartingHealth => _startingHealth;

        public int CurrentHealth { get; private set; }

        public EnemyWaveSessionStatus Status { get; private set; }

        public IReadOnlyList<EnemyState> ActiveEnemies => _activeEnemiesView;

        public int CurrentWaveIndex => _waveSystem.CurrentWaveIndex;

        public int WaveCount => _waveSystem.WaveCount;

        public int OutstandingEnemyCount => _waveSystem.OutstandingEnemyCount;

        public EnemyRemovalResult AdvanceEnemies(float deltaSeconds)
        {
            Guard.NonNegative(deltaSeconds, nameof(deltaSeconds));
            if (Status != EnemyWaveSessionStatus.Running)
            {
                return EmptyRemovalResult();
            }

            var removedOrders = new List<long>();
            int totalDamageApplied = 0;
            foreach (EnemyState enemy in _activeEnemies)
            {
                EnemyAdvanceResult advanceResult = enemy.Advance(deltaSeconds);
                if (!advanceResult.ReachedEndpoint)
                {
                    continue;
                }

                int appliedDamage = Math.Min(CurrentHealth, advanceResult.EndpointDamage);
                CurrentHealth -= appliedDamage;
                totalDamageApplied += appliedDamage;
                removedOrders.Add(enemy.SpawnOrder);
            }

            foreach (long spawnOrder in removedOrders)
            {
                _waveSystem.MarkEnemyRemoved(spawnOrder);
            }

            _activeEnemies.RemoveAll(enemy => enemy.Status == EnemyStatus.ReachedEndpoint);

            bool defeatedThisPhase = CurrentHealth == 0;
            if (defeatedThisPhase)
            {
                Status = EnemyWaveSessionStatus.Defeat;
            }

            return new EnemyRemovalResult(
                removedOrders,
                totalDamageApplied,
                killReward: 0,
                defeatedThisPhase);
        }

        public EnemyRemovalResult RemoveDeadEnemies()
        {
            if (Status != EnemyWaveSessionStatus.Running)
            {
                return EmptyRemovalResult();
            }

            var removedOrders = new List<long>();
            int totalKillReward = 0;
            foreach (EnemyState enemy in _activeEnemies)
            {
                if (enemy.Status != EnemyStatus.Dead)
                {
                    continue;
                }

                try
                {
                    totalKillReward = checked(totalKillReward + enemy.Definition.KillReward);
                }
                catch (OverflowException exception)
                {
                    throw new InvalidOperationException("Kill reward total exceeded Int32.MaxValue.", exception);
                }

                removedOrders.Add(enemy.SpawnOrder);
            }

            foreach (long spawnOrder in removedOrders)
            {
                _waveSystem.MarkEnemyRemoved(spawnOrder);
            }

            _activeEnemies.RemoveAll(enemy => enemy.Status == EnemyStatus.Dead);
            return new EnemyRemovalResult(
                removedOrders,
                playerDamageApplied: 0,
                totalKillReward,
                defeatedThisPhase: false);
        }

        public EnemySpawnPhaseResult AdvanceWaves(float deltaSeconds)
        {
            Guard.NonNegative(deltaSeconds, nameof(deltaSeconds));
            if (Status != EnemyWaveSessionStatus.Running)
            {
                return new EnemySpawnPhaseResult(null, Array.Empty<EnemyState>(), false);
            }

            WaveAdvanceResult waveResult = _waveSystem.Advance(deltaSeconds);
            var spawnedEnemies = new List<EnemyState>(waveResult.SpawnRequests.Count);
            foreach (EnemySpawnRequest request in waveResult.SpawnRequests)
            {
                var enemy = new EnemyState(request.Enemy, _path, request.SpawnOrder);
                _activeEnemies.Add(enemy);
                spawnedEnemies.Add(enemy);
            }

            bool victoryThisPhase = waveResult.StageCompletedThisAdvance;
            if (victoryThisPhase)
            {
                Status = EnemyWaveSessionStatus.Victory;
            }

            return new EnemySpawnPhaseResult(waveResult, spawnedEnemies, victoryThisPhase);
        }

        public void Restart()
        {
            _activeEnemies.Clear();
            _waveSystem = new WaveSystem(_waveDefinitions, _initialSpawnOrder);
            CurrentHealth = _startingHealth;
            Status = EnemyWaveSessionStatus.Running;
        }

        private static EnemyRemovalResult EmptyRemovalResult()
        {
            return new EnemyRemovalResult(
                Array.Empty<long>(),
                playerDamageApplied: 0,
                killReward: 0,
                defeatedThisPhase: false);
        }
    }
}
