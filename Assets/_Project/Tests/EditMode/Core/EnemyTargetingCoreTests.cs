#nullable enable

using System;
using NUnit.Framework;
using RandomTowerDefense.Core.Combat;
using RandomTowerDefense.Core.Enemies;
using RandomTowerDefense.Core.Towers;

namespace RandomTowerDefense.Tests.EditMode.Core
{
    public sealed class EnemyTargetingCoreTests
    {
        [Test]
        public void Advance_MovesAcrossPathSegments()
        {
            EnemyState enemy = CreateEnemy(moveSpeed: 2f);

            EnemyAdvanceResult result = enemy.Advance(2f);

            Assert.That(result.DistanceMoved, Is.EqualTo(4f));
            Assert.That(result.ReachedEndpoint, Is.False);
            Assert.That(enemy.Position.X, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(enemy.Position.Y, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(enemy.PathProgress, Is.EqualTo(4f / 7f).Within(0.0001f));
        }

        [Test]
        public void Advance_EmitsEndpointDamageOnlyOnce()
        {
            EnemyState enemy = CreateEnemy(moveSpeed: 10f, endpointDamage: 3);
            int endpointEvents = 0;
            int totalEndpointDamage = 0;

            for (int step = 0; step < 4; step++)
            {
                EnemyAdvanceResult result = enemy.Advance(0.25f);
                if (result.ReachedEndpoint)
                {
                    endpointEvents++;
                    totalEndpointDamage += result.EndpointDamage;
                }
            }

            DamageResult damageAfterEndpoint = enemy.ApplyDamage(100f);

            Assert.That(endpointEvents, Is.EqualTo(1));
            Assert.That(totalEndpointDamage, Is.EqualTo(3));
            Assert.That(enemy.Status, Is.EqualTo(EnemyStatus.ReachedEndpoint));
            Assert.That(damageAfterEndpoint.Killed, Is.False);
            Assert.That(damageAfterEndpoint.KillReward, Is.Zero);
        }

        [Test]
        public void ApplyDamage_EmitsKillRewardOnlyOnce()
        {
            EnemyState enemy = CreateEnemy(maxHealth: 10f, killReward: 4);

            DamageResult first = enemy.ApplyDamage(4f);
            DamageResult lethal = enemy.ApplyDamage(10f);
            DamageResult duplicate = enemy.ApplyDamage(100f);
            EnemyAdvanceResult movementAfterDeath = enemy.Advance(100f);

            Assert.That(first.AppliedDamage, Is.EqualTo(4f));
            Assert.That(first.Killed, Is.False);
            Assert.That(lethal.AppliedDamage, Is.EqualTo(6f));
            Assert.That(lethal.Killed, Is.True);
            Assert.That(lethal.KillReward, Is.EqualTo(4));
            Assert.That(duplicate.AppliedDamage, Is.Zero);
            Assert.That(duplicate.Killed, Is.False);
            Assert.That(duplicate.KillReward, Is.Zero);
            Assert.That(movementAfterDeath.ReachedEndpoint, Is.False);
            Assert.That(movementAfterDeath.EndpointDamage, Is.Zero);
            Assert.That(enemy.Status, Is.EqualTo(EnemyStatus.Dead));
        }

        [Test]
        public void Select_PrioritizesHighestProgressWithinInclusiveRange()
        {
            EnemyState leading = CreateEnemy(spawnOrder: 2);
            EnemyState trailing = CreateEnemy(spawnOrder: 1);
            leading.Advance(2.5f);
            trailing.Advance(2f);

            EnemyState? selected = TargetSelector.Select(
                new Point2(3f, -3f),
                5f,
                new[] { trailing, leading });

            Assert.That(selected, Is.SameAs(leading));
            Assert.That(leading.Position, Is.EqualTo(new Point2(3f, 2f)));
        }

        [Test]
        public void Select_UsesLowestSpawnOrderForProgressTie()
        {
            EnemyState laterSpawn = CreateEnemy(spawnOrder: 2);
            EnemyState earlierSpawn = CreateEnemy(spawnOrder: 1);
            laterSpawn.Advance(1f);
            earlierSpawn.Advance(1f);

            EnemyState? selected = TargetSelector.Select(
                new Point2(0f, 0f),
                10f,
                new[] { laterSpawn, earlierSpawn });

            Assert.That(selected, Is.SameAs(earlierSpawn));
        }

        [Test]
        public void Select_IgnoresDeadAndOutOfRangeEnemies()
        {
            EnemyState dead = CreateEnemy(spawnOrder: 0, maxHealth: 1f);
            EnemyState outOfRange = CreateEnemy(spawnOrder: 1);
            dead.ApplyDamage(1f);
            outOfRange.Advance(3f);

            EnemyState? selected = TargetSelector.Select(
                new Point2(0f, 0f),
                2f,
                new[] { dead, outOfRange });

            Assert.That(selected, Is.Null);
        }

        [Test]
        public void InvalidDefinitionsAndTimeSteps_FailClearly()
        {
            Assert.Throws<ArgumentException>(() =>
                new EnemyDefinition("Enemy Slime", 10f, 1f, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new EnemyDefinition("enemy_slime", 0f, 1f, 1, 1));
            Assert.Throws<ArgumentException>(() =>
                new Path2D(new[] { new Point2(0f, 0f) }));
            Assert.Throws<ArgumentException>(() =>
                new Path2D(new[] { new Point2(0f, 0f), new Point2(0f, 0f) }));

            EnemyState enemy = CreateEnemy();
            Assert.Throws<ArgumentOutOfRangeException>(() => enemy.Advance(-0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => enemy.ApplyDamage(float.NaN));
        }

        private static EnemyState CreateEnemy(
            long spawnOrder = 0,
            float maxHealth = 10f,
            float moveSpeed = 2f,
            int endpointDamage = 1,
            int killReward = 1)
        {
            var definition = new EnemyDefinition(
                "enemy_test",
                maxHealth,
                moveSpeed,
                endpointDamage,
                killReward);
            var path = new Path2D(new[]
            {
                new Point2(0f, 0f),
                new Point2(3f, 0f),
                new Point2(3f, 4f)
            });
            return new EnemyState(definition, path, spawnOrder);
        }
    }
}
