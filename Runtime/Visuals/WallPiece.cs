using UnityEngine;

namespace DungeonGen
{
    /// <summary>
    /// One wall-like surface seen from above. Plain data, so the visibility rules can be tested without Unity objects.
    /// </summary>
    public struct WallPiece
    {
        public Vector3 Position;

        /// <summary>The direction the surface faces, out of its room.</summary>
        public Vector3 Normal;

        /// <summary>Extent along <c>Cross(Vector3.up, Normal)</c>, in world coordinates.</summary>
        public float Min;
        public float Max;

        public float MinY;
        public float MaxY;

        public bool IsDoor;

        /// <summary>Only meaningful for doors. A connected door is an opening, an unconnected one is a closed wall.</summary>
        public bool IsConnected;

        public float Length => Max - Min;

        /// <summary>True for a door that leads into another room.</summary>
        public bool IsOpening => IsDoor && IsConnected;
    }
}
