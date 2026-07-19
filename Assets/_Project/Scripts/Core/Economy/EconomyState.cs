using System;
using RandomTowerDefense.Core.Combat;

namespace RandomTowerDefense.Core.Economy
{
    public sealed class EconomyState
    {
        public EconomyState(int startingCurrency)
        {
            Balance = Guard.NonNegative(startingCurrency, nameof(startingCurrency));
        }

        public int Balance { get; private set; }

        public bool CanAfford(int amount)
        {
            Guard.NonNegative(amount, nameof(amount));
            return Balance >= amount;
        }

        public bool TrySpend(int amount)
        {
            Guard.NonNegative(amount, nameof(amount));
            if (Balance < amount)
            {
                return false;
            }

            Balance -= amount;
            return true;
        }

        public void Credit(int amount)
        {
            Guard.NonNegative(amount, nameof(amount));
            try
            {
                Balance = checked(Balance + amount);
            }
            catch (OverflowException exception)
            {
                throw new InvalidOperationException("Currency balance exceeded Int32.MaxValue.", exception);
            }
        }
    }
}
