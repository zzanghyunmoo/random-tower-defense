#nullable enable

using System;
using System.Collections.Generic;
using NUnit.Framework;
using RandomTowerDefense.Core.Combat;
using RandomTowerDefense.Core.Enemies;
using RandomTowerDefense.Core.Towers;

namespace RandomTowerDefense.Tests.EditMode.Core
{
    public sealed class TowerProjectileCoreTests
    {
        [Test]
        public void TowerAttackSystem_FiresImmediatelyAndAtExactCooldownBoundary()
        {
            EnemyState enemy = CreateEnemy(position: 2f);
            TowerState tower = CreateTower(placementOrder: 0, attackInterval: 1f);
            var system = new TowerAttackSystem();

            IReadOnlyList<ProjectileState> initial = system.Advance(0.25f, new[] { tower }, new[] { enemy });
            IReadOnlyList<ProjectileState> beforeBoundary = system.Advance(0.5f, new[] { tower }, new[] { enemy });
            IReadOnlyList<ProjectileState> atBoundary = system.Advance(0.25f, new[] { tower }, new[] { enemy });

            Assert.That(initial, Has.Count.EqualTo(1));
            Assert.That(beforeBoundary, Is.Empty);
            Assert.That(atBoundary, Has.Count.EqualTo(1));
            Assert.That(initial[0].Order, Is.EqualTo(0));
            Assert.That(atBoundary[0].Order, Is.EqualTo(1));
            Assert.That(tower.CooldownRemaining, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void TowerAttackSystem_CatchesUpCadenceAndUsesPlacementOrder()
        {
            EnemyState enemy = CreateEnemy(position: 2f);
            TowerState laterTower = CreateTower(placementOrder: 2, attackInterval: 1f);
            TowerState earlierTower = CreateTower(placementOrder: 1, attackInterval: 1f);
            var system = new TowerAttackSystem(initialProjectileOrder: 10);

            IReadOnlyList<ProjectileState> projectiles = system.Advance(
                2.5f,
                new[] { laterTower, earlierTower },
                new[] { enemy });

            Assert.That(projectiles, Has.Count.EqualTo(6));
            Assert.That(projectiles[0].SourceTowerOrder, Is.EqualTo(1));
            Assert.That(projectiles[2].SourceTowerOrder, Is.EqualTo(1));
            Assert.That(projectiles[3].SourceTowerOrder, Is.EqualTo(2));
            Assert.That(projectiles[5].SourceTowerOrder, Is.EqualTo(2));
            Assert.That(projectiles[0].Order, Is.EqualTo(10));
            Assert.That(projectiles[5].Order, Is.EqualTo(15));
            Assert.That(system.NextProjectileOrder, Is.EqualTo(16));
            Assert.That(earlierTower.CooldownRemaining, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void TowerAttackSystem_RemainsReadyWhenNoTargetExists()
        {
            EnemyState enemy = CreateEnemy(position: 20f);
            TowerState tower = CreateTower(placementOrder: 0, attackInterval: 1f, range: 5f);
            var system = new TowerAttackSystem();

            IReadOnlyList<ProjectileState> projectiles = system.Advance(3f, new[] { tower }, new[] { enemy });

            Assert.That(projectiles, Is.Empty);
            Assert.That(tower.CooldownRemaining, Is.Zero);
        }

        [Test]
        public void Projectile_TravelsAndAppliesDamageOnlyOnce()
        {
            EnemyState enemy = CreateEnemy(position: 3f, maxHealth: 10f);
            var projectile = new ProjectileState(0, 0, new Point2(0f, 0f), enemy, 2f, 4f);

            ProjectileAdvanceResult movement = projectile.Advance(1f);
            Point2 positionAfterMovement = projectile.Position;
            ProjectileAdvanceResult hit = projectile.Advance(0.5f);
            ProjectileAdvanceResult duplicate = projectile.Advance(10f);

            Assert.That(movement.DistanceMoved, Is.EqualTo(2f));
            Assert.That(movement.ResolvedThisAdvance, Is.False);
            Assert.That(positionAfterMovement, Is.EqualTo(new Point2(2f, 0f)));
            Assert.That(projectile.Position, Is.EqualTo(new Point2(3f, 0f)));
            Assert.That(hit.ResolvedThisAdvance, Is.True);
            Assert.That(hit.Status, Is.EqualTo(ProjectileStatus.Hit));
            Assert.That(hit.Damage.AppliedDamage, Is.EqualTo(4f));
            Assert.That(duplicate.ResolvedThisAdvance, Is.False);
            Assert.That(duplicate.Damage.AppliedDamage, Is.Zero);
            Assert.That(enemy.CurrentHealth, Is.EqualTo(6f));
        }

        [Test]
        public void ProjectileResolver_UsesProjectileOrderAndEmitsOneKillReward()
        {
            EnemyState enemy = CreateEnemy(position: 3f, maxHealth: 5f, killReward: 7);
            var lethalFirst = new ProjectileState(1, 0, new Point2(0f, 0f), enemy, 10f, 5f);
            var redundantSecond = new ProjectileState(2, 1, new Point2(0f, 0f), enemy, 10f, 3f);

            IReadOnlyList<ProjectileAdvanceResult> results = ProjectileResolver.Advance(
                1f,
                new[] { redundantSecond, lethalFirst });
            IReadOnlyList<ProjectileAdvanceResult> duplicate = ProjectileResolver.Advance(
                1f,
                new[] { redundantSecond, lethalFirst });

            Assert.That(results[0].ProjectileOrder, Is.EqualTo(1));
            Assert.That(results[0].Status, Is.EqualTo(ProjectileStatus.Hit));
            Assert.That(results[0].Damage.Killed, Is.True);
            Assert.That(results[0].Damage.KillReward, Is.EqualTo(7));
            Assert.That(results[1].ProjectileOrder, Is.EqualTo(2));
            Assert.That(results[1].Status, Is.EqualTo(ProjectileStatus.TargetLost));
            Assert.That(results[1].Damage.AppliedDamage, Is.Zero);
            Assert.That(duplicate[0].ResolvedThisAdvance, Is.False);
            Assert.That(duplicate[1].ResolvedThisAdvance, Is.False);
            Assert.That(duplicate[0].Damage.KillReward + duplicate[1].Damage.KillReward, Is.Zero);
            Assert.That(enemy.CurrentHealth, Is.Zero);
        }

        [Test]
        public void Projectile_CleansUpWhenTargetReachedEndpointFirst()
        {
            EnemyState enemy = CreateEnemy(position: 9f, moveSpeed: 10f);
            var projectile = new ProjectileState(0, 0, new Point2(0f, 0f), enemy, 1f, 5f);
            enemy.Advance(1f);

            ProjectileAdvanceResult result = projectile.Advance(1f);

            Assert.That(enemy.Status, Is.EqualTo(EnemyStatus.ReachedEndpoint));
            Assert.That(result.ResolvedThisAdvance, Is.True);
            Assert.That(result.Status, Is.EqualTo(ProjectileStatus.TargetLost));
            Assert.That(result.Damage.AppliedDamage, Is.Zero);
        }

        [Test]
        public void InvalidTowerAndProjectileInputs_FailClearly()
        {
            Assert.Throws<ArgumentException>(() => new TowerDefinition("Tower Basic", 5f, 1f, 2f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TowerDefinition("tower_basic", -1f, 1f, 2f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TowerDefinition("tower_basic", 5f, 0f, 2f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TowerDefinition("tower_basic", 5f, 1f, 0f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TowerDefinition("tower_basic", 5f, 1f, 2f, 0f));

            EnemyState enemy = CreateEnemy(position: 2f);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ProjectileState(-1, 0, new Point2(0f, 0f), enemy, 1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ProjectileState(0, 0, new Point2(0f, 0f), enemy, float.NaN, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new TowerAttackSystem().Advance(float.NaN, Array.Empty<TowerState>(), new[] { enemy }));
        }

        private static TowerState CreateTower(
            long placementOrder,
            float attackInterval,
            float range = 10f)
        {
            var definition = new TowerDefinition(
                "tower_test",
                range,
                attackInterval,
                projectileSpeed: 5f,
                projectileDamage: 2f);
            return new TowerState(definition, new Point2(0f, 0f), placementOrder);
        }

        private static EnemyState CreateEnemy(
            float position,
            float maxHealth = 10f,
            float moveSpeed = 1f,
            int killReward = 1)
        {
            var definition = new EnemyDefinition(
                "enemy_projectile_test",
                maxHealth,
                moveSpeed,
                endpointDamage: 1,
                killReward);
            var path = new Path2D(new[]
            {
                new Point2(0f, 0f),
                new Point2(10f, 0f)
            });
            var enemy = new EnemyState(definition, path, spawnOrder: 0);
            enemy.Advance(position / moveSpeed);
            return enemy;
        }
    }
}
