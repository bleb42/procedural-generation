using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGen
{
    /// <summary>
    /// Hides wall pieces that neighbouring rooms both provide on the same plane, and updates door visuals.
    /// The rules live in <see cref="WallVisibility"/>.
    /// </summary>
    public static class RoomVisualCoordinator
    {
        public static IEnumerator ApplyVisualsRoutine(IReadOnlyList<SpawnedRoom> rooms, FrameBudget budget)
        {
            budget.Begin();

            var surfaces = new List<WallSurface>();
            var pieces = new List<WallPiece>();

            foreach (var room in rooms)
            {
                CollectPieces(room, surfaces, pieces);

                if (budget.Exceeded)
                {
                    yield return null;
                    budget.Begin();
                }
            }

            var hidden = WallVisibility.ResolveHidden(pieces);

            for (int i = 0; i < surfaces.Count; i++)
                if (hidden[i])
                    surfaces[i].Hide();

            foreach (var room in rooms)
            {
                ApplyDoorStatesFor(room);
                ApplyWallVisualsFor(room);

                if (budget.Exceeded)
                {
                    yield return null;
                    budget.Begin();
                }
            }
        }

        private static void CollectPieces(SpawnedRoom room, List<WallSurface> surfaces, List<WallPiece> pieces)
        {
            foreach (var wall in room.Instance.WallSockets)
                AddPiece(wall, wall != null ? wall.transform : null, false, false, surfaces, pieces);

            var sockets = room.Instance.DoorSockets;
            var doors = room.Node.Doors;

            for (int i = 0; i < sockets.Count; i++)
            {
                bool connected = i < doors.Count && doors[i].IsConnected;
                var visual = sockets[i] != null ? sockets[i].GetVisual(connected) : null;
                var root = visual != null ? visual.transform : (sockets[i] != null ? sockets[i].transform : null);

                AddPiece(sockets[i], root, true, connected, surfaces, pieces);
            }
        }

        private static void AddPiece(WallSurface surface, Transform root, bool isDoor, bool isConnected,
    List<WallSurface> surfaces, List<WallPiece> pieces)
        {
            if (surface == null || root == null)
                return;

            var normal = surface.OutwardDirection;
            var tangent = Vector3.Cross(Vector3.up, normal).normalized;

            if (!TryGetExtent(root, tangent, out float min, out float max, out float minY, out float maxY))
                return;

            surfaces.Add(surface);
            pieces.Add(new WallPiece
            {
                Position = surface.Position,
                Normal = normal,
                Min = min,
                Max = max,
                MinY = minY,
                MaxY = maxY,
                IsDoor = isDoor,
                IsConnected = isConnected
            });
        }

        private static bool TryGetExtent(Transform root, Vector3 tangent,
    out float min, out float max, out float minY, out float maxY)
        {
            min = float.MaxValue;
            max = float.MinValue;
            minY = float.MaxValue;
            maxY = float.MinValue;

            foreach (var meshFilter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (meshFilter.sharedMesh == null)
                    continue;

                var matrix = meshFilter.transform.localToWorldMatrix;
                var bounds = meshFilter.sharedMesh.bounds;
                var center = matrix.MultiplyPoint3x4(bounds.center);

                float centerT = Vector3.Dot(center, tangent);
                float halfT = Mathf.Abs(Vector3.Dot(tangent, matrix.GetColumn(0))) * bounds.extents.x
                            + Mathf.Abs(Vector3.Dot(tangent, matrix.GetColumn(1))) * bounds.extents.y
                            + Mathf.Abs(Vector3.Dot(tangent, matrix.GetColumn(2))) * bounds.extents.z;

                float halfY = Mathf.Abs(matrix.m10) * bounds.extents.x
                            + Mathf.Abs(matrix.m11) * bounds.extents.y
                            + Mathf.Abs(matrix.m12) * bounds.extents.z;

                min = Mathf.Min(min, centerT - halfT);
                max = Mathf.Max(max, centerT + halfT);
                minY = Mathf.Min(minY, center.y - halfY);
                maxY = Mathf.Max(maxY, center.y + halfY);
            }

            return min <= max;
        }

        private static void ApplyDoorStatesFor(SpawnedRoom room)
        {
            var sockets = room.Instance.DoorSockets;
            var doors = room.Node.Doors;
            int count = Mathf.Min(sockets.Count, doors.Count);

            for (int i = 0; i < count; i++)
                sockets[i].SetConnected(doors[i].IsConnected);
        }

        private static void ApplyWallVisualsFor(SpawnedRoom room)
        {
            foreach (var wallSocket in room.Instance.WallSockets)
                wallSocket.ApplyVisual();
        }
    }
}
