using System.Collections.Generic;

namespace DungeonGen
{
    /// <summary>Input of <see cref="DungeonLayoutGenerator"/>. Independent of Unity assets, so it is easy to test.</summary>
    public sealed class LayoutRequest
    {
        public IRoomBlueprint Root;

        /// <summary>Rooms with two or more doors. Rooms with fewer doors are ignored.</summary>
        public IReadOnlyList<IRoomBlueprint> Branches;

        /// <summary>Dead-end room used when no branch fits. Optional.</summary>
        public IRoomBlueprint Leaf;

        public IReadOnlyList<SpecialRoomRequest> SpecialRooms;

        public int MinRooms = 8;
        public int MaxRooms = 12;
        public bool ConnectAdjacentDoors = true;

        /// <summary>How many special rooms may exceed the room count. -1 = unlimited.</summary>
        public int MaxExtraRooms = -1;
    }
}
