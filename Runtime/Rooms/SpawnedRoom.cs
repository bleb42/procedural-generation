using System.Collections.Generic;
using UnityEngine;

namespace DungeonGen
{
    /// <summary>A room that exists in the scene.</summary>
    public sealed class SpawnedRoom
    {
        private const float ContainsTolerance = 0.05f;

        public PlacementNode Node { get; }
        public RoomInstance Instance { get; }
        public float Weight { get; }

        public SpawnedRoom(PlacementNode node, RoomInstance instance, float weight)
        {
            Node = node;
            Instance = instance;
            Weight = weight;
        }

        public string Name => Node.Blueprint.Name;

        /// <summary>Number of rooms between this one and the root.</summary>
        public int Depth => Node.Depth;

        /// <summary>Floor rectangles in dungeon space (XZ plane relative to the dungeon root).</summary>
        public IReadOnlyList<FloorRect> Rects => Node.Rects;

        /// <summary>True when a point in dungeon space lies on the floor of this room.</summary>
        public bool Contains(Vector2 dungeonPoint)
        {
            foreach (var rect in Node.Rects)
                if (rect.Contains(dungeonPoint, ContainsTolerance))
                    return true;

            return false;
        }
    }
}
