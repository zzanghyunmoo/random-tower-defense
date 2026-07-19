#nullable enable

using System;
using System.Linq;
using NUnit.Framework;
using RandomTowerDefense.Core.Combat;
using RandomTowerDefense.Core.Enemies;
using RandomTowerDefense.Core.Towers;
using RandomTowerDefense.Core.Waves;

namespace RandomTowerDefense.Tests.EditMode.Core
{
    public sealed class GameSessionCoreTests
    {
        [Test]
        public void Advance_ResolvesOrderedProjectilesAndCreditsOneKillReward()
        {
            GameSessionDefinition definition = CreateDefinition(
                startingHealth: 3,
                startingCurrency: 20,
                summonCost: 10,
                slotCount: 2,
                enemyMoveSpeed: 0f,
                endpointDamage: 1,
                killReward: 4,
                towerDamage: 5f);
            var session = new GameSession(definition, seed: 17);
            Assert.That(session.TrySummonTower().Succeeded, Is.True);
            Assert.That(session.TrySummonTower().Succeeded, Is.True);
            session.Advance(0f);

            GameSessionTickResult combat = session.Advance(0.1f);
            GameSessionTickResult duplicate = session.Advance(1f);

            Assert.That(combat.FiredProjectiles, Has.Count.EqualTo(2));
            Assert.That(combat.ProjectileResults, Has.Count.EqualTo(2));
            Assert.That(combat.ProjectileResults[0].Status, Is.EqualTo(ProjectileStatus.Hit));
            Assert.That(combat.ProjectileResults[0].Damage.Killed, Is.True);
            Assert.That(combat.ProjectileResults[1].Status, Is.EqualTo(ProjectileStatus.TargetLost));
            Assert.That(combat.DeadEnemyCleanup.KillReward, Is.EqualTo(4));
            Assert.That(session.Economy.Balance, Is.EqualTo(4));
            Assert.That(session.Status, Is.EqualTo(GameSessionStatus.Victory));
            Assert.That(combat.StatusChanged, Is.True);
            Assert.That(duplicate.DeadEnemyCleanup.KillReward, Is.Zero);
            Assert.That(duplicate.FiredProjectiles, Is.Empty);
        }

        [Test]
        public void EndpointPhase_RemovesTargetBeforeAttackAndDoesNotGrantReward()
        {
            GameSessionDefinition definition = CreateDefinition(
                startingHealth: 2,
                startingCurrency: 10,
                summonCost: 10,
                slotCount: 1,
                enemyMoveSpeed: 10f,
                endpointDamage: 1,
                killReward: 7,
                towerDamage: 100f,
                pathLength: 1f);
            var session = new GameSession(definition, seed: 1);
            session.TrySummonTower();
            session.Advance(0f);

            GameSessionTickResult result = session.Advance(0.1f);

            Assert.That(result.Movement.PlayerDamageApplied, Is.EqualTo(1));
            Assert.That(result.Movement.RemovedEnemyOrders, Is.EqualTo(new[] { 0L }));
            Assert.That(result.FiredProjectiles, Is.Empty);
            Assert.That(result.DeadEnemyCleanup.KillReward, Is.Zero);
            Assert.That(session.Economy.Balance, Is.Zero);
            Assert.That(session.CurrentHealth, Is.EqualTo(1));
            Assert.That(session.Status, Is.EqualTo(GameSessionStatus.Victory));
        }

        [Test]
        public void DefeatStopsRemainingPhasesAndRejectsSummon()
        {
            GameSessionDefinition definition = CreateDefinition(
                startingHealth: 1,
                startingCurrency: 20,
                summonCost: 10,
                slotCount: 2,
                enemyMoveSpeed: 10f,
                endpointDamage: 1,
                killReward: 3,
                towerDamage: 10f,
                pathLength: 1f);
            var session = new GameSession(definition, seed: 3);
            session.Advance(0f);

            GameSessionTickResult defeat = session.Advance(0.1f);
            int balanceBeforeRejectedSummon = session.Economy.Balance;
            TowerSummonResult rejected = session.TrySummonTower();
            GameSessionTickResult duplicate = session.Advance(100f);

            Assert.That(defeat.Movement.DefeatedThisPhase, Is.True);
            Assert.That(defeat.SpawnPhase, Is.Null);
            Assert.That(defeat.FiredProjectiles, Is.Empty);
            Assert.That(session.Status, Is.EqualTo(GameSessionStatus.Defeat));
            Assert.That(rejected.Failure, Is.EqualTo(TowerSummonFailure.SessionClosed));
            Assert.That(session.Economy.Balance, Is.EqualTo(balanceBeforeRejectedSummon));
            Assert.That(duplicate.StatusChanged, Is.False);
            Assert.That(duplicate.ProjectileResults, Is.Empty);
        }

        [Test]
        public void Restart_ClearsAllRuntimeStateAndReplaysSeededSummons()
        {
            GameSessionDefinition definition = CreateDefinition(
                startingHealth: 5,
                startingCurrency: 20,
                summonCost: 10,
                slotCount: 2,
                enemyMoveSpeed: 0f,
                endpointDamage: 1,
                killReward: 1,
                towerDamage: 1f,
                towerKinds: 2,
                projectileSpeed: 0.1f,
                slotPositionOffset: 5f);
            var session = new GameSession(definition, seed: 918);

            string[] firstSummons = SummonTwice(session);
            session.Advance(0f);
            session.Advance(0.1f);
            Assert.That(session.ActiveEnemies, Is.Not.Empty);
            Assert.That(session.ActiveProjectiles, Is.Not.Empty);

            session.Restart();
            string[] restartedSummons = SummonTwice(session);
            GameSessionTickResult restartedFirstTick = session.Advance(0f);

            Assert.That(restartedSummons, Is.EqualTo(firstSummons));
            Assert.That(session.Status, Is.EqualTo(GameSessionStatus.Running));
            Assert.That(session.CurrentHealth, Is.EqualTo(5));
            Assert.That(session.Economy.Balance, Is.Zero);
            Assert.That(session.ActiveProjectiles, Is.Empty);
            Assert.That(restartedFirstTick.SpawnPhase!.SpawnedEnemies[0].SpawnOrder, Is.EqualTo(0));
        }

        [Test]
        public void CanSummon_TracksCurrencySlotsPoolAndSessionStatus()
        {
            GameSessionDefinition definition = CreateDefinition(
                startingHealth: 1,
                startingCurrency: 10,
                summonCost: 10,
                slotCount: 1,
                enemyMoveSpeed: 10f,
                endpointDamage: 1,
                killReward: 1,
                towerDamage: 1f,
                pathLength: 1f);
            var session = new GameSession(definition, seed: 1);

            Assert.That(session.CanSummon, Is.True);
            session.TrySummonTower();
            Assert.That(session.CanSummon, Is.False);
            session.Advance(0f);
            session.Advance(0.1f);
            Assert.That(session.Status, Is.EqualTo(GameSessionStatus.Defeat));
            Assert.That(session.CanSummon, Is.False);
        }

        [Test]
        public void InvalidSessionDefinitions_FailClearly()
        {
            GameSessionDefinition valid = CreateDefinition(
                startingHealth: 1,
                startingCurrency: 0,
                summonCost: 0,
                slotCount: 0,
                enemyMoveSpeed: 0f,
                endpointDamage: 1,
                killReward: 1,
                towerDamage: 1f);

            Assert.Throws<ArgumentException>(() => new GameSessionDefinition(
                "Stage Bad",
                valid.EnemyPath,
                1,
                0,
                0,
                valid.Waves,
                valid.TowerSlots,
                valid.SummonPool));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameSessionDefinition(
                "stage_test",
                valid.EnemyPath,
                0,
                0,
                0,
                valid.Waves,
                valid.TowerSlots,
                valid.SummonPool));
            Assert.Throws<ArgumentException>(() => new GameSessionDefinition(
                "stage_test",
                valid.EnemyPath,
                1,
                0,
                0,
                Array.Empty<WaveDefinition>(),
                valid.TowerSlots,
                valid.SummonPool));
            Assert.Throws<ArgumentException>(() => new GameSessionDefinition(
                "stage_test",
                valid.EnemyPath,
                1,
                int.MaxValue,
                0,
                valid.Waves,
                valid.TowerSlots,
                valid.SummonPool));

            var session = new GameSession(valid, seed: 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => session.Advance(float.NaN));
        }

        private static string[] SummonTwice(GameSession session)
        {
            return Enumerable.Range(0, 2)
                .Select(_ => session.TrySummonTower())
                .Select(result => $"{result.SlotId}:{result.Tower!.Definition.Id}")
                .ToArray();
        }

        private static GameSessionDefinition CreateDefinition(
            int startingHealth,
            int startingCurrency,
            int summonCost,
            int slotCount,
            float enemyMoveSpeed,
            int endpointDamage,
            int killReward,
            float towerDamage,
            float pathLength = 10f,
            int towerKinds = 1,
            float projectileSpeed = 100f,
            float slotPositionOffset = 0f)
        {
            var path = new Path2D(new[]
            {
                new Point2(0f, 0f),
                new Point2(pathLength, 0f)
            });
            var enemy = new EnemyDefinition(
                "enemy_session_test",
                maxHealth: 5f,
                enemyMoveSpeed,
                endpointDamage,
                killReward);
            var wave = new WaveDefinition("wave_session_01", enemy, enemyCount: 1, spawnIntervalSeconds: 1f);

            var slots = Enumerable.Range(0, slotCount)
                .Select(index => new TowerSlotDefinition(
                    $"slot_{index}",
                    new Point2(slotPositionOffset + index, 0f),
                    placementOrder: index))
                .ToArray();
            var pool = Enumerable.Range(0, towerKinds)
                .Select(index => new TowerDefinition(
                    $"tower_session_{index}",
                    range: 20f,
                    attackIntervalSeconds: 1f,
                    projectileSpeed,
                    projectileDamage: towerDamage))
                .Select(tower => new TowerSummonPoolEntry(tower, weight: 1))
                .ToArray();

            return new GameSessionDefinition(
                "stage_session_test",
                path,
                startingHealth,
                startingCurrency,
                summonCost,
                new[] { wave },
                slots,
                pool);
        }
    }
}
