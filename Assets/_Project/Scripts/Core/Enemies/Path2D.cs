#nullable enable

using System;
using System.Collections.Generic;
using RandomTowerDefense.Core.Combat;

namespace RandomTowerDefense.Core.Enemies
{
    public sealed class Path2D
    {
        private readonly Point2[] _waypoints;
        private readonly float[] _segmentLengths;

        public Path2D(IReadOnlyList<Point2> waypoints)
        {
            if (waypoints == null)
            {
                throw new ArgumentNullException(nameof(waypoints));
            }

            if (waypoints.Count < 2)
            {
                throw new ArgumentException("A path requires at least two waypoints.", nameof(waypoints));
            }

            _waypoints = new Point2[waypoints.Count];
            for (int index = 0; index < waypoints.Count; index++)
            {
                _waypoints[index] = waypoints[index];
            }

            _segmentLengths = new float[_waypoints.Length - 1];
            double totalLength = 0d;
            for (int index = 0; index < _segmentLengths.Length; index++)
            {
                double squaredLength = _waypoints[index].DistanceSquaredTo(_waypoints[index + 1]);
                if (squaredLength == 0d)
                {
                    throw new ArgumentException(
                        "Consecutive path waypoints must not overlap.",
                        nameof(waypoints));
                }

                double segmentLength = Math.Sqrt(squaredLength);
                totalLength += segmentLength;
                if (totalLength > float.MaxValue)
                {
                    throw new ArgumentException("Path length is too large.", nameof(waypoints));
                }

                _segmentLengths[index] = (float)segmentLength;
            }

            Length = (float)totalLength;
        }

        public float Length { get; }

        public Point2 Start => _waypoints[0];

        public Point2 End => _waypoints[_waypoints.Length - 1];

        public Point2 GetPosition(float distance)
        {
            float remainingDistance = Guard.NonNegative(distance, nameof(distance));
            if (remainingDistance == 0f)
            {
                return Start;
            }

            if (remainingDistance >= Length)
            {
                return End;
            }

            for (int index = 0; index < _segmentLengths.Length; index++)
            {
                float segmentLength = _segmentLengths[index];
                if (remainingDistance <= segmentLength)
                {
                    return Point2.Lerp(
                        _waypoints[index],
                        _waypoints[index + 1],
                        remainingDistance / segmentLength);
                }

                remainingDistance -= segmentLength;
            }

            return End;
        }
    }
}
