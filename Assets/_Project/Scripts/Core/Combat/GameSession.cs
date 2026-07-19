#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RandomTowerDefense.Core.Economy;
using RandomTowerDefense.Core.Enemies;
using RandomTowerDefense.Core.Random;
using RandomTowerDefense.Core.Towers;
using RandomTowerDefense.Core.Waves;

namespace RandomTowerDefense.Core.Combat
{
    public sealed class GameSession
    {
        private readonly ulong _seed;
        private readonly List<ProjectileState> _activeProjectiles = new List<ProjectileState>();
        private readonly ReadOnlyCollection<ProjectileState> _activeProjectilesView;

        private EnemyWaveSession _enemyWaves = null!;
        private EconomyState _economy = null!;
        private TowerGrid _towerGrid = null!;
        private TowerSummonSystem _summonSystem = null!;
        private TowerAttackSystem _towerAttackSystem = null!;

        public GameSession(GameSessionDefinition definition, ulong seed)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _seed = seed;
            _activeProjectilesView = _activeProjectiles.AsReadOnly();
            Restart();
        }

        public GameSessionDefinition Definition { get; }

        public ulong Seed => _seed;

        public GameSessionStatus Status => ToGameStatus(_enemyWaves.Status);

        public int CurrentHealth => _enemyWaves.CurrentHealth;

        public int CurrentWaveIndex => _enemyWaves.CurrentWaveIndex;

        public IReadOnlyList<EnemyState> ActiveEnemies => _enemyWaves.ActiveEnemies;

        public IReadOnlyList<ProjectileState> ActiveProjectiles => _activeProjectilesView;

        public EconomyState Economy => _economy;

        public TowerGrid TowerGrid => _towerGrid;

        public bool CanSummon => Status == GameSessionStatus.Running
            && _economy.CanAfford(Definition.SummonCost)
            && _towerGrid.EmptySlotCount > 0
            && Definition.SummonPool.Count > 0;

        public TowerSummonResult TrySummonTower()
        {
            return _summonSystem.TrySummon(
                Status == GameSessionStatus.Running,
                _economy,
                _towerGrid);
        }

        public GameSessionTickResult Advance(float deltaSeconds)
        {
            Guard.NonNegative(deltaSeconds, nameof(deltaSeconds));
            GameSessionStatus statusBefore = Status;
            if (statusBefore != GameSessionStatus.Running)
            {
                return NoChange(statusBefore);
            }

            EnemyRemovalResult movement = _enemyWaves.AdvanceEnemies(deltaSeconds);
            if (Status == GameSessionStatus.Defeat)
            {
                return new GameSessionTickResult(
                    movement,
                    Array.Empty<ProjectileState>(),
                    Array.Empty<ProjectileAdvanceResult>(),
                    EmptyRemovalResult(),
                    spawnPhase: null,
                    statusBefore,
                    Status);
            }

            IReadOnlyList<ProjectileState> firedProjectiles = _towerAttackSystem.Advance(
                deltaSeconds,
                _towerGrid.GetOccupiedTowers(),
                _enemyWaves.ActiveEnemies);
            foreach (ProjectileState projectile in firedProjectiles)
            {
                _activeProjectiles.Add(projectile);
            }

            IReadOnlyList<ProjectileAdvanceResult> projectileResults = ProjectileResolver.Advance(
                deltaSeconds,
                _activeProjectiles);
            _activeProjectiles.RemoveAll(projectile => !projectile.IsFlying);

            EnemyRemovalResult deadEnemyCleanup = _enemyWaves.RemoveDeadEnemies();
            if (deadEnemyCleanup.KillReward > 0)
            {
                _economy.Credit(deadEnemyCleanup.KillReward);
            }

            EnemySpawnPhaseResult spawnPhase = _enemyWaves.AdvanceWaves(deltaSeconds);
            return new GameSessionTickResult(
                movement,
                firedProjectiles,
                projectileResults,
                deadEnemyCleanup,
                spawnPhase,
                statusBefore,
                Status);
        }

        public void Restart()
        {
            var enemyWaves = new EnemyWaveSession(
                Definition.EnemyPath,
                Definition.StartingHealth,
                Definition.Waves);
            var economy = new EconomyState(Definition.StartingCurrency);
            var towerGrid = new TowerGrid(Definition.TowerSlots);
            var summonSystem = new TowerSummonSystem(
                new SeededRandomSource(_seed),
                Definition.SummonCost,
                Definition.SummonPool);
            var towerAttackSystem = new TowerAttackSystem();

            _activeProjectiles.Clear();
            _enemyWaves = enemyWaves;
            _economy = economy;
            _towerGrid = towerGrid;
            _summonSystem = summonSystem;
            _towerAttackSystem = towerAttackSystem;
        }

        private GameSessionTickResult NoChange(GameSessionStatus status)
        {
            return new GameSessionTickResult(
                EmptyRemovalResult(),
                Array.Empty<ProjectileState>(),
                Array.Empty<ProjectileAdvanceResult>(),
                EmptyRemovalResult(),
                spawnPhase: null,
                status,
                status);
        }

        private static EnemyRemovalResult EmptyRemovalResult()
        {
            return new EnemyRemovalResult(
                Array.Empty<long>(),
                playerDamageApplied: 0,
                killReward: 0,
                defeatedThisPhase: false);
        }

        private static GameSessionStatus ToGameStatus(EnemyWaveSessionStatus status)
        {
            switch (status)
            {
                case EnemyWaveSessionStatus.Running:
                    return GameSessionStatus.Running;
                case EnemyWaveSessionStatus.Victory:
                    return GameSessionStatus.Victory;
                case EnemyWaveSessionStatus.Defeat:
                    return GameSessionStatus.Defeat;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown enemy wave session status.");
            }
        }
    }
}
