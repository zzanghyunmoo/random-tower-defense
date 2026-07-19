#nullable enable

using System;
using System.Collections.Generic;
using RandomTowerDefense.Core.Combat;

namespace RandomTowerDefense.Core.Towers
{
    public sealed class TowerGrid
    {
        private readonly TowerSlotDefinition[] _slots;
        private readonly TowerState?[] _towers;

        public TowerGrid(IReadOnlyList<TowerSlotDefinition> slots)
        {
            if (slots == null)
            {
                throw new ArgumentNullException(nameof(slots));
            }

            _slots = new TowerSlotDefinition[slots.Count];
            _towers = new TowerState?[slots.Count];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var placementOrders = new HashSet<long>();
            for (int index = 0; index < slots.Count; index++)
            {
                TowerSlotDefinition slot = slots[index]
                    ?? throw new ArgumentException("Slot collection must not contain null.", nameof(slots));
                if (!ids.Add(slot.Id))
                {
                    throw new ArgumentException($"Duplicate tower slot ID '{slot.Id}'.", nameof(slots));
                }

                if (!placementOrders.Add(slot.PlacementOrder))
                {
                    throw new ArgumentException(
                        $"Duplicate tower slot placement order '{slot.PlacementOrder}'.",
                        nameof(slots));
                }

                _slots[index] = slot;
            }

            EmptySlotCount = slots.Count;
        }

        public int SlotCount => _slots.Length;

        public int EmptySlotCount { get; private set; }

        public TowerSlotDefinition GetSlot(int index)
        {
            ValidateIndex(index);
            return _slots[index];
        }

        public TowerState? GetTower(int index)
        {
            ValidateIndex(index);
            return _towers[index];
        }

        public IReadOnlyList<TowerState> GetOccupiedTowers()
        {
            var occupied = new List<TowerState>(_towers.Length - EmptySlotCount);
            foreach (TowerState? tower in _towers)
            {
                if (tower != null)
                {
                    occupied.Add(tower);
                }
            }

            return occupied;
        }

        internal int GetEmptySlotIndex(int emptySlotOrdinal)
        {
            if (emptySlotOrdinal < 0 || emptySlotOrdinal >= EmptySlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(emptySlotOrdinal));
            }

            int currentOrdinal = 0;
            for (int index = 0; index < _towers.Length; index++)
            {
                if (_towers[index] != null)
                {
                    continue;
                }

                if (currentOrdinal == emptySlotOrdinal)
                {
                    return index;
                }

                currentOrdinal++;
            }

            throw new InvalidOperationException("Empty slot count did not match grid contents.");
        }

        internal TowerState Place(int slotIndex, TowerDefinition definition)
        {
            ValidateIndex(slotIndex);
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (_towers[slotIndex] != null)
            {
                throw new InvalidOperationException($"Tower slot '{_slots[slotIndex].Id}' is already occupied.");
            }

            TowerSlotDefinition slot = _slots[slotIndex];
            var tower = new TowerState(definition, slot.Position, slot.PlacementOrder);
            _towers[slotIndex] = tower;
            EmptySlotCount--;
            return tower;
        }

        private void ValidateIndex(int index)
        {
            if (index < 0 || index >= _slots.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }
}
