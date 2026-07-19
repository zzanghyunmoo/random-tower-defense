using System;
using RandomTowerDefense.Core.Combat;

namespace RandomTowerDefense.Core.Towers
{
    public sealed class TowerSummonPoolEntry
    {
        public TowerSummonPoolEntry(TowerDefinition tower, int weight)
        {
            Tower = tower ?? throw new ArgumentNullException(nameof(tower));
            Weight = Guard.Positive(weight, nameof(weight));
        }

        public TowerDefinition Tower { get; }

        public int Weight { get; }
    }
}
