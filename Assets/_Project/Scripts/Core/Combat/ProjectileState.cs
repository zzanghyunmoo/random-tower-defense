#nullable enable

using System;
using RandomTowerDefense.Core.Enemies;

namespace RandomTowerDefense.Core.Combat
{
    public sealed class ProjectileState
    {
        public ProjectileState(
            long order,
            long sourceTowerOrder,
            Point2 origin,
            EnemyState target,
            float speed,
            float damage)
        {
            Order = Guard.NonNegative(order, nameof(order));
            SourceTowerOrder = Guard.NonNegative(sourceTowerOrder, nameof(sourceTowerOrder));
            Position = origin;
            Target = target ?? throw new ArgumentNullException(nameof(target));
            Speed = Guard.Positive(speed, nameof(speed));
            Damage = Guard.Positive(damage, nameof(damage));
            Status = ProjectileStatus.Flying;
        }

        public long Order { get; }

        public long SourceTowerOrder { get; }

        public Point2 Position { get; private set; }

        public EnemyState Target { get; }

        public float Speed { get; }

        public float Damage { get; }

        public ProjectileStatus Status { get; private set; }

        public bool IsFlying => Status == ProjectileStatus.Flying;

        public ProjectileAdvanceResult Advance(float deltaSeconds)
        {
            Guard.NonNegative(deltaSeconds, nameof(deltaSeconds));
            if (!IsFlying)
            {
                return NoChange();
            }

            if (!Target.IsAlive)
            {
                Status = ProjectileStatus.TargetLost;
                return new ProjectileAdvanceResult(Order, 0f, true, Status, DamageResult.Ignored);
            }

            Point2 targetPosition = Target.Position;
            double distanceSquared = Position.DistanceSquaredTo(targetPosition);
            if (distanceSquared == 0d)
            {
                return HitTarget(targetPosition, 0f);
            }

            if (deltaSeconds == 0f)
            {
                return NoChange();
            }

            double distance = Math.Sqrt(distanceSquared);
            double travelDistance = (double)Speed * deltaSeconds;
            if (travelDistance >= distance)
            {
                return HitTarget(targetPosition, (float)distance);
            }

            Position = Point2.Lerp(Position, targetPosition, (float)(travelDistance / distance));
            return new ProjectileAdvanceResult(
                Order,
                (float)travelDistance,
                false,
                Status,
                DamageResult.Ignored);
        }

        private ProjectileAdvanceResult HitTarget(Point2 targetPosition, float distanceMoved)
        {
            Position = targetPosition;
            DamageResult damageResult = Target.ApplyDamage(Damage);
            Status = ProjectileStatus.Hit;
            return new ProjectileAdvanceResult(Order, distanceMoved, true, Status, damageResult);
        }

        private ProjectileAdvanceResult NoChange()
        {
            return new ProjectileAdvanceResult(Order, 0f, false, Status, DamageResult.Ignored);
        }
    }
}
