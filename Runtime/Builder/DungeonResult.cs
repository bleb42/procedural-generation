using System.Collections.Generic;
using UnityEngine;

namespace DungeonGen
{
    /// <summary>A generated dungeon that exists in the scene.</summary>
    public sealed class DungeonResult
    {
        public DungeonLayout Layout { get; }
        public IReadOnlyList<SpawnedRoom> Rooms { get; }

        /// <summary>Parent of all spawned rooms. Dungeon space is the XZ plane of this transform.</summary>
        public Transform Root { get; }

        public int Seed => Layout.Seed;

        public DungeonResult(DungeonLayout layout, IReadOnlyList<SpawnedRoom> rooms, Transform root)
        {
            Layout = layout;
            Rooms = rooms;
            Root = root;
        }

        /// <summary>Converts a world position to dungeon space (XZ plane relative to <see cref="Root"/>).</summary>
        public Vector2 ToLocal(Vector3 worldPosition)
        {
            var local = Root.InverseTransformPoint(worldPosition);
            return new Vector2(local.x, local.z);
        }

        /// <summary>Finds the room that contains a world position, or null.</summary>
        public SpawnedRoom FindRoom(Vector3 worldPosition)
        {
            var point = ToLocal(worldPosition);

            foreach (var room in Rooms)
                if (room.Contains(point))
                    return room;

            return null;
        }
    }
}
