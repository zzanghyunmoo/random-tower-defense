#nullable enable

using System;
using System.Collections.Generic;
using RandomTowerDefense.Core.Combat;
using RandomTowerDefense.Core.Enemies;

namespace RandomTowerDefense.Core.Towers
{
    public sealed class TowerAttackSystem
    {
        private long _nextProjectileOrder;

        public TowerAttackSystem(long initialProjectileOrder = 0)
        {
            _nextProjectileOrder = Guard.NonNegative(initialProjectileOrder, nameof(initialProjectileOrder));
        }

        public long NextProjectileOrder => _nextProjectileOrder;

        public IReadOnlyList<ProjectileState> Advance(
            float deltaSeconds,
            IReadOnlyList<TowerState> towers,
            IReadOnlyList<EnemyState> enemies)
        {
            Guard.NonNegative(deltaSeconds, nameof(deltaSeconds));
            if (towers == null)
            {
                throw new ArgumentNullException(nameof(towers));
            }

            if (enemies == null)
            {
                throw new ArgumentNullException(nameof(enemies));
            }

            var orderedTowers = new List<OrderedTower>(towers.Count);
            for (int index = 0; index < towers.Count; index++)
            {
                TowerState tower = towers[index]
                    ?? throw new ArgumentException("Tower collection must not contain null.", nameof(towers));
                orderedTowers.Add(new OrderedTower(tower, index));
            }

            orderedTowers.Sort(OrderedTower.Compare);

            var projectiles = new List<ProjectileState>();
            foreach (OrderedTower orderedTower in orderedTowers)
            {
                TowerState tower = orderedTower.Tower;
                EnemyState? target = TargetSelector.Select(
                    tower.Position,
                    tower.Definition.Range,
                    enemies);
                int attackCount = tower.AdvanceAttackClock(deltaSeconds, target != null);
                for (int attackIndex = 0; attackIndex < attackCount; attackIndex++)
                {
                    if (_nextProjectileOrder == long.MaxValue)
                    {
                        throw new InvalidOperationException("Projectile order has reached Int64.MaxValue.");
                    }

                    projectiles.Add(new ProjectileState(
                        _nextProjectileOrder,
                        tower.PlacementOrder,
                        tower.Position,
                        target!,
                        tower.Definition.ProjectileSpeed,
                        tower.Definition.ProjectileDamage));
                    _nextProjectileOrder++;
                }
            }

            return projectiles;
        }

        private readonly struct OrderedTower
        {
            public OrderedTower(TowerState tower, int inputIndex)
            {
                Tower = tower;
                InputIndex = inputIndex;
            }

            public TowerState Tower { get; }

            public int InputIndex { get; }

            public static int Compare(OrderedTower left, OrderedTower right)
            {
                int placementComparison = left.Tower.PlacementOrder.CompareTo(right.Tower.PlacementOrder);
                return placementComparison != 0
                    ? placementComparison
                    : left.InputIndex.CompareTo(right.InputIndex);
            }
        }
    }
}
