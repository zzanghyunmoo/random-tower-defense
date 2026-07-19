using RandomTowerDefense.Core.Combat;

namespace RandomTowerDefense.Core.Enemies
{
    public sealed class EnemyDefinition
    {
        public EnemyDefinition(
            string id,
            float maxHealth,
            float moveSpeed,
            int endpointDamage,
            int killReward)
        {
            Id = Guard.DefinitionId(id, nameof(id));
            MaxHealth = Guard.Positive(maxHealth, nameof(maxHealth));
            MoveSpeed = Guard.NonNegative(moveSpeed, nameof(moveSpeed));
            EndpointDamage = Guard.NonNegative(endpointDamage, nameof(endpointDamage));
            KillReward = Guard.NonNegative(killReward, nameof(killReward));
        }

        public string Id { get; }

        public float MaxHealth { get; }

        public float MoveSpeed { get; }

        public int EndpointDamage { get; }

        public int KillReward { get; }
    }
}
