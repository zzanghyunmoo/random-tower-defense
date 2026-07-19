#nullable enable

using System;
using System.Collections.Generic;
using NUnit.Framework;
using RandomTowerDefense.Core.Enemies;
using RandomTowerDefense.Core.Waves;

namespace RandomTowerDefense.Tests.EditMode.Core
{
    public sealed class WaveCoreTests
    {
        [Test]
        public void Advance_SpawnsImmediatelyAndAtExactIntervalBoundaries()
        {
            var system = new WaveSystem(new[] { CreateWave("wave_01", enemyCount: 3, interval: 1f) });

            WaveAdvanceResult initial = system.Advance(0f);
            WaveAdvanceResult beforeBoundary = system.Advance(0.5f);
            WaveAdvanceResult atBoundary = system.Advance(0.5f);
            WaveAdvanceResult finalSpawn = system.Advance(1f);

            Assert.That(initial.WaveStartedThisAdvance, Is.True);
            Assert.That(initial.StartedWaveIndex, Is.EqualTo(0));
            Assert.That(initial.SpawnRequests, Has.Count.EqualTo(1));
            Assert.That(beforeBoundary.SpawnRequests, Is.Empty);
            Assert.That(atBoundary.SpawnRequests, Has.Count.EqualTo(1));
            Assert.That(finalSpawn.SpawnRequests, Has.Count.EqualTo(1));
            Assert.That(initial.SpawnRequests[0].SpawnIndex, Is.EqualTo(0));
            Assert.That(atBoundary.SpawnRequests[0].SpawnIndex, Is.EqualTo(1));
            Assert.That(finalSpawn.SpawnRequests[0].SpawnIndex, Is.EqualTo(2));
            Assert.That(system.SpawnedInCurrentWave, Is.EqualTo(3));
            Assert.That(system.OutstandingEnemyCount, Is.EqualTo(3));
        }

        [Test]
        public void Advance_CatchesUpSpawnsInStableGlobalOrder()
        {
            var system = new WaveSystem(
                new[] { CreateWave("wave_01", enemyCount: 4, interval: 0.5f) },
                initialSpawnOrder: 10);

            WaveAdvanceResult result = system.Advance(1.2f);

            Assert.That(result.SpawnRequests, Has.Count.EqualTo(3));
            Assert.That(result.SpawnRequests[0].SpawnOrder, Is.EqualTo(10));
            Assert.That(result.SpawnRequests[1].SpawnOrder, Is.EqualTo(11));
            Assert.That(result.SpawnRequests[2].SpawnOrder, Is.EqualTo(12));
            Assert.That(result.SpawnRequests[2].WaveId, Is.EqualTo("wave_01"));
            Assert.That(system.NextSpawnOrder, Is.EqualTo(13));

            WaveAdvanceResult remaining = system.Advance(0.3f);
            Assert.That(remaining.SpawnRequests, Has.Count.EqualTo(1));
            Assert.That(remaining.SpawnRequests[0].SpawnOrder, Is.EqualTo(13));
        }

        [Test]
        public void ClearedWave_TransitionsAndFinalWaveCompletesExactlyOnce()
        {
            var system = new WaveSystem(new[]
            {
                CreateWave("wave_01", enemyCount: 1, interval: 1f),
                CreateWave("wave_02", enemyCount: 1, interval: 1f)
            });

            WaveAdvanceResult firstWave = system.Advance(0f);
            WaveAdvanceResult blockedByLivingEnemy = system.Advance(10f);
            system.MarkEnemyRemoved(firstWave.SpawnRequests[0].SpawnOrder);
            WaveAdvanceResult transition = system.Advance(0f);
            system.MarkEnemyRemoved(transition.SpawnRequests[0].SpawnOrder);
            WaveAdvanceResult completed = system.Advance(0f);
            WaveAdvanceResult duplicate = system.Advance(100f);

            Assert.That(blockedByLivingEnemy.WaveCompletedThisAdvance, Is.False);
            Assert.That(transition.CompletedWaveIndex, Is.EqualTo(0));
            Assert.That(transition.StartedWaveIndex, Is.EqualTo(1));
            Assert.That(transition.SpawnRequests, Has.Count.EqualTo(1));
            Assert.That(completed.CompletedWaveIndex, Is.EqualTo(1));
            Assert.That(completed.StageCompletedThisAdvance, Is.True);
            Assert.That(system.Status, Is.EqualTo(WaveSystemStatus.Completed));
            Assert.That(duplicate.StageCompletedThisAdvance, Is.False);
            Assert.That(duplicate.SpawnRequests, Is.Empty);
        }

        [Test]
        public void MarkEnemyRemoved_RejectsUnknownAndDuplicateOrders()
        {
            var system = new WaveSystem(new[] { CreateWave("wave_01", enemyCount: 1, interval: 1f) });
            WaveAdvanceResult spawn = system.Advance(0f);
            long spawnOrder = spawn.SpawnRequests[0].SpawnOrder;

            Assert.Throws<InvalidOperationException>(() => system.MarkEnemyRemoved(spawnOrder + 1));
            system.MarkEnemyRemoved(spawnOrder);
            Assert.Throws<InvalidOperationException>(() => system.MarkEnemyRemoved(spawnOrder));
            Assert.That(system.OutstandingEnemyCount, Is.Zero);
        }

        [Test]
        public void SameDefinitionsAndSteps_ReproduceSpawnSequence()
        {
            string[] first = RunSequence();
            string[] second = RunSequence();

            Assert.That(first, Is.EqualTo(second));
        }

        [Test]
        public void InvalidWaveInputs_FailClearly()
        {
            EnemyDefinition enemy = CreateEnemy("enemy_test");
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new WaveDefinition("wave_01", enemy, 0, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new WaveDefinition("wave_01", enemy, 1, 0f));
            Assert.Throws<ArgumentException>(() =>
                new WaveSystem(Array.Empty<WaveDefinition>()));

            WaveDefinition wave = CreateWave("wave_same", 1, 1f);
            Assert.Throws<ArgumentException>(() =>
                new WaveSystem(new[] { wave, wave }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new WaveSystem(new[] { wave }, long.MaxValue));

            var system = new WaveSystem(new[] { wave });
            Assert.Throws<ArgumentOutOfRangeException>(() => system.Advance(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => system.MarkEnemyRemoved(-1));
        }

        private static string[] RunSequence()
        {
            var system = new WaveSystem(new[]
            {
                CreateWave("wave_01", 2, 0.5f),
                CreateWave("wave_02", 1, 1f)
            }, initialSpawnOrder: 3);
            var sequence = new List<string>();

            WaveAdvanceResult first = system.Advance(0.5f);
            AddSpawns(sequence, first);
            foreach (EnemySpawnRequest request in first.SpawnRequests)
            {
                system.MarkEnemyRemoved(request.SpawnOrder);
            }

            WaveAdvanceResult second = system.Advance(0f);
            AddSpawns(sequence, second);
            foreach (EnemySpawnRequest request in second.SpawnRequests)
            {
                system.MarkEnemyRemoved(request.SpawnOrder);
            }

            WaveAdvanceResult completed = system.Advance(0f);
            sequence.Add($"complete:{completed.CompletedWaveIndex}:{completed.StageCompletedThisAdvance}");
            return sequence.ToArray();
        }

        private static void AddSpawns(List<string> sequence, WaveAdvanceResult result)
        {
            foreach (EnemySpawnRequest request in result.SpawnRequests)
            {
                sequence.Add($"{request.WaveId}:{request.SpawnIndex}:{request.SpawnOrder}:{request.Enemy.Id}");
            }
        }

        private static WaveDefinition CreateWave(string id, int enemyCount, float interval)
        {
            return new WaveDefinition(id, CreateEnemy($"enemy_{id}"), enemyCount, interval);
        }

        private static EnemyDefinition CreateEnemy(string id)
        {
            return new EnemyDefinition(id, 10f, 1f, endpointDamage: 1, killReward: 1);
        }
    }
}
