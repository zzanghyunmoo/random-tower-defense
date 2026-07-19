using System;

namespace RandomTowerDefense.Core.Random
{
    public sealed class SeededRandomSource : IRandomSource
    {
        private const ulong Multiplier = 6364136223846793005UL;

        private ulong _state;
        private readonly ulong _increment;

        public SeededRandomSource(ulong seed, ulong stream = 54UL)
        {
            _increment = unchecked((stream << 1) | 1UL);
            NextUInt();
            _state = unchecked(_state + seed);
            NextUInt();
        }

        public int NextInt(int exclusiveMaximum)
        {
            if (exclusiveMaximum <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(exclusiveMaximum),
                    exclusiveMaximum,
                    "Exclusive maximum must be positive.");
            }

            uint bound = (uint)exclusiveMaximum;
            uint threshold = unchecked(0u - bound) % bound;
            uint value;
            do
            {
                value = NextUInt();
            }
            while (value < threshold);

            return (int)(value % bound);
        }

        private uint NextUInt()
        {
            ulong previousState = _state;
            _state = unchecked((previousState * Multiplier) + _increment);
            uint shifted = (uint)(((previousState >> 18) ^ previousState) >> 27);
            int rotation = (int)(previousState >> 59);
            return (shifted >> rotation) | (shifted << ((-rotation) & 31));
        }
    }
}
