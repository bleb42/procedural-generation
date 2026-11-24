using System.Collections.Generic;
using UnityEngine;

namespace DungeonGen.Tests
{
    /// <summary>
    /// Builds the wall pieces of a layout the same way the room factory builds real rooms:
    /// a side without a door is one wall, a side with a door is two flanking walls and a door.
    /// Works for single-rectangle rooms centered on their origin.
    /// </summary>
    internal static class WallGeometry
    {
        public const float DoorWidth = 2f;

        private static readonly Direction[] Sides =
            { Direction.North, Direction.East, Direction.South, Direction.West };

        public static List<WallPiece> Build(DungeonLayout layout)
        {
            var pieces = new List<WallPiece>();

            foreach (var node in layout.Nodes)
            {
                var size = node.Blueprint.FloorRects[0].Size;

                foreach (var side in Sides)
                {
                    int doorIndex = FindDoor(node.Blueprint, side);

                    var forward = DirectionUtil.ToVector(side);
                    var tangent = new Vector2(forward.y, -forward.x);
                    bool alongX = side == Direction.North || side == Direction.South;

                    float length = alongX ? size.x : size.y;
                    var edge = forward * ((alongX ? size.y : size.x) * 0.5f);

                    if (doorIndex < 0)
                    {
                        pieces.Add(Piece(node, edge, tangent, forward, 0f, length, false, false));
                        continue;
                    }

                    float flank = (length - DoorWidth) * 0.5f;
                    float offset = DoorWidth * 0.5f + flank * 0.5f;

                    pieces.Add(Piece(node, edge, tangent, forward, -offset, flank, false, false));
                    pieces.Add(Piece(node, edge, tangent, forward, offset, flank, false, false));
                    pieces.Add(Piece(node, edge, tangent, forward, 0f, DoorWidth, true, node.Doors[doorIndex].IsConnected));
                }
            }

            return pieces;
        }

        private static int FindDoor(IRoomBlueprint blueprint, Direction side)
        {
            for (int i = 0; i < blueprint.Doors.Count; i++)
                if (blueprint.Doors[i].Direction == side)
                    return i;

            return -1;
        }

        private static WallPiece Piece(PlacementNode node, Vector2 edge, Vector2 tangent, Vector2 forward,
            float offset, float length, bool isDoor, bool isConnected)
        {
            int steps = node.RotationSteps;

            var center = node.Origin + DirectionUtil.RotateVector(edge + tangent * offset, steps);
            var normal = DirectionUtil.RotateVector(forward, steps);
            var worldTangent = new Vector2(normal.y, -normal.x);

            float along = Vector2.Dot(center, worldTangent);

            return new WallPiece
            {
                Position = new Vector3(center.x, 0f, center.y),
                Normal = new Vector3(normal.x, 0f, normal.y),
                Min = along - length * 0.5f,
                Max = along + length * 0.5f,
                IsDoor = isDoor,
                IsConnected = isConnected
            };
        }
    }
}
