#nullable enable

using System;
using System.Collections.Generic;
using NUnit.Framework;
using RandomTowerDefense.Core.Combat;
using RandomTowerDefense.Core.Economy;
using RandomTowerDefense.Core.Random;
using RandomTowerDefense.Core.Towers;

namespace RandomTowerDefense.Tests.EditMode.Core
{
    public sealed class TowerSummonCoreTests
    {
        [Test]
        public void TrySummon_SpendsCostAndPlacesOneWeightedTower()
        {
            TowerDefinition common = CreateTower("tower_common");
            TowerDefinition rare = CreateTower("tower_rare");
            var random = new SequenceRandomSource(1, 0);
            var system = new TowerSummonSystem(
                random,
                summonCost: 10,
                new[]
                {
                    new TowerSummonPoolEntry(common, 1),
                    new TowerSummonPoolEntry(rare, 2)
                });
            var economy = new EconomyState(10);
            TowerGrid grid = CreateGrid("slot_a");

            TowerSummonResult result = system.TrySummon(true, economy, grid);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Failure, Is.EqualTo(TowerSummonFailure.None));
            Assert.That(result.SlotId, Is.EqualTo("slot_a"));
            Assert.That(result.Tower, Is.SameAs(grid.GetTower(0)));
            Assert.That(result.Tower!.Definition, Is.SameAs(rare));
            Assert.That(result.CostSpent, Is.EqualTo(10));
            Assert.That(economy.Balance, Is.Zero);
            Assert.That(grid.EmptySlotCount, Is.Zero);
            Assert.That(random.RequestedBounds, Is.EqualTo(new[] { 3, 1 }));
        }

        [Test]
        public void TrySummon_FailuresDoNotMutateEconomyGridOrRandom()
        {
            TowerDefinition tower = CreateTower("tower_test");
            var pool = new[] { new TowerSummonPoolEntry(tower, 1) };

            AssertFailedWithoutMutation(
                expectedFailure: TowerSummonFailure.SessionClosed,
                sessionActive: false,
                economy: new EconomyState(10),
                grid: CreateGrid("slot_a"),
                pool);
            AssertFailedWithoutMutation(
                expectedFailure: TowerSummonFailure.InsufficientCurrency,
                sessionActive: true,
                economy: new EconomyState(9),
                grid: CreateGrid("slot_a"),
                pool);
            AssertFailedWithoutMutation(
                expectedFailure: TowerSummonFailure.NoEmptySlot,
                sessionActive: true,
                economy: new EconomyState(10),
                grid: CreateGrid(),
                pool);
            AssertFailedWithoutMutation(
                expectedFailure: TowerSummonFailure.EmptyPool,
                sessionActive: true,
                economy: new EconomyState(10),
                grid: CreateGrid("slot_a"),
                Array.Empty<TowerSummonPoolEntry>());
        }

        [Test]
        public void FailedCommand_DoesNotConsumeSeededRandomState()
        {
            TowerDefinition first = CreateTower("tower_first");
            TowerDefinition second = CreateTower("tower_second");
            var pool = new[]
            {
                new TowerSummonPoolEntry(first, 1),
                new TowerSummonPoolEntry(second, 1)
            };
            var afterFailureSystem = new TowerSummonSystem(new SeededRandomSource(27), 10, pool);
            var freshSystem = new TowerSummonSystem(new SeededRandomSource(27), 10, pool);
            var afterFailureEconomy = new EconomyState(0);
            var freshEconomy = new EconomyState(10);
            TowerGrid afterFailureGrid = CreateGrid("slot_a", "slot_b");
            TowerGrid freshGrid = CreateGrid("slot_a", "slot_b");

            TowerSummonResult failed = afterFailureSystem.TrySummon(true, afterFailureEconomy, afterFailureGrid);
            afterFailureEconomy.Credit(10);
            TowerSummonResult afterFailure = afterFailureSystem.TrySummon(true, afterFailureEconomy, afterFailureGrid);
            TowerSummonResult fresh = freshSystem.TrySummon(true, freshEconomy, freshGrid);

            Assert.That(failed.Failure, Is.EqualTo(TowerSummonFailure.InsufficientCurrency));
            Assert.That(afterFailure.Tower!.Definition.Id, Is.EqualTo(fresh.Tower!.Definition.Id));
            Assert.That(afterFailure.SlotId, Is.EqualTo(fresh.SlotId));
        }

        [Test]
        public void SameSeedAndCommands_ReproduceTowerAndSlotSequence()
        {
            string[] firstRun = RunSummonSequence(seed: 991);
            string[] secondRun = RunSummonSequence(seed: 991);

            Assert.That(firstRun, Is.EqualTo(secondRun));
        }

        [Test]
        public void FullGridFailure_DoesNotSpendOrDrawAgain()
        {
            TowerDefinition tower = CreateTower("tower_test");
            var random = new SequenceRandomSource(0, 0);
            var system = new TowerSummonSystem(
                random,
                summonCost: 10,
                new[] { new TowerSummonPoolEntry(tower, 1) });
            var economy = new EconomyState(30);
            TowerGrid grid = CreateGrid("slot_a");

            TowerSummonResult first = system.TrySummon(true, economy, grid);
            TowerSummonResult second = system.TrySummon(true, economy, grid);

            Assert.That(first.Succeeded, Is.True);
            Assert.That(second.Failure, Is.EqualTo(TowerSummonFailure.NoEmptySlot));
            Assert.That(economy.Balance, Is.EqualTo(20));
            Assert.That(random.RequestedBounds, Has.Count.EqualTo(2));
        }

        [Test]
        public void InvalidRandomResult_DoesNotSpendOrPlace()
        {
            TowerDefinition tower = CreateTower("tower_test");
            var random = new SequenceRandomSource(0, 1);
            var system = new TowerSummonSystem(
                random,
                summonCost: 10,
                new[] { new TowerSummonPoolEntry(tower, 1) });
            var economy = new EconomyState(10);
            TowerGrid grid = CreateGrid("slot_a");

            Assert.Throws<InvalidOperationException>(() => system.TrySummon(true, economy, grid));
            Assert.That(economy.Balance, Is.EqualTo(10));
            Assert.That(grid.EmptySlotCount, Is.EqualTo(1));
            Assert.That(grid.GetTower(0), Is.Null);
            Assert.That(random.RequestedBounds, Is.EqualTo(new[] { 1, 1 }));
        }

        [Test]
        public void InvalidGridAndPoolDefinitions_FailClearly()
        {
            TowerDefinition tower = CreateTower("tower_test");
            var duplicateSlots = new[]
            {
                new TowerSlotDefinition("slot_same", new Point2(0f, 0f), 0),
                new TowerSlotDefinition("slot_same", new Point2(1f, 0f), 1)
            };
            var duplicateOrders = new[]
            {
                new TowerSlotDefinition("slot_a", new Point2(0f, 0f), 0),
                new TowerSlotDefinition("slot_b", new Point2(1f, 0f), 0)
            };

            Assert.Throws<ArgumentException>(() => new TowerGrid(duplicateSlots));
            Assert.Throws<ArgumentException>(() => new TowerGrid(duplicateOrders));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TowerSummonPoolEntry(tower, 0));
            Assert.Throws<ArgumentException>(() => new TowerSummonSystem(
                new SeededRandomSource(1),
                10,
                new[]
                {
                    new TowerSummonPoolEntry(tower, 1),
                    new TowerSummonPoolEntry(tower, 2)
                }));
            Assert.Throws<ArgumentException>(() => new TowerSummonSystem(
                new SeededRandomSource(1),
                10,
                new[]
                {
                    new TowerSummonPoolEntry(CreateTower("tower_heavy"), int.MaxValue),
                    new TowerSummonPoolEntry(CreateTower("tower_extra"), 1)
                }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TowerSummonSystem(
                new SeededRandomSource(1),
                -1,
                Array.Empty<TowerSummonPoolEntry>()));
        }

        private static void AssertFailedWithoutMutation(
            TowerSummonFailure expectedFailure,
            bool sessionActive,
            EconomyState economy,
            TowerGrid grid,
            IReadOnlyList<TowerSummonPoolEntry> pool)
        {
            var random = new SequenceRandomSource();
            var system = new TowerSummonSystem(random, summonCost: 10, pool);
            int originalBalance = economy.Balance;
            int originalEmptySlots = grid.EmptySlotCount;

            TowerSummonResult result = system.TrySummon(sessionActive, economy, grid);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure, Is.EqualTo(expectedFailure));
            Assert.That(result.Tower, Is.Null);
            Assert.That(result.SlotId, Is.Null);
            Assert.That(result.CostSpent, Is.Zero);
            Assert.That(economy.Balance, Is.EqualTo(originalBalance));
            Assert.That(grid.EmptySlotCount, Is.EqualTo(originalEmptySlots));
            Assert.That(random.RequestedBounds, Is.Empty);
        }

        private static string[] RunSummonSequence(ulong seed)
        {
            var pool = new[]
            {
                new TowerSummonPoolEntry(CreateTower("tower_arrow"), 3),
                new TowerSummonPoolEntry(CreateTower("tower_frost"), 2),
                new TowerSummonPoolEntry(CreateTower("tower_fire"), 1)
            };
            var system = new TowerSummonSystem(new SeededRandomSource(seed), 10, pool);
            var economy = new EconomyState(40);
            TowerGrid grid = CreateGrid("slot_a", "slot_b", "slot_c", "slot_d");
            var sequence = new string[4];
            for (int index = 0; index < sequence.Length; index++)
            {
                TowerSummonResult result = system.TrySummon(true, economy, grid);
                sequence[index] = $"{result.SlotId}:{result.Tower!.Definition.Id}";
            }

            return sequence;
        }

        private static TowerDefinition CreateTower(string id)
        {
            return new TowerDefinition(id, 5f, 1f, 4f, 2f);
        }

        private static TowerGrid CreateGrid(params string[] slotIds)
        {
            var slots = new TowerSlotDefinition[slotIds.Length];
            for (int index = 0; index < slotIds.Length; index++)
            {
                slots[index] = new TowerSlotDefinition(
                    slotIds[index],
                    new Point2(index, 0f),
                    placementOrder: index);
            }

            return new TowerGrid(slots);
        }

        private sealed class SequenceRandomSource : IRandomSource
        {
            private readonly Queue<int> _values;

            public SequenceRandomSource(params int[] values)
            {
                _values = new Queue<int>(values);
            }

            public List<int> RequestedBounds { get; } = new List<int>();

            public int NextInt(int exclusiveMaximum)
            {
                RequestedBounds.Add(exclusiveMaximum);
                if (_values.Count == 0)
                {
                    throw new InvalidOperationException("No deterministic random value remains.");
                }

                return _values.Dequeue();
            }
        }
    }
}
