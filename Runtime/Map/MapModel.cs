using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGen
{
    /// <summary>What the player knows about the dungeon: which rooms exist, which were visited, where the player is.</summary>
    public sealed class MapModel
    {
        private readonly List<SpawnedRoom> _rooms = new();
        private readonly HashSet<SpawnedRoom> _visited = new();

        private DungeonResult _result;

        public IReadOnlyList<SpawnedRoom> Rooms => _rooms;
        public SpawnedRoom Current { get; private set; }
        public SpawnedRoom Start { get; private set; }

        /// <summary>Raised when the rooms change (a dungeon was built or cleared).</summary>
        public event Action Rebuilt;

        /// <summary>Raised when the player enters a different room.</summary>
        public event Action Entered;

        public bool IsVisited(SpawnedRoom room) => _visited.Contains(room);

        /// <summary>Converts a world position to dungeon space.</summary>
        public Vector2 ToLocal(Vector3 worldPosition) =>
            _result != null ? _result.ToLocal(worldPosition) : new Vector2(worldPosition.x, worldPosition.z);

        /// <summary>Finds the room that contains a point in dungeon space, or null.</summary>
        public SpawnedRoom RoomAt(Vector2 dungeonPoint)
        {
            foreach (var room in _rooms)
                if (room.Contains(dungeonPoint))
                    return room;

            return null;
        }

        public void Build(DungeonResult result)
        {
            ResetData();

            _result = result;

            if (result != null && result.Rooms.Count > 0)
            {
                _rooms.AddRange(result.Rooms);

                Current = _rooms[0];
                Start = Current;
                _visited.Add(Current);
            }

            Rebuilt?.Invoke();
        }

        public void Clear()
        {
            ResetData();
            Rebuilt?.Invoke();
        }

        public void Track(Vector3 worldPosition)
        {
            if (_rooms.Count == 0)
                return;

            var point = ToLocal(worldPosition);

            if (Current != null && Current.Contains(point))
                return;

            var room = RoomAt(point);

            if (room == null || room == Current)
                return;

            Current = room;
            _visited.Add(room);
            Entered?.Invoke();
        }

        private void ResetData()
        {
            _rooms.Clear();
            _visited.Clear();
            _result = null;
            Current = null;
            Start = null;
        }
    }
}
