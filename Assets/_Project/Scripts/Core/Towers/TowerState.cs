#nullable enable

using System;
using RandomTowerDefense.Core.Combat;

namespace RandomTowerDefense.Core.Towers
{
    public sealed class TowerState
    {
        public TowerState(TowerDefinition definition, Point2 position, long placementOrder)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Position = position;
            PlacementOrder = Guard.NonNegative(placementOrder, nameof(placementOrder));
        }

        public TowerDefinition Definition { get; }

        public Point2 Position { get; }

        public long PlacementOrder { get; }

        public float CooldownRemaining { get; private set; }

        internal int AdvanceAttackClock(float deltaSeconds, bool hasTarget)
        {
            Guard.NonNegative(deltaSeconds, nameof(deltaSeconds));
            if (deltaSeconds == 0f)
            {
                return 0;
            }

            double remaining = CooldownRemaining - deltaSeconds;
            if (!hasTarget)
            {
                CooldownRemaining = (float)Math.Max(0d, remaining);
                return 0;
            }

            if (remaining > 0d)
            {
                CooldownRemaining = (float)remaining;
                return 0;
            }

            double interval = Definition.AttackIntervalSeconds;
            double overdue = -remaining;
            double attackCount = Math.Floor(overdue / interval) + 1d;
            if (attackCount > int.MaxValue)
            {
                throw new InvalidOperationException("A single advance cannot schedule more than Int32.MaxValue attacks.");
            }

            int scheduledAttackCount = (int)attackCount;
            double elapsedIntoNextInterval = overdue - ((scheduledAttackCount - 1d) * interval);
            CooldownRemaining = (float)(interval - elapsedIntoNextInterval);
            return scheduledAttackCount;
        }
    }
}
