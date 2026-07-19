#nullable enable

using System;
using NUnit.Framework;
using RandomTowerDefense.Core.Combat;
using RandomTowerDefense.Core.Enemies;
using RandomTowerDefense.Core.Waves;

namespace RandomTowerDefense.Tests.EditMode.Core
{
    public sealed class EnemyWaveSessionCoreTests
    {
        [Test]
        public void EndpointDamage_RemovesEnemyOnceAndCompletesWithVictory()
        {
            EnemyWaveSession session = CreateSession(startingHealth: 3, endpointDamage: 2);
            EnemySpawnPhaseResult spawn = session.AdvanceWaves(0f);

            EnemyRemovalResult movement = session.AdvanceEnemies(1f);
            EnemyRemovalResult duplicate = session.AdvanceEnemies(10f);
            EnemySpawnPhaseResult completed = session.AdvanceWaves(0f);

            Assert.That(spawn.SpawnedEnemies, Has.Count.EqualTo(1));
            Assert.That(movement.RemovedEnemyOrders, Is.EqualTo(new[] { 0L }));
            Assert.That(movement.PlayerDamageApplied, Is.EqualTo(2));
            Assert.That(movement.KillReward, Is.Zero);
            Assert.That(session.CurrentHealth, Is.EqualTo(1));
            Assert.That(duplicate.PlayerDamageApplied, Is.Zero);
            Assert.That(completed.VictoryThisPhase, Is.True);
            Assert.That(session.Status, Is.EqualTo(EnemyWaveSessionStatus.Victory));
            Assert.That(session.OutstandingEnemyCount, Is.Zero);
        }

        [Test]
        public void MultipleEndpoints_ClampHealthAndDefeatStopsWaveProgression()
        {
            EnemyWaveSession session = CreateSession(
                startingHealth: 1,
                endpointDamage: 1,
                enemyCount: 2,
                spawnInterval: 1f);
            session.AdvanceWaves(1f);

            EnemyRemovalResult movement = session.AdvanceEnemies(1f);
            EnemySpawnPhaseResult blocked = session.AdvanceWaves(100f);

            Assert.That(movement.RemovedEnemyOrders, Is.EqualTo(new[] { 0L, 1L }));
            Assert.That(movement.PlayerDamageApplied, Is.EqualTo(1));
            Assert.That(movement.DefeatedThisPhase, Is.True);
            Assert.That(session.CurrentHealth, Is.Zero);
            Assert.That(session.Status, Is.EqualTo(EnemyWaveSessionStatus.Defeat));
            Assert.That(session.ActiveEnemies, Is.Empty);
            Assert.That(blocked.WaveResult, Is.Null);
            Assert.That(blocked.SpawnedEnemies, Is.Empty);
        }

        [Test]
        public void RemoveDeadEnemies_ReturnsEachKillRewardOnce()
        {
            EnemyWaveSession session = CreateSession(
                startingHealth: 5,
                endpointDamage: 1,
                enemyCount: 2,
                spawnInterval: 1f,
                killReward: 3);
            EnemySpawnPhaseResult spawn = session.AdvanceWaves(1f);
            foreach (EnemyState enemy in spawn.SpawnedEnemies)
            {
                enemy.ApplyDamage(100f);
            }

            EnemyRemovalResult cleanup = session.RemoveDeadEnemies();
            EnemyRemovalResult duplicate = session.RemoveDeadEnemies();
            EnemySpawnPhaseResult completed = session.AdvanceWaves(0f);

            Assert.That(cleanup.RemovedEnemyOrders, Is.EqualTo(new[] { 0L, 1L }));
            Assert.That(cleanup.KillReward, Is.EqualTo(6));
            Assert.That(cleanup.PlayerDamageApplied, Is.Zero);
            Assert.That(duplicate.KillReward, Is.Zero);
            Assert.That(completed.VictoryThisPhase, Is.True);
        }

        [Test]
        public void AdvanceWaves_CreatesEnemiesWithConfiguredPathAndStableOrder()
        {
            EnemyWaveSession session = CreateSession(
                startingHealth: 5,
                endpointDamage: 1,
                enemyCount: 2,
                spawnInterval: 0.5f,
                initialSpawnOrder: 7);

            EnemySpawnPhaseResult result = session.AdvanceWaves(0.5f);

            Assert.That(result.SpawnedEnemies, Has.Count.EqualTo(2));
            Assert.That(result.SpawnedEnemies[0].SpawnOrder, Is.EqualTo(7));
            Assert.That(result.SpawnedEnemies[1].SpawnOrder, Is.EqualTo(8));
            Assert.That(result.SpawnedEnemies[0].Path, Is.SameAs(result.SpawnedEnemies[1].Path));
            Assert.That(result.SpawnedEnemies[0].Position, Is.EqualTo(new Point2(0f, 0f)));
            Assert.That(session.ActiveEnemies, Is.EqualTo(result.SpawnedEnemies));
        }

        [Test]
        public void Restart_DiscardsRuntimeStateAndReplaysInitialSpawnOrder()
        {
            EnemyWaveSession session = CreateSession(startingHealth: 3, endpointDamage: 3);
            EnemySpawnPhaseResult beforeRestart = session.AdvanceWaves(0f);
            session.AdvanceEnemies(1f);
            Assert.That(session.Status, Is.EqualTo(EnemyWaveSessionStatus.Defeat));

            session.Restart();
            EnemySpawnPhaseResult afterRestart = session.AdvanceWaves(0f);

            Assert.That(session.Status, Is.EqualTo(EnemyWaveSessionStatus.Running));
            Assert.That(session.CurrentHealth, Is.EqualTo(3));
            Assert.That(session.ActiveEnemies, Has.Count.EqualTo(1));
            Assert.That(afterRestart.SpawnedEnemies[0], Is.Not.SameAs(beforeRestart.SpawnedEnemies[0]));
            Assert.That(afterRestart.SpawnedEnemies[0].SpawnOrder, Is.EqualTo(0));
        }

        [Test]
        public void InvalidSessionInputs_FailClearly()
        {
            Path2D path = CreatePath();
            WaveDefinition wave = CreateWave("wave_01", 1, 1f, 1, 1);

            Assert.Throws<ArgumentNullException>(() =>
                new EnemyWaveSession(null!, 1, new[] { wave }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new EnemyWaveSession(path, 0, new[] { wave }));
            Assert.Throws<ArgumentException>(() =>
                new EnemyWaveSession(path, 1, Array.Empty<WaveDefinition>()));

            EnemyWaveSession session = new EnemyWaveSession(path, 1, new[] { wave });
            Assert.Throws<ArgumentOutOfRangeException>(() => session.AdvanceEnemies(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => session.AdvanceWaves(-1f));
        }

        private static EnemyWaveSession CreateSession(
            int startingHealth,
            int endpointDamage,
            int enemyCount = 1,
            float spawnInterval = 1f,
            int killReward = 1,
            long initialSpawnOrder = 0)
        {
            return new EnemyWaveSession(
                CreatePath(),
                startingHealth,
                new[]
                {
                    CreateWave(
                        "wave_01",
                        enemyCount,
                        spawnInterval,
                        endpointDamage,
                        killReward)
                },
                initialSpawnOrder);
        }

        private static WaveDefinition CreateWave(
            string id,
            int enemyCount,
            float spawnInterval,
            int endpointDamage,
            int killReward)
        {
            var enemy = new EnemyDefinition(
                $"enemy_{id}",
                maxHealth: 5f,
                moveSpeed: 1f,
                endpointDamage,
                killReward);
            return new WaveDefinition(id, enemy, enemyCount, spawnInterval);
        }

        private static Path2D CreatePath()
        {
            return new Path2D(new[]
            {
                new Point2(0f, 0f),
                new Point2(1f, 0f)
            });
        }
    }
}
