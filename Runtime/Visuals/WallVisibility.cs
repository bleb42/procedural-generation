using System.Collections.Generic;
using UnityEngine;

namespace DungeonGen
{
    /// <summary>
    /// Decides which wall pieces are hidden when rooms touch.
    /// A piece is hidden only when the visible pieces of the neighbouring room fully cover it,
    /// so a hole can never appear. Larger pieces are decided first and always stay.
    /// </summary>
    public static class WallVisibility
    {
        private const float PlaneTolerance = 0.05f;
        private const float AngleTolerance = 5f;
        private const float CoverTolerance = 0.02f;
        private const float BucketSize = 0.5f;

        /// <summary>Returns, for every piece, whether it should be hidden.</summary>
        public static bool[] ResolveHidden(IReadOnlyList<WallPiece> pieces)
        {
            int count = pieces.Count;

            var hidden = new bool[count];
            var kept = new bool[count];
            var index = new PlaneIndex(pieces);
            var order = new List<int>(count);

            for (int i = 0; i < count; i++)
                order.Add(i);

            order.Sort((a, b) =>
            {
                int byLength = pieces[b].Length.CompareTo(pieces[a].Length);
                return byLength != 0 ? byLength : a.CompareTo(b);
            });

            var candidates = new List<int>();
            var intervals = new List<Vector2>();

            foreach (int i in order)
            {
                var piece = pieces[i];

                index.GetCandidates(i, candidates);
                intervals.Clear();

                foreach (int j in candidates)
                {
                    var other = pieces[j];

                    if (!kept[j] || other.IsOpening != piece.IsOpening || !Faces(piece, other))
                        continue;

                    if (other.MinY > piece.MinY + CoverTolerance || other.MaxY < piece.MaxY - CoverTolerance)
                        continue;

                    intervals.Add(new Vector2(-other.Max, -other.Min));
                }

                if (IsCovered(piece.Min, piece.Max, intervals))
                    hidden[i] = true;
                else
                    kept[i] = true;
            }

            return hidden;
        }

        /// <summary>True when two pieces lie on the same plane and face each other.</summary>
        public static bool Faces(WallPiece a, WallPiece b)
        {
            if (Vector3.Angle(a.Normal, -b.Normal) > AngleTolerance)
                return false;

            return Mathf.Abs(Vector3.Dot(a.Position - b.Position, a.Normal)) <= PlaneTolerance;
        }

        private static bool IsCovered(float min, float max, List<Vector2> intervals)
        {
            if (max - min <= CoverTolerance)
                return false;

            intervals.Sort((a, b) => a.x.CompareTo(b.x));

            float reach = min;

            foreach (var interval in intervals)
            {
                if (interval.x > reach + CoverTolerance)
                    break;

                reach = Mathf.Max(reach, interval.y);
            }

            return reach >= max - CoverTolerance;
        }

        private sealed class PlaneIndex
        {
            private readonly Dictionary<(int axis, int plane), List<int>> _buckets = new();
            private readonly int[] _axis;
            private readonly int[] _plane;

            public PlaneIndex(IReadOnlyList<WallPiece> pieces)
            {
                _axis = new int[pieces.Count];
                _plane = new int[pieces.Count];

                for (int i = 0; i < pieces.Count; i++)
                {
                    int axis = DominantAxis(pieces[i].Normal);
                    int plane = Mathf.RoundToInt(Coord(pieces[i].Position, axis) / BucketSize);

                    _axis[i] = axis;
                    _plane[i] = plane;

                    var key = (axis, plane);

                    if (!_buckets.TryGetValue(key, out var list))
                        _buckets[key] = list = new List<int>();

                    list.Add(i);
                }
            }

            public void GetCandidates(int i, List<int> result)
            {
                result.Clear();

                for (int d = -1; d <= 1; d++)
                {
                    if (!_buckets.TryGetValue((_axis[i], _plane[i] + d), out var list))
                        continue;

                    foreach (int j in list)
                        if (j != i)
                            result.Add(j);
                }
            }

            private static int DominantAxis(Vector3 n)
            {
                float x = Mathf.Abs(n.x), y = Mathf.Abs(n.y), z = Mathf.Abs(n.z);
                return x >= y && x >= z ? 0 : (y >= z ? 1 : 2);
            }

            private static float Coord(Vector3 p, int axis) => axis == 0 ? p.x : (axis == 1 ? p.y : p.z);
        }
    }
}
