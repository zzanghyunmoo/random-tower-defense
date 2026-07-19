#nullable enable

using System;

namespace RandomTowerDefense.Core.Combat
{
    public readonly struct Point2 : IEquatable<Point2>
    {
        public Point2(float x, float y)
        {
            X = Guard.Finite(x, nameof(x));
            Y = Guard.Finite(y, nameof(y));
        }

        public float X { get; }

        public float Y { get; }

        public double DistanceSquaredTo(Point2 other)
        {
            double deltaX = (double)other.X - X;
            double deltaY = (double)other.Y - Y;
            return (deltaX * deltaX) + (deltaY * deltaY);
        }

        public static Point2 Lerp(Point2 start, Point2 end, float amount)
        {
            float clampedAmount = Math.Max(0f, Math.Min(1f, Guard.Finite(amount, nameof(amount))));
            return new Point2(
                start.X + ((end.X - start.X) * clampedAmount),
                start.Y + ((end.Y - start.Y) * clampedAmount));
        }

        public bool Equals(Point2 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object? obj)
        {
            return obj is Point2 other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public static bool operator ==(Point2 left, Point2 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Point2 left, Point2 right)
        {
            return !left.Equals(right);
        }
    }
}
