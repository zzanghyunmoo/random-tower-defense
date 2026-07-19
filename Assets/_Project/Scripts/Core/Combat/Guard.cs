#nullable enable

using System;

namespace RandomTowerDefense.Core.Combat
{
    internal static class Guard
    {
        public static string DefinitionId(string value, string parameterName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            if (value.Length == 0)
            {
                throw new ArgumentException("Definition ID must not be empty.", parameterName);
            }

            foreach (char character in value)
            {
                bool isLowercaseLetter = character >= 'a' && character <= 'z';
                bool isDigit = character >= '0' && character <= '9';
                if (!isLowercaseLetter && !isDigit && character != '_')
                {
                    throw new ArgumentException(
                        "Definition ID must contain only lowercase letters, digits, and underscores.",
                        parameterName);
                }
            }

            return value;
        }

        public static float Finite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite.");
            }

            return value;
        }

        public static float NonNegative(float value, string parameterName)
        {
            Finite(value, parameterName);
            if (value < 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value must not be negative.");
            }

            return value;
        }

        public static float Positive(float value, string parameterName)
        {
            Finite(value, parameterName);
            if (value <= 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value must be positive.");
            }

            return value;
        }

        public static int NonNegative(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value must not be negative.");
            }

            return value;
        }

        public static int Positive(int value, string parameterName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value must be positive.");
            }

            return value;
        }

        public static long NonNegative(long value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value must not be negative.");
            }

            return value;
        }
    }
}
