using System;
using UnityEngine;

namespace DungeonGen
{
    /// <summary>Axis-aligned rectangle on the XZ plane (Vector2.y is world Z).</summary>
    [Serializable]
    public struct FloorRect
    {
        public Vector2 Center;
        public Vector2 Size;

        public FloorRect(Vector2 center, Vector2 size)
        {
            Center = center;
            Size = size;
        }

        public Vector2 Min => Center - Size * 0.5f;
        public Vector2 Max => Center + Size * 0.5f;

        /// <summary>Rotates around the origin by <c>steps * 90</c> degrees (same turn as a yaw rotation).</summary>
        public FloorRect Rotated(int steps)
        {
            bool swap = (DirectionUtil.WrapSteps(steps) & 1) == 1;
            var size = swap ? new Vector2(Size.y, Size.x) : Size;

            return new FloorRect(DirectionUtil.RotateVector(Center, steps), size);
        }

        public FloorRect Translated(Vector2 offset) => new FloorRect(Center + offset, Size);

        /// <summary>True when the overlap is larger than <paramref name="epsilon"/> on both axes.</summary>
        public bool Overlaps(FloorRect other, float epsilon = 0f)
        {
            Vector2 aMin = Min, aMax = Max, bMin = other.Min, bMax = other.Max;

            return Mathf.Min(aMax.x, bMax.x) - Mathf.Max(aMin.x, bMin.x) > epsilon
                && Mathf.Min(aMax.y, bMax.y) - Mathf.Max(aMin.y, bMin.y) > epsilon;
        }

        public bool Contains(Vector2 point, float tolerance = 0f)
        {
            Vector2 min = Min, max = Max;

            return point.x >= min.x - tolerance && point.x <= max.x + tolerance
                && point.y >= min.y - tolerance && point.y <= max.y + tolerance;
        }

        public bool TryIntersect(FloorRect other, out FloorRect result)
        {
            Vector2 min = Vector2.Max(Min, other.Min);
            Vector2 max = Vector2.Min(Max, other.Max);

            if (max.x - min.x <= 0f || max.y - min.y <= 0f)
            {
                result = default;
                return false;
            }

            result = new FloorRect((min + max) * 0.5f, max - min);
            return true;
        }
    }
}
