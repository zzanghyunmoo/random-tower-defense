#nullable enable

using System;
using System.Collections.Generic;
using RandomTowerDefense.Core.Combat;
using RandomTowerDefense.Core.Enemies;

namespace RandomTowerDefense.Core.Towers
{
    public static class TargetSelector
    {
        public static EnemyState? Select(
            Point2 origin,
            float range,
            IReadOnlyList<EnemyState> enemies)
        {
            Guard.NonNegative(range, nameof(range));
            if (enemies == null)
            {
                throw new ArgumentNullException(nameof(enemies));
            }

            double rangeSquared = (double)range * range;
            EnemyState? bestTarget = null;

            for (int index = 0; index < enemies.Count; index++)
            {
                EnemyState enemy = enemies[index]
                    ?? throw new ArgumentException("Enemy collection must not contain null.", nameof(enemies));

                if (!enemy.IsAlive || origin.DistanceSquaredTo(enemy.Position) > rangeSquared)
                {
                    continue;
                }

                if (bestTarget == null
                    || enemy.PathProgress > bestTarget.PathProgress
                    || (enemy.PathProgress.Equals(bestTarget.PathProgress)
                        && enemy.SpawnOrder < bestTarget.SpawnOrder))
                {
                    bestTarget = enemy;
                }
            }

            return bestTarget;
        }
    }
}
