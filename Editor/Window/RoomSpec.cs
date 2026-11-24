using System;

namespace DungeonGen.Editor
{
    /// <summary>Settings of a generated starter room (a box with doors).</summary>
    [Serializable]
    internal sealed class RoomSpec
    {
        public string Name = "Room";
        public float SizeX = 8f;
        public float SizeZ = 8f;
        public float Height = 3f;
        public float DoorWidth = 2f;
        public float DoorHeight = 2.4f;
        public float WallThickness = 0.3f;

        public bool North = true;
        public bool East;
        public bool South = true;
        public bool West;

        public bool HasDoor(Direction direction)
        {
            switch (direction)
            {
                case Direction.North: return North;
                case Direction.East: return East;
                case Direction.South: return South;
                default: return West;
            }
        }

        public void SetDoors(bool north, bool east, bool south, bool west)
        {
            North = north;
            East = east;
            South = south;
            West = west;
        }

        public RoomSpec Copy(string name, float sizeX, float sizeZ, bool north, bool east, bool south, bool west)
        {
            return new RoomSpec
            {
                Name = name,
                SizeX = sizeX,
                SizeZ = sizeZ,
                Height = Height,
                DoorWidth = DoorWidth,
                DoorHeight = DoorHeight,
                WallThickness = WallThickness,
                North = north,
                East = east,
                South = south,
                West = west
            };
        }
    }
}
