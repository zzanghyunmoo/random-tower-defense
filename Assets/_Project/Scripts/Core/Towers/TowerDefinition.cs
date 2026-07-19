#nullable enable

using RandomTowerDefense.Core.Combat;

namespace RandomTowerDefense.Core.Towers
{
    public sealed class TowerDefinition
    {
        public TowerDefinition(
            string id,
            float range,
            float attackIntervalSeconds,
            float projectileSpeed,
            float projectileDamage)
        {
            Id = Guard.DefinitionId(id, nameof(id));
            Range = Guard.NonNegative(range, nameof(range));
            AttackIntervalSeconds = Guard.Positive(attackIntervalSeconds, nameof(attackIntervalSeconds));
            ProjectileSpeed = Guard.Positive(projectileSpeed, nameof(projectileSpeed));
            ProjectileDamage = Guard.Positive(projectileDamage, nameof(projectileDamage));
        }

        public string Id { get; }

        public float Range { get; }

        public float AttackIntervalSeconds { get; }

        public float ProjectileSpeed { get; }

        public float ProjectileDamage { get; }
    }
}
