using System;
using UnityEngine;

namespace DungeonGen
{
    /// <summary>A door of a room in the room's local space: position on the floor edge and outward direction.</summary>
    [Serializable]
    public struct DoorDef
    {
        public Vector2 Position;
        public Direction Direction;

        public DoorDef(Vector2 position, Direction direction)
        {
            Position = position;
            Direction = direction;
        }
    }
}
