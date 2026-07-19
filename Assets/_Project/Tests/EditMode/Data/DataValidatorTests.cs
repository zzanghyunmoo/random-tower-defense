#nullable enable

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RandomTowerDefense.Data.Definitions;
using RandomTowerDefense.Data.Validation;
using UnityEngine;

namespace RandomTowerDefense.Tests.EditMode.Data
{
    public sealed class DataValidatorTests
    {
        private readonly List<ScriptableObject> _assets = new List<ScriptableObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (ScriptableObject asset in _assets)
            {
                Object.DestroyImmediate(asset);
            }

            _assets.Clear();
        }

        [Test]
        public void CompleteDataSetHasNoValidationIssues()
        {
            IReadOnlyList<DefinitionAsset> definitions = CreateValidDataSet();

            DataValidationResult result = DataValidator.Validate(definitions);

            Assert.That(result.IsValid, Is.True, string.Join("\n", result.Issues));
            Assert.That(result.Issues, Is.Empty);
        }

        [Test]
        public void InvalidDataSetCollectsIndependentErrors()
        {
            EnemyDefinitionAsset enemy = Create<EnemyDefinitionAsset>();
            enemy.ConfigureForEditor("duplicate_id", -1f, float.NaN, -1, -1);
            ProjectileDefinitionAsset projectile = Create<ProjectileDefinitionAsset>();
            projectile.ConfigureForEditor("duplicate_id", 0f);
            TowerDefinitionAsset tower = Create<TowerDefinitionAsset>();
            tower.ConfigureForEditor("Tower Bad", -1f, 0f, float.PositiveInfinity, null!);
            WaveDefinitionAsset wave = Create<WaveDefinitionAsset>();
            wave.ConfigureForEditor(string.Empty, null!, 0, float.NaN);
            StageDefinitionAsset stage = Create<StageDefinitionAsset>();
            stage.ConfigureForEditor(
                "stage_invalid",
                0,
                null!,
                new[] { Vector2.zero, Vector2.zero, new Vector2(float.NaN, 1f) },
                new[] { wave },
                new[]
                {
                    new TowerSlotData("slot_bad", Vector2.zero, 0),
                    new TowerSlotData("slot_bad", new Vector2(float.PositiveInfinity, 0f), 0),
                },
                new[] { new TowerPoolEntryData(tower, 0) });

            DataValidationResult result = DataValidator.Validate(
                new DefinitionAsset[] { enemy, projectile, tower, wave, stage });
            string[] codes = result.Issues.Select(issue => issue.Code).ToArray();

            Assert.That(result.IsValid, Is.False);
            Assert.That(codes, Does.Contain("id.duplicate"));
            Assert.That(codes, Does.Contain("enemy.max_health"));
            Assert.That(codes, Does.Contain("tower.projectile.required"));
            Assert.That(codes, Does.Contain("wave.enemy.required"));
            Assert.That(codes, Does.Contain("stage.economy.required"));
            Assert.That(codes, Does.Contain("stage.path.overlap"));
            Assert.That(codes, Does.Contain("stage.path.finite"));
            Assert.That(codes, Does.Contain("stage.tower_slot.duplicate_id"));
            Assert.That(codes, Does.Contain("stage.tower_slot.duplicate_order"));
            Assert.That(codes, Does.Contain("stage.summon_pool.weight"));
        }

        [Test]
        public void IndividualValidationUsesTheSameLocalRules()
        {
            EnemyDefinitionAsset enemy = Create<EnemyDefinitionAsset>();
            enemy.ConfigureForEditor("Enemy Bad", 0f, -1f, -1, -1);

            DataValidationResult result = DataValidator.ValidateDefinition(enemy);
            string[] codes = result.Issues.Select(issue => issue.Code).ToArray();

            Assert.That(codes, Does.Contain("id.format"));
            Assert.That(codes, Does.Contain("enemy.max_health"));
            Assert.That(codes, Does.Contain("enemy.move_speed"));
            Assert.That(codes, Does.Contain("enemy.endpoint_damage"));
            Assert.That(codes, Does.Contain("enemy.kill_reward"));
        }

        [Test]
        public void SummonWeightAndMaximumCurrencyOverflowAreReported()
        {
            EnemyDefinitionAsset enemy = Create<EnemyDefinitionAsset>();
            enemy.ConfigureForEditor("enemy_rich", 1f, 1f, 1, int.MaxValue);
            WaveDefinitionAsset wave = Create<WaveDefinitionAsset>();
            wave.ConfigureForEditor("wave_rich", enemy, 2, 1f);
            EconomyDefinitionAsset economy = Create<EconomyDefinitionAsset>();
            economy.ConfigureForEditor("economy_rich", 1, 1);
            ProjectileDefinitionAsset projectileA = Create<ProjectileDefinitionAsset>();
            projectileA.ConfigureForEditor("projectile_a", 1f);
            ProjectileDefinitionAsset projectileB = Create<ProjectileDefinitionAsset>();
            projectileB.ConfigureForEditor("projectile_b", 1f);
            TowerDefinitionAsset towerA = Create<TowerDefinitionAsset>();
            towerA.ConfigureForEditor("tower_a", 1f, 1f, 1f, projectileA);
            TowerDefinitionAsset towerB = Create<TowerDefinitionAsset>();
            towerB.ConfigureForEditor("tower_b", 1f, 1f, 1f, projectileB);
            StageDefinitionAsset stage = Create<StageDefinitionAsset>();
            stage.ConfigureForEditor(
                "stage_overflow",
                1,
                economy,
                new[] { Vector2.zero, Vector2.right },
                new[] { wave },
                new[] { new TowerSlotData("slot_a", Vector2.zero, 0) },
                new[]
                {
                    new TowerPoolEntryData(towerA, int.MaxValue),
                    new TowerPoolEntryData(towerB, 1),
                });

            DataValidationResult result = DataValidator.Validate(
                new DefinitionAsset[] { enemy, wave, economy, projectileA, projectileB, towerA, towerB, stage });
            string[] codes = result.Issues.Select(issue => issue.Code).ToArray();

            Assert.That(codes, Does.Contain("stage.summon_pool.weight_total"));
            Assert.That(codes, Does.Contain("stage.currency.maximum"));
        }

        [Test]
        public void ReferencesOutsideDataSetAreReported()
        {
            IReadOnlyList<DefinitionAsset> complete = CreateValidDataSet();
            DefinitionAsset[] incomplete = complete
                .Where(definition => definition is not EconomyDefinitionAsset && definition is not ProjectileDefinitionAsset)
                .ToArray();

            DataValidationResult result = DataValidator.Validate(incomplete);
            string[] codes = result.Issues.Select(issue => issue.Code).ToArray();

            Assert.That(codes, Does.Contain("stage.economy.outside_dataset"));
            Assert.That(codes, Does.Contain("tower.projectile.outside_dataset"));
        }

        private IReadOnlyList<DefinitionAsset> CreateValidDataSet()
        {
            EnemyDefinitionAsset enemy = Create<EnemyDefinitionAsset>();
            enemy.ConfigureForEditor("enemy_basic", 10f, 2f, 1, 2);
            ProjectileDefinitionAsset projectile = Create<ProjectileDefinitionAsset>();
            projectile.ConfigureForEditor("projectile_basic", 8f);
            TowerDefinitionAsset tower = Create<TowerDefinitionAsset>();
            tower.ConfigureForEditor("tower_basic", 3f, 1f, 4f, projectile);
            WaveDefinitionAsset wave = Create<WaveDefinitionAsset>();
            wave.ConfigureForEditor("wave_stage_01_01", enemy, 3, 0.5f);
            EconomyDefinitionAsset economy = Create<EconomyDefinitionAsset>();
            economy.ConfigureForEditor("economy_basic", 10, 5);
            StageDefinitionAsset stage = Create<StageDefinitionAsset>();
            stage.ConfigureForEditor(
                "stage_01",
                10,
                economy,
                new[] { Vector2.zero, Vector2.right },
                new[] { wave },
                new[] { new TowerSlotData("slot_01", Vector2.zero, 0) },
                new[] { new TowerPoolEntryData(tower, 1) });

            return new DefinitionAsset[] { enemy, projectile, tower, wave, economy, stage };
        }

        private T Create<T>()
            where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);
            return asset;
        }
    }
}
