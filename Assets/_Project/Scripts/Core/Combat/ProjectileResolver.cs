#nullable enable

using System;
using System.Collections.Generic;

namespace RandomTowerDefense.Core.Combat
{
    public static class ProjectileResolver
    {
        public static IReadOnlyList<ProjectileAdvanceResult> Advance(
            float deltaSeconds,
            IReadOnlyList<ProjectileState> projectiles)
        {
            Guard.NonNegative(deltaSeconds, nameof(deltaSeconds));
            if (projectiles == null)
            {
                throw new ArgumentNullException(nameof(projectiles));
            }

            var orderedProjectiles = new List<OrderedProjectile>(projectiles.Count);
            for (int index = 0; index < projectiles.Count; index++)
            {
                ProjectileState projectile = projectiles[index]
                    ?? throw new ArgumentException("Projectile collection must not contain null.", nameof(projectiles));
                orderedProjectiles.Add(new OrderedProjectile(projectile, index));
            }

            orderedProjectiles.Sort(OrderedProjectile.Compare);

            var results = new List<ProjectileAdvanceResult>(orderedProjectiles.Count);
            foreach (OrderedProjectile orderedProjectile in orderedProjectiles)
            {
                results.Add(orderedProjectile.Projectile.Advance(deltaSeconds));
            }

            return results;
        }

        private readonly struct OrderedProjectile
        {
            public OrderedProjectile(ProjectileState projectile, int inputIndex)
            {
                Projectile = projectile;
                InputIndex = inputIndex;
            }

            public ProjectileState Projectile { get; }

            public int InputIndex { get; }

            public static int Compare(OrderedProjectile left, OrderedProjectile right)
            {
                int orderComparison = left.Projectile.Order.CompareTo(right.Projectile.Order);
                return orderComparison != 0
                    ? orderComparison
                    : left.InputIndex.CompareTo(right.InputIndex);
            }
        }
    }
}
