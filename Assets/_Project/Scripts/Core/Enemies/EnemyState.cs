#nullable enable

using System;
using RandomTowerDefense.Core.Combat;

namespace RandomTowerDefense.Core.Enemies
{
    public sealed class EnemyState
    {
        public EnemyState(EnemyDefinition definition, Path2D path, long spawnOrder)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Path = path ?? throw new ArgumentNullException(nameof(path));
            SpawnOrder = Guard.NonNegative(spawnOrder, nameof(spawnOrder));
            CurrentHealth = definition.MaxHealth;
            Status = EnemyStatus.Alive;
        }

        public EnemyDefinition Definition { get; }

        public Path2D Path { get; }

        public long SpawnOrder { get; }

        public float CurrentHealth { get; private set; }

        public float DistanceTravelled { get; private set; }

        public float PathProgress => DistanceTravelled / Path.Length;

        public Point2 Position => Path.GetPosition(DistanceTravelled);

        public EnemyStatus Status { get; private set; }

        public bool IsAlive => Status == EnemyStatus.Alive;

        public EnemyAdvanceResult Advance(float deltaSeconds)
        {
            Guard.NonNegative(deltaSeconds, nameof(deltaSeconds));
            if (!IsAlive || deltaSeconds == 0f || Definition.MoveSpeed == 0f)
            {
                return EnemyAdvanceResult.NoChange;
            }

            float previousDistance = DistanceTravelled;
            double nextDistance = previousDistance + ((double)Definition.MoveSpeed * deltaSeconds);
            DistanceTravelled = nextDistance >= Path.Length ? Path.Length : (float)nextDistance;

            bool reachedEndpoint = DistanceTravelled >= Path.Length;
            if (reachedEndpoint)
            {
                Status = EnemyStatus.ReachedEndpoint;
            }

            return new EnemyAdvanceResult(
                DistanceTravelled - previousDistance,
                reachedEndpoint,
                reachedEndpoint ? Definition.EndpointDamage : 0);
        }

        public DamageResult ApplyDamage(float damage)
        {
            Guard.NonNegative(damage, nameof(damage));
            if (!IsAlive || damage == 0f)
            {
                return DamageResult.Ignored;
            }

            float appliedDamage = Math.Min(CurrentHealth, damage);
            CurrentHealth -= appliedDamage;
            if (CurrentHealth > 0f)
            {
                return new DamageResult(appliedDamage, false, 0);
            }

            CurrentHealth = 0f;
            Status = EnemyStatus.Dead;
            return new DamageResult(appliedDamage, true, Definition.KillReward);
        }
    }
}
