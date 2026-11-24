using System.Collections.Generic;
using UnityEngine;

namespace DungeonGen
{
    /// <summary>
    /// Measures a room prefab: floor rectangles and doors in the room's local space.
    /// Used by the baker, and at runtime to detect rooms that changed after baking.
    /// </summary>
    public static class RoomMeasure
    {
        public const float MatchTolerance = 0.05f;

        private const float EdgeTolerance = 0.05f;
        private const float ProbeDistance = 0.1f;
        private const float AxisToleranceDegrees = 5f;

        /// <summary>Fills <paramref name="floors"/> and <paramref name="doors"/>. Returns false when problems were found.</summary>
        public static bool Measure(RoomInstance room, List<FloorRect> floors, List<DoorDef> doors, List<string> problems)
        {
            floors.Clear();
            doors.Clear();

            int problemsBefore = problems.Count;

            if (!IsUnitScale(room.transform.localScale))
                problems.Add("The room root must have scale (1, 1, 1).");

            MeasureFloors(room, floors, problems);
            MeasureDoors(room, floors, doors, problems);

            return problems.Count == problemsBefore;
        }

        /// <summary>True when the measured room still matches what was baked into the blueprint.</summary>
        public static bool Matches(IReadOnlyList<FloorRect> floors, IReadOnlyList<DoorDef> doors, IRoomBlueprint baked)
        {
            if (floors.Count != baked.FloorRects.Count || doors.Count != baked.Doors.Count)
                return false;

            foreach (var bakedRect in baked.FloorRects)
                if (!ContainsRect(floors, bakedRect))
                    return false;

            for (int i = 0; i < doors.Count; i++)
            {
                var a = doors[i];
                var b = baked.Doors[i];

                if (a.Direction != b.Direction || (a.Position - b.Position).sqrMagnitude > MatchTolerance * MatchTolerance)
                    return false;
            }

            return true;
        }

        private static bool ContainsRect(IReadOnlyList<FloorRect> rects, FloorRect target)
        {
            foreach (var rect in rects)
            {
                if ((rect.Center - target.Center).sqrMagnitude <= MatchTolerance * MatchTolerance
                    && (rect.Size - target.Size).sqrMagnitude <= MatchTolerance * MatchTolerance)
                    return true;
            }

            return false;
        }

        private static void MeasureFloors(RoomInstance room, List<FloorRect> floors, List<string> problems)
        {
            var root = room.transform;
            var areas = room.GetComponentsInChildren<FloorArea>(true);

            if (areas.Length == 0)
            {
                problems.Add("No FloorArea found. Add at least one.");
                return;
            }

            foreach (var area in areas)
            {
                var t = area.transform;

                if (!IsAxisAligned(root.InverseTransformDirection(t.forward)))
                    problems.Add($"FloorArea '{t.name}' must be rotated in 90-degree steps around Y.");

                Vector3 half = area.Size * 0.5f;
                float minX = float.MaxValue, minZ = float.MaxValue;
                float maxX = float.MinValue, maxZ = float.MinValue;

                for (int sx = -1; sx <= 1; sx += 2)
                {
                    for (int sz = -1; sz <= 1; sz += 2)
                    {
                        var corner = root.InverseTransformPoint(t.TransformPoint(new Vector3(half.x * sx, 0f, half.z * sz)));

                        minX = Mathf.Min(minX, corner.x);
                        maxX = Mathf.Max(maxX, corner.x);
                        minZ = Mathf.Min(minZ, corner.z);
                        maxZ = Mathf.Max(maxZ, corner.z);
                    }
                }

                floors.Add(new FloorRect(
                    new Vector2((minX + maxX) * 0.5f, (minZ + maxZ) * 0.5f),
                    new Vector2(maxX - minX, maxZ - minZ)));
            }
        }

        private static void MeasureDoors(RoomInstance room, List<FloorRect> floors, List<DoorDef> doors, List<string> problems)
        {
            var root = room.transform;
            var sockets = room.DoorSockets;

            for (int i = 0; i < sockets.Count; i++)
            {
                var socket = sockets[i];

                if (socket == null)
                {
                    problems.Add($"Door socket #{i} is empty. The list order must match the baked doors.");
                    continue;
                }

                var localPosition = root.InverseTransformPoint(socket.transform.position);
                var localOutward = root.InverseTransformDirection(socket.OutwardDirection);

                if (!IsAxisAligned(localOutward))
                    problems.Add($"Door '{socket.name}' must face North, East, South or West.");

                var door = new DoorDef(new Vector2(localPosition.x, localPosition.z), ToDirection(localOutward));

                if (floors.Count > 0)
                    ValidateDoor(socket.name, door, floors, problems);

                doors.Add(door);
            }
        }

        private static void ValidateDoor(string name, DoorDef door, List<FloorRect> floors, List<string> problems)
        {
            bool onEdge = false;

            foreach (var floor in floors)
            {
                if (IsOnEdge(floor, door))
                {
                    onEdge = true;
                    break;
                }
            }

            if (!onEdge)
            {
                problems.Add($"Door '{name}' is not on the outer edge of a FloorArea (facing {door.Direction}).");
                return;
            }

            var probe = door.Position + DirectionUtil.ToVector(door.Direction) * ProbeDistance;

            foreach (var floor in floors)
            {
                if (!floor.Contains(probe))
                    continue;

                problems.Add($"Door '{name}' faces into another FloorArea of the same room.");
                return;
            }
        }

        private static bool IsOnEdge(FloorRect floor, DoorDef door)
        {
            Vector2 min = floor.Min, max = floor.Max, p = door.Position;

            switch (door.Direction)
            {
                case Direction.North:
                    return Mathf.Abs(p.y - max.y) <= EdgeTolerance && p.x >= min.x - EdgeTolerance && p.x <= max.x + EdgeTolerance;

                case Direction.East:
                    return Mathf.Abs(p.x - max.x) <= EdgeTolerance && p.y >= min.y - EdgeTolerance && p.y <= max.y + EdgeTolerance;

                case Direction.South:
                    return Mathf.Abs(p.y - min.y) <= EdgeTolerance && p.x >= min.x - EdgeTolerance && p.x <= max.x + EdgeTolerance;

                default:
                    return Mathf.Abs(p.x - min.x) <= EdgeTolerance && p.y >= min.y - EdgeTolerance && p.y <= max.y + EdgeTolerance;
            }
        }

        private static Direction ToDirection(Vector3 localDirection)
        {
            int steps = Mathf.RoundToInt(Mathf.Atan2(localDirection.x, localDirection.z) / (Mathf.PI * 0.5f));
            return (Direction)DirectionUtil.WrapSteps(steps);
        }

        private static bool IsAxisAligned(Vector3 localDirection)
        {
            if (Mathf.Abs(localDirection.y) > 0.05f)
                return false;

            float yaw = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
            float nearest = Mathf.Round(yaw / 90f) * 90f;

            return Mathf.Abs(Mathf.DeltaAngle(yaw, nearest)) <= AxisToleranceDegrees;
        }

        private static bool IsUnitScale(Vector3 scale) =>
            Mathf.Abs(scale.x - 1f) < 0.001f && Mathf.Abs(scale.y - 1f) < 0.001f && Mathf.Abs(scale.z - 1f) < 0.001f;
    }
}
