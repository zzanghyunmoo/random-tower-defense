#nullable enable

using System;
using System.Collections.Generic;
using NUnit.Framework;
using RandomTowerDefense.Core.Combat;
using RandomTowerDefense.Data.Definitions;
using RandomTowerDefense.Data.Runtime;
using UnityEngine;

namespace RandomTowerDefense.Tests.EditMode.Data
{
    public sealed class GameDataMapperTests
    {
        private readonly List<UnityEngine.Object> _createdAssets = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object asset in _createdAssets)
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }

            _createdAssets.Clear();
        }

        [Test]
        public void ToCore_MapsCompleteStageIntoPlayableSessionDefinition()
        {
            ProjectileDefinitionAsset projectile = CreateProjectile("projectile_basic", 8f);
            TowerDefinitionAsset arrow = CreateTower("tower_arrow", projectile, damage: 3f);
            TowerDefinitionAsset frost = CreateTower("tower_frost", projectile, damage: 2f);
            EnemyDefinitionAsset slime = CreateEnemy("enemy_slime", maxHealth: 5f, killReward: 2);
            EnemyDefinitionAsset runner = CreateEnemy("enemy_runner", maxHealth: 3f, killReward: 3);
            WaveDefinitionAsset wave1 = CreateWave("wave_01", slime, 2, 1f);
            WaveDefinitionAsset wave2 = CreateWave("wave_02", runner, 3, 0.8f);
            WaveDefinitionAsset wave3 = CreateWave("wave_03", slime, 4, 0.6f);
            EconomyDefinitionAsset economy = CreateEconomy("economy_default", 20, 10);
            StageDefinitionAsset stage = CreateAsset<StageDefinitionAsset>();
            stage.ConfigureForEditor(
                "stage_01",
                startingHealth: 10,
                economy,
                new[] { new Vector2(0f, 0f), new Vector2(5f, 0f), new Vector2(5f, 3f) },
                new[] { wave1, wave2, wave3 },
                new[]
                {
                    new TowerSlotData("slot_00", new Vector2(1f, 1f), 0),
                    new TowerSlotData("slot_01", new Vector2(3f, 1f), 1)
                },
                new[]
                {
                    new TowerPoolEntryData(arrow, 3),
                    new TowerPoolEntryData(frost, 1)
                });

            GameSessionDefinition result = GameDataMapper.ToCore(stage);
            var session = new GameSession(result, seed: 42);
            GameSessionTickResult firstTick = session.Advance(0f);

            Assert.That(result.Id, Is.EqualTo("stage_01"));
            Assert.That(result.StartingHealth, Is.EqualTo(10));
            Assert.That(result.StartingCurrency, Is.EqualTo(20));
            Assert.That(result.SummonCost, Is.EqualTo(10));
            Assert.That(result.Waves, Has.Count.EqualTo(3));
            Assert.That(result.Waves[1].Enemy.Id, Is.EqualTo("enemy_runner"));
            Assert.That(result.TowerSlots, Has.Count.EqualTo(2));
            Assert.That(result.SummonPool, Has.Count.EqualTo(2));
            Assert.That(result.SummonPool[0].Tower.ProjectileDamage, Is.EqualTo(3f));
            Assert.That(result.SummonPool[0].Tower.ProjectileSpeed, Is.EqualTo(8f));
            Assert.That(session.CanSummon, Is.True);
            Assert.That(firstTick.SpawnPhase!.SpawnedEnemies[0].Definition.Id, Is.EqualTo("enemy_slime"));
        }

        [Test]
        public void ToCore_MissingReferencesFailWithAssetContext()
        {
            EnemyDefinitionAsset enemy = CreateEnemy("enemy_slime", 5f, 1);
            WaveDefinitionAsset wave = CreateWave("wave_01", enemy, 1, 1f);
            EconomyDefinitionAsset economy = CreateEconomy("economy_default", 10, 10);
            TowerDefinitionAsset towerWithoutProjectile = CreateAsset<TowerDefinitionAsset>();
            towerWithoutProjectile.SetIdForEditor("tower_broken");
            StageDefinitionAsset stage = CreateAsset<StageDefinitionAsset>();
            stage.ConfigureForEditor(
                "stage_01",
                5,
                economy,
                new[] { Vector2.zero, Vector2.right },
                new[] { wave },
                new[] { new TowerSlotData("slot_00", Vector2.zero, 0) },
                new[] { new TowerPoolEntryData(towerWithoutProjectile, 1) });

            ArgumentException exception = Assert.Throws<ArgumentException>(() => GameDataMapper.ToCore(stage))!;

            Assert.That(exception.Message, Does.Contain("tower_broken"));
            Assert.That(exception.Message, Does.Contain("projectile"));
        }

        private T CreateAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            _createdAssets.Add(asset);
            return asset;
        }

        private EnemyDefinitionAsset CreateEnemy(string id, float maxHealth, int killReward)
        {
            EnemyDefinitionAsset asset = CreateAsset<EnemyDefinitionAsset>();
            asset.ConfigureForEditor(id, maxHealth, moveSpeed: 1f, endpointDamage: 1, killReward);
            return asset;
        }

        private ProjectileDefinitionAsset CreateProjectile(string id, float speed)
        {
            ProjectileDefinitionAsset asset = CreateAsset<ProjectileDefinitionAsset>();
            asset.ConfigureForEditor(id, speed);
            return asset;
        }

        private TowerDefinitionAsset CreateTower(
            string id,
            ProjectileDefinitionAsset projectile,
            float damage)
        {
            TowerDefinitionAsset asset = CreateAsset<TowerDefinitionAsset>();
            asset.ConfigureForEditor(id, range: 5f, attackIntervalSeconds: 1f, damage, projectile);
            return asset;
        }

        private WaveDefinitionAsset CreateWave(
            string id,
            EnemyDefinitionAsset enemy,
            int count,
            float interval)
        {
            WaveDefinitionAsset asset = CreateAsset<WaveDefinitionAsset>();
            asset.ConfigureForEditor(id, enemy, count, interval);
            return asset;
        }

        private EconomyDefinitionAsset CreateEconomy(string id, int currency, int cost)
        {
            EconomyDefinitionAsset asset = CreateAsset<EconomyDefinitionAsset>();
            asset.ConfigureForEditor(id, currency, cost);
            return asset;
        }
    }
}
