using System.Collections.Generic;

namespace DungeonGen
{
    /// <summary>What the layout generator needs to know about a room. Everything is in the room's local space.</summary>
    public interface IRoomBlueprint
    {
        string Name { get; }
        IReadOnlyList<FloorRect> FloorRects { get; }
        IReadOnlyList<DoorDef> Doors { get; }

        /// <summary>Free-form value for your own systems, for example an enemy budget per room.</summary>
        float Weight { get; }
    }
}
