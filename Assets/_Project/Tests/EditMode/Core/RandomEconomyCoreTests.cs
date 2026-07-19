using System;
using NUnit.Framework;
using RandomTowerDefense.Core.Economy;
using RandomTowerDefense.Core.Random;

namespace RandomTowerDefense.Tests.EditMode.Core
{
    public sealed class RandomEconomyCoreTests
    {
        [Test]
        public void EconomyState_RejectsInvalidAmountsAndOverflow()
        {
            var economy = new EconomyState(5);

            Assert.That(economy.TrySpend(6), Is.False);
            Assert.That(economy.TrySpend(5), Is.True);
            economy.Credit(3);

            Assert.That(economy.Balance, Is.EqualTo(3));
            Assert.That(economy.CanAfford(3), Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => new EconomyState(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => economy.CanAfford(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => economy.Credit(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => economy.TrySpend(-1));
            Assert.Throws<InvalidOperationException>(() => new EconomyState(int.MaxValue).Credit(1));
        }

        [Test]
        public void SeededRandomSource_IsBoundedAndReproducible()
        {
            var first = new SeededRandomSource(1234);
            var second = new SeededRandomSource(1234);
            int[] expected = { 0, 3, 1, 3, 1, 6, 1, 0, 3, 4, 3, 1, 1, 3, 6, 1 };
            var firstValues = new int[16];
            var secondValues = new int[16];

            for (int index = 0; index < firstValues.Length; index++)
            {
                firstValues[index] = first.NextInt(7);
                secondValues[index] = second.NextInt(7);
            }

            Assert.That(firstValues, Is.EqualTo(secondValues));
            Assert.That(firstValues, Is.EqualTo(expected));
            Assert.That(firstValues, Has.All.InRange(0, 6));
            Assert.Throws<ArgumentOutOfRangeException>(() => first.NextInt(0));
        }
    }
}
