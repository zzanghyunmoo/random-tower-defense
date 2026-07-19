#nullable enable

using System;
using System.Collections.Generic;
using RandomTowerDefense.Core.Combat;
using RandomTowerDefense.Core.Enemies;
using RandomTowerDefense.Core.Towers;
using RandomTowerDefense.Core.Waves;
using RandomTowerDefense.Data.Definitions;

namespace RandomTowerDefense.Data.Runtime
{
    public static class GameDataMapper
    {
        public static GameSessionDefinition ToCore(StageDefinitionAsset stage)
        {
            if (stage == null)
            {
                throw new ArgumentNullException(nameof(stage));
            }

            EconomyDefinitionAsset economy = stage.Economy
                ?? throw new ArgumentException("Stage economy reference is required.", nameof(stage));

            var pathPoints = new Point2[stage.PathPoints.Count];
            for (int index = 0; index < stage.PathPoints.Count; index++)
            {
                pathPoints[index] = new Point2(
                    stage.PathPoints[index].x,
                    stage.PathPoints[index].y);
            }

            var waves = new List<WaveDefinition>(stage.Waves.Count);
            foreach (WaveDefinitionAsset? waveAsset in stage.Waves)
            {
                if (waveAsset == null)
                {
                    throw new ArgumentException("Stage wave references must not contain null.", nameof(stage));
                }

                EnemyDefinitionAsset enemyAsset = waveAsset.Enemy
                    ?? throw new ArgumentException(
                        $"Wave '{waveAsset.Id}' requires an enemy reference.",
                        nameof(stage));
                var enemy = new EnemyDefinition(
                    enemyAsset.Id,
                    enemyAsset.MaxHealth,
                    enemyAsset.MoveSpeed,
                    enemyAsset.EndpointDamage,
                    enemyAsset.KillReward);
                waves.Add(new WaveDefinition(
                    waveAsset.Id,
                    enemy,
                    waveAsset.EnemyCount,
                    waveAsset.SpawnIntervalSeconds));
            }

            var slots = new List<TowerSlotDefinition>(stage.TowerSlots.Count);
            foreach (TowerSlotData? slot in stage.TowerSlots)
            {
                if (slot == null)
                {
                    throw new ArgumentException("Stage tower slots must not contain null.", nameof(stage));
                }

                slots.Add(new TowerSlotDefinition(
                    slot.Id,
                    new Point2(slot.Position.x, slot.Position.y),
                    slot.PlacementOrder));
            }

            var summonPool = new List<TowerSummonPoolEntry>(stage.SummonPool.Count);
            foreach (TowerPoolEntryData? entry in stage.SummonPool)
            {
                if (entry == null)
                {
                    throw new ArgumentException("Stage summon pool must not contain null.", nameof(stage));
                }

                TowerDefinitionAsset towerAsset = entry.Tower
                    ?? throw new ArgumentException("Summon pool tower reference is required.", nameof(stage));
                ProjectileDefinitionAsset projectileAsset = towerAsset.Projectile
                    ?? throw new ArgumentException(
                        $"Tower '{towerAsset.Id}' requires a projectile reference.",
                        nameof(stage));
                var tower = new TowerDefinition(
                    towerAsset.Id,
                    towerAsset.Range,
                    towerAsset.AttackIntervalSeconds,
                    projectileAsset.Speed,
                    towerAsset.Damage);
                summonPool.Add(new TowerSummonPoolEntry(tower, entry.Weight));
            }

            return new GameSessionDefinition(
                stage.Id,
                new Path2D(pathPoints),
                stage.StartingHealth,
                economy.StartingCurrency,
                economy.SummonCost,
                waves,
                slots,
                summonPool);
        }
    }
}
