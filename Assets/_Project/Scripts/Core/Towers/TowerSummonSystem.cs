#nullable enable

using System;
using System.Collections.Generic;
using RandomTowerDefense.Core.Combat;
using RandomTowerDefense.Core.Economy;
using RandomTowerDefense.Core.Random;

namespace RandomTowerDefense.Core.Towers
{
    public sealed class TowerSummonSystem
    {
        private readonly IRandomSource _randomSource;
        private readonly TowerSummonPoolEntry[] _pool;
        private readonly int _totalWeight;

        public TowerSummonSystem(
            IRandomSource randomSource,
            int summonCost,
            IReadOnlyList<TowerSummonPoolEntry> pool)
        {
            _randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
            SummonCost = Guard.NonNegative(summonCost, nameof(summonCost));
            if (pool == null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            _pool = new TowerSummonPoolEntry[pool.Count];
            var towerIds = new HashSet<string>(StringComparer.Ordinal);
            int totalWeight = 0;
            for (int index = 0; index < pool.Count; index++)
            {
                TowerSummonPoolEntry entry = pool[index]
                    ?? throw new ArgumentException("Summon pool must not contain null.", nameof(pool));
                if (!towerIds.Add(entry.Tower.Id))
                {
                    throw new ArgumentException(
                        $"Duplicate tower ID '{entry.Tower.Id}' in summon pool.",
                        nameof(pool));
                }

                try
                {
                    totalWeight = checked(totalWeight + entry.Weight);
                }
                catch (OverflowException exception)
                {
                    throw new ArgumentException("Summon pool weight total exceeded Int32.MaxValue.", nameof(pool), exception);
                }

                _pool[index] = entry;
            }

            _totalWeight = totalWeight;
        }

        public int SummonCost { get; }

        public TowerSummonResult TrySummon(
            bool sessionActive,
            EconomyState economy,
            TowerGrid grid)
        {
            if (economy == null)
            {
                throw new ArgumentNullException(nameof(economy));
            }

            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (!sessionActive)
            {
                return TowerSummonResult.Failed(TowerSummonFailure.SessionClosed);
            }

            if (!economy.CanAfford(SummonCost))
            {
                return TowerSummonResult.Failed(TowerSummonFailure.InsufficientCurrency);
            }

            if (grid.EmptySlotCount == 0)
            {
                return TowerSummonResult.Failed(TowerSummonFailure.NoEmptySlot);
            }

            if (_pool.Length == 0)
            {
                return TowerSummonResult.Failed(TowerSummonFailure.EmptyPool);
            }

            TowerDefinition selectedTower = SelectTower();
            int emptySlotOrdinal = NextValidatedIndex(grid.EmptySlotCount);
            int slotIndex = grid.GetEmptySlotIndex(emptySlotOrdinal);
            if (!economy.TrySpend(SummonCost))
            {
                throw new InvalidOperationException("Currency changed during a single summon command.");
            }

            TowerState tower = grid.Place(slotIndex, selectedTower);
            return TowerSummonResult.Success(grid.GetSlot(slotIndex).Id, tower, SummonCost);
        }

        private TowerDefinition SelectTower()
        {
            int roll = NextValidatedIndex(_totalWeight);
            int cumulativeWeight = 0;
            foreach (TowerSummonPoolEntry entry in _pool)
            {
                cumulativeWeight += entry.Weight;
                if (roll < cumulativeWeight)
                {
                    return entry.Tower;
                }
            }

            throw new InvalidOperationException("Summon pool weight did not contain the random roll.");
        }

        private int NextValidatedIndex(int exclusiveMaximum)
        {
            int value = _randomSource.NextInt(exclusiveMaximum);
            if (value < 0 || value >= exclusiveMaximum)
            {
                throw new InvalidOperationException(
                    $"Random source returned {value} outside [0, {exclusiveMaximum}).");
            }

            return value;
        }
    }
}
