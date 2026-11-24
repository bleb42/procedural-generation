using System.Collections.Generic;
using UnityEngine;

namespace DungeonGen
{
    /// <summary>A room placed by the layout generator. Positions are in dungeon space (XZ plane).</summary>
    public sealed class PlacementNode
    {
        public IRoomBlueprint Blueprint { get; set; }
        public PlacementNode Parent { get; set; }

        /// <summary>Where the room's local origin ends up.</summary>
        public Vector2 Origin { get; set; }

        /// <summary>Rotation around Y in 90-degree steps.</summary>
        public int RotationSteps { get; set; }

        public List<PlacementNode> Children { get; } = new();
        public List<PlacementDoor> Doors { get; } = new();

        /// <summary>Floor rectangles of the room in dungeon space.</summary>
        public List<FloorRect> Rects { get; } = new();

        /// <summary>Number of rooms between this one and the root.</summary>
        public int Depth
        {
            get
            {
                int depth = 0;

                for (var node = Parent; node != null; node = node.Parent)
                    depth++;

                return depth;
            }
        }
    }
}
