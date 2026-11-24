using UnityEngine;

namespace DungeonGen
{
    /// <summary>A door of a placed room, in dungeon space.</summary>
    public sealed class PlacementDoor
    {
        public Vector2 Position { get; set; }
        public Direction Direction { get; set; }
        public int BlueprintDoorIndex { get; set; }
        public bool IsConnected { get; set; }
    }
}
