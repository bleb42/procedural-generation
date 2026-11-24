using UnityEngine;

namespace DungeonGen
{
    /// <summary>Cardinal direction on the XZ plane. North is +Z, East is +X.</summary>
    public enum Direction
    {
        North,
        East,
        South,
        West
    }

    /// <summary>Helpers for <see cref="Direction"/> and rotations in 90-degree steps around Y.</summary>
    public static class DirectionUtil
    {
        /// <summary>Wraps any number of 90-degree steps into the range 0..3.</summary>
        public static int WrapSteps(int steps) => ((steps % 4) + 4) % 4;

        public static Direction Opposite(Direction direction) => Rotate(direction, 2);

        public static Direction Rotate(Direction direction, int steps) =>
            (Direction)(((int)direction + WrapSteps(steps)) % 4);

        /// <summary>Unit vector of the direction. Vector2.x maps to world X, Vector2.y maps to world Z.</summary>
        public static Vector2 ToVector(Direction direction) => direction switch
        {
            Direction.North => Vector2.up,
            Direction.East => Vector2.right,
            Direction.South => Vector2.down,
            _ => Vector2.left
        };

        /// <summary>Rotates a vector by the same turn as a yaw of <c>steps * 90</c> degrees.</summary>
        public static Vector2 RotateVector(Vector2 vector, int steps)
        {
            steps = WrapSteps(steps);

            for (int i = 0; i < steps; i++)
                vector = new Vector2(vector.y, -vector.x);

            return vector;
        }

        public static float ToYaw(int steps) => WrapSteps(steps) * 90f;
    }
}
