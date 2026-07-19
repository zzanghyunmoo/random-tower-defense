#nullable enable

using System;
using System.Collections.Generic;
using RandomTowerDefense.Data.Definitions;
using UnityEngine;

namespace RandomTowerDefense.Data.Validation
{
    public static class DataValidator
    {
        public static DataValidationResult Validate(IReadOnlyList<DefinitionAsset> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            var issues = new List<DataValidationIssue>();
            var knownDefinitions = new HashSet<DefinitionAsset>();
            bool hasStage = false;

            for (int index = 0; index < definitions.Count; index++)
            {
                DefinitionAsset? definition = definitions[index];
                if (definition == null)
                {
                    Add(issues, "dataset.null_definition", "The data set contains a null definition.", null);
                    continue;
                }

                knownDefinitions.Add(definition);
                hasStage |= definition is StageDefinitionAsset;
                ValidateLocal(definition, issues);
            }

            if (knownDefinitions.Count == 0)
            {
                Add(issues, "dataset.empty", "The data set must contain definitions.", null);
            }
            else if (!hasStage)
            {
                Add(issues, "dataset.stage.required", "The data set must contain at least one stage.", null);
            }

            ValidateDuplicateDefinitionIds(definitions, issues);
            foreach (DefinitionAsset? definition in definitions)
            {
                if (definition != null)
                {
                    ValidateReferences(definition, knownDefinitions, issues);
                }
            }

            return new DataValidationResult(issues);
        }

        public static DataValidationResult ValidateDefinition(DefinitionAsset definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            var issues = new List<DataValidationIssue>();
            ValidateLocal(definition, issues);
            return new DataValidationResult(issues);
        }

        private static void ValidateLocal(DefinitionAsset definition, List<DataValidationIssue> issues)
        {
            ValidateId(definition.Id, "id", "Definition ID", definition, issues);

            switch (definition)
            {
                case EnemyDefinitionAsset enemy:
                    ValidatePositive(enemy.MaxHealth, "enemy.max_health", "Enemy max health", enemy, issues);
                    ValidateNonNegative(enemy.MoveSpeed, "enemy.move_speed", "Enemy move speed", enemy, issues);
                    ValidateNonNegative(enemy.EndpointDamage, "enemy.endpoint_damage", "Enemy endpoint damage", enemy, issues);
                    ValidateNonNegative(enemy.KillReward, "enemy.kill_reward", "Enemy kill reward", enemy, issues);
                    break;

                case ProjectileDefinitionAsset projectile:
                    ValidatePositive(projectile.Speed, "projectile.speed", "Projectile speed", projectile, issues);
                    break;

                case TowerDefinitionAsset tower:
                    ValidateNonNegative(tower.Range, "tower.range", "Tower range", tower, issues);
                    ValidatePositive(
                        tower.AttackIntervalSeconds,
                        "tower.attack_interval",
                        "Tower attack interval",
                        tower,
                        issues);
                    ValidatePositive(tower.Damage, "tower.damage", "Tower damage", tower, issues);
                    if (tower.Projectile == null)
                    {
                        Add(issues, "tower.projectile.required", $"{Label(tower)} requires a projectile.", tower);
                    }

                    break;

                case WaveDefinitionAsset wave:
                    if (wave.Enemy == null)
                    {
                        Add(issues, "wave.enemy.required", $"{Label(wave)} requires an enemy.", wave);
                    }

                    ValidatePositive(wave.EnemyCount, "wave.enemy_count", "Wave enemy count", wave, issues);
                    ValidatePositive(
                        wave.SpawnIntervalSeconds,
                        "wave.spawn_interval",
                        "Wave spawn interval",
                        wave,
                        issues);
                    break;

                case EconomyDefinitionAsset economy:
                    ValidateNonNegative(
                        economy.StartingCurrency,
                        "economy.starting_currency",
                        "Starting currency",
                        economy,
                        issues);
                    ValidateNonNegative(economy.SummonCost, "economy.summon_cost", "Summon cost", economy, issues);
                    break;

                case StageDefinitionAsset stage:
                    ValidateStage(stage, issues);
                    break;
            }
        }

        private static void ValidateStage(StageDefinitionAsset stage, List<DataValidationIssue> issues)
        {
            ValidatePositive(stage.StartingHealth, "stage.starting_health", "Stage starting health", stage, issues);
            if (stage.Economy == null)
            {
                Add(issues, "stage.economy.required", $"{Label(stage)} requires an economy.", stage);
            }

            ValidatePath(stage, issues);
            ValidateWaves(stage, issues);
            ValidateTowerSlots(stage, issues);
            ValidateSummonPool(stage, issues);
            ValidateMaximumCurrency(stage, issues);
        }

        private static void ValidatePath(StageDefinitionAsset stage, List<DataValidationIssue> issues)
        {
            IReadOnlyList<Vector2>? points = stage.PathPoints;
            if (points == null || points.Count < 2)
            {
                Add(issues, "stage.path.minimum", $"{Label(stage)} requires at least two path points.", stage);
                return;
            }

            double totalLength = 0d;
            for (int index = 0; index < points.Count; index++)
            {
                Vector2 point = points[index];
                if (!IsFinite(point.x) || !IsFinite(point.y))
                {
                    Add(issues, "stage.path.finite", $"{Label(stage)} path point {index} must be finite.", stage);
                    continue;
                }

                if (index == 0)
                {
                    continue;
                }

                Vector2 previous = points[index - 1];
                if (!IsFinite(previous.x) || !IsFinite(previous.y))
                {
                    continue;
                }

                double deltaX = (double)point.x - previous.x;
                double deltaY = (double)point.y - previous.y;
                double squaredLength = (deltaX * deltaX) + (deltaY * deltaY);
                if (squaredLength == 0d)
                {
                    Add(
                        issues,
                        "stage.path.overlap",
                        $"{Label(stage)} path points {index - 1} and {index} must not overlap.",
                        stage);
                    continue;
                }

                totalLength += Math.Sqrt(squaredLength);
                if (totalLength > float.MaxValue)
                {
                    Add(issues, "stage.path.length", $"{Label(stage)} path length exceeds the supported range.", stage);
                    return;
                }
            }
        }

        private static void ValidateWaves(StageDefinitionAsset stage, List<DataValidationIssue> issues)
        {
            IReadOnlyList<WaveDefinitionAsset>? waves = stage.Waves;
            if (waves == null || waves.Count == 0)
            {
                Add(issues, "stage.waves.minimum", $"{Label(stage)} requires at least one wave.", stage);
                return;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < waves.Count; index++)
            {
                WaveDefinitionAsset? wave = waves[index];
                if (wave == null)
                {
                    Add(issues, "stage.wave.required", $"{Label(stage)} wave {index} is missing.", stage);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(wave.Id) && !ids.Add(wave.Id))
                {
                    Add(issues, "stage.wave.duplicate", $"{Label(stage)} repeats wave ID '{wave.Id}'.", stage);
                }
            }
        }

        private static void ValidateTowerSlots(StageDefinitionAsset stage, List<DataValidationIssue> issues)
        {
            IReadOnlyList<TowerSlotData>? slots = stage.TowerSlots;
            if (slots == null || slots.Count == 0)
            {
                Add(issues, "stage.tower_slots.minimum", $"{Label(stage)} requires at least one tower slot.", stage);
                return;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var placementOrders = new HashSet<long>();
            for (int index = 0; index < slots.Count; index++)
            {
                TowerSlotData? slot = slots[index];
                if (slot == null)
                {
                    Add(issues, "stage.tower_slot.required", $"{Label(stage)} tower slot {index} is missing.", stage);
                    continue;
                }

                ValidateId(slot.Id, "stage.tower_slot.id", $"Tower slot {index} ID", stage, issues);
                if (!string.IsNullOrWhiteSpace(slot.Id) && !ids.Add(slot.Id))
                {
                    Add(issues, "stage.tower_slot.duplicate_id", $"{Label(stage)} repeats tower slot ID '{slot.Id}'.", stage);
                }

                if (slot.PlacementOrder < 0)
                {
                    Add(issues, "stage.tower_slot.placement_order", $"{Label(stage)} tower slot {index} placement order must not be negative.", stage);
                }
                else if (!placementOrders.Add(slot.PlacementOrder))
                {
                    Add(issues, "stage.tower_slot.duplicate_order", $"{Label(stage)} repeats tower slot placement order {slot.PlacementOrder}.", stage);
                }

                if (!IsFinite(slot.Position.x) || !IsFinite(slot.Position.y))
                {
                    Add(issues, "stage.tower_slot.position", $"{Label(stage)} tower slot {index} position must be finite.", stage);
                }
            }
        }

        private static void ValidateSummonPool(StageDefinitionAsset stage, List<DataValidationIssue> issues)
        {
            IReadOnlyList<TowerPoolEntryData>? pool = stage.SummonPool;
            if (pool == null || pool.Count == 0)
            {
                Add(issues, "stage.summon_pool.minimum", $"{Label(stage)} requires at least one summon entry.", stage);
                return;
            }

            long totalWeight = 0;
            var towerIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < pool.Count; index++)
            {
                TowerPoolEntryData? entry = pool[index];
                if (entry == null)
                {
                    Add(issues, "stage.summon_pool.entry_required", $"{Label(stage)} summon entry {index} is missing.", stage);
                    continue;
                }

                if (entry.Tower == null)
                {
                    Add(issues, "stage.summon_pool.tower_required", $"{Label(stage)} summon entry {index} requires a tower.", stage);
                }
                else if (!string.IsNullOrWhiteSpace(entry.Tower.Id) && !towerIds.Add(entry.Tower.Id))
                {
                    Add(issues, "stage.summon_pool.duplicate_tower", $"{Label(stage)} repeats tower ID '{entry.Tower.Id}' in its summon pool.", stage);
                }

                if (entry.Weight <= 0)
                {
                    Add(issues, "stage.summon_pool.weight", $"{Label(stage)} summon entry {index} weight must be positive.", stage);
                    continue;
                }

                totalWeight += entry.Weight;
                if (totalWeight > int.MaxValue)
                {
                    Add(issues, "stage.summon_pool.weight_total", $"{Label(stage)} summon weights exceed Int32.MaxValue.", stage);
                    return;
                }
            }
        }

        private static void ValidateMaximumCurrency(StageDefinitionAsset stage, List<DataValidationIssue> issues)
        {
            if (stage.Economy == null || stage.Economy.StartingCurrency < 0 || stage.Waves == null)
            {
                return;
            }

            long maximumCurrency = stage.Economy.StartingCurrency;
            try
            {
                foreach (WaveDefinitionAsset? wave in stage.Waves)
                {
                    if (wave?.Enemy == null || wave.EnemyCount <= 0 || wave.Enemy.KillReward < 0)
                    {
                        continue;
                    }

                    maximumCurrency = checked(maximumCurrency + ((long)wave.EnemyCount * wave.Enemy.KillReward));
                }
            }
            catch (OverflowException)
            {
                Add(issues, "stage.currency.maximum", $"{Label(stage)} maximum currency exceeds Int32.MaxValue.", stage);
                return;
            }

            if (maximumCurrency > int.MaxValue)
            {
                Add(issues, "stage.currency.maximum", $"{Label(stage)} maximum currency exceeds Int32.MaxValue.", stage);
            }
        }

        private static void ValidateDuplicateDefinitionIds(
            IReadOnlyList<DefinitionAsset> definitions,
            List<DataValidationIssue> issues)
        {
            var countsById = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (DefinitionAsset? definition in definitions)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
                {
                    continue;
                }

                countsById.TryGetValue(definition.Id, out int count);
                countsById[definition.Id] = count + 1;
            }

            foreach (DefinitionAsset? definition in definitions)
            {
                if (definition == null ||
                    string.IsNullOrWhiteSpace(definition.Id) ||
                    countsById[definition.Id] < 2)
                {
                    continue;
                }

                Add(issues, "id.duplicate", $"Definition ID '{definition.Id}' is duplicated.", definition);
            }
        }

        private static void ValidateReferences(
            DefinitionAsset definition,
            HashSet<DefinitionAsset> knownDefinitions,
            List<DataValidationIssue> issues)
        {
            switch (definition)
            {
                case TowerDefinitionAsset tower:
                    ValidateKnownReference(tower, tower.Projectile, "tower.projectile.outside_dataset", "projectile", knownDefinitions, issues);
                    break;

                case WaveDefinitionAsset wave:
                    ValidateKnownReference(wave, wave.Enemy, "wave.enemy.outside_dataset", "enemy", knownDefinitions, issues);
                    break;

                case StageDefinitionAsset stage:
                    ValidateKnownReference(stage, stage.Economy, "stage.economy.outside_dataset", "economy", knownDefinitions, issues);
                    if (stage.Waves != null)
                    {
                        foreach (WaveDefinitionAsset? wave in stage.Waves)
                        {
                            ValidateKnownReference(stage, wave, "stage.wave.outside_dataset", "wave", knownDefinitions, issues);
                        }
                    }

                    if (stage.SummonPool != null)
                    {
                        foreach (TowerPoolEntryData? entry in stage.SummonPool)
                        {
                            ValidateKnownReference(stage, entry?.Tower, "stage.summon_pool.outside_dataset", "tower", knownDefinitions, issues);
                        }
                    }

                    break;
            }
        }

        private static void ValidateKnownReference(
            DefinitionAsset source,
            DefinitionAsset? reference,
            string code,
            string role,
            HashSet<DefinitionAsset> knownDefinitions,
            List<DataValidationIssue> issues)
        {
            if (reference != null && !knownDefinitions.Contains(reference))
            {
                Add(issues, code, $"{Label(source)} references {role} '{reference.Id}' outside the data set.", source);
            }
        }

        private static void ValidateId(
            string? id,
            string codePrefix,
            string fieldName,
            DefinitionAsset source,
            List<DataValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Add(issues, $"{codePrefix}.required", $"{fieldName} is required on {Label(source)}.", source);
                return;
            }

            if (!IsValidId(id))
            {
                Add(issues, $"{codePrefix}.format", $"{fieldName} '{id}' must use lowercase letters, digits, and underscores.", source);
            }
        }

        private static bool IsValidId(string id)
        {
            if (id.Length == 0 || id[0] < 'a' || id[0] > 'z')
            {
                return false;
            }

            for (int index = 1; index < id.Length; index++)
            {
                char value = id[index];
                bool isLowercase = value >= 'a' && value <= 'z';
                bool isDigit = value >= '0' && value <= '9';
                if (!isLowercase && !isDigit && value != '_')
                {
                    return false;
                }
            }

            return true;
        }

        private static void ValidatePositive(
            float value,
            string code,
            string fieldName,
            DefinitionAsset source,
            List<DataValidationIssue> issues)
        {
            if (!IsFinite(value) || value <= 0f)
            {
                Add(issues, code, $"{fieldName} must be finite and positive on {Label(source)}.", source);
            }
        }

        private static void ValidateNonNegative(
            float value,
            string code,
            string fieldName,
            DefinitionAsset source,
            List<DataValidationIssue> issues)
        {
            if (!IsFinite(value) || value < 0f)
            {
                Add(issues, code, $"{fieldName} must be finite and non-negative on {Label(source)}.", source);
            }
        }

        private static void ValidatePositive(
            int value,
            string code,
            string fieldName,
            DefinitionAsset source,
            List<DataValidationIssue> issues)
        {
            if (value <= 0)
            {
                Add(issues, code, $"{fieldName} must be positive on {Label(source)}.", source);
            }
        }

        private static void ValidateNonNegative(
            int value,
            string code,
            string fieldName,
            DefinitionAsset source,
            List<DataValidationIssue> issues)
        {
            if (value < 0)
            {
                Add(issues, code, $"{fieldName} must not be negative on {Label(source)}.", source);
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static string Label(DefinitionAsset definition)
        {
            return string.IsNullOrWhiteSpace(definition.Id)
                ? definition.GetType().Name
                : $"{definition.GetType().Name} '{definition.Id}'";
        }

        private static void Add(
            List<DataValidationIssue> issues,
            string code,
            string message,
            DefinitionAsset? source)
        {
            issues.Add(new DataValidationIssue(code, message, source));
        }
    }
}
