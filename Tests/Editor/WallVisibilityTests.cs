using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DungeonGen.Tests
{
    public class WallVisibilityTests
    {
        private const int SeedCount = 300;
        private const float SampleStep = 0.1f;

        // The seed that produced a hole in the wall of the starter set.
        private const int ReportedSeed = -758880434;

        // ---------------------------------------------------------------- rules on hand-made pieces

        /// <summary>A piece on the plane x = 0 covering z in [z0, z1]. The facing decides the tangent direction.</summary>
        private static WallPiece Piece(bool facingPlusX, float z0, float z1, bool isDoor = false, bool isConnected = false)
        {
            return new WallPiece
            {
                Position = new Vector3(0f, 0f, (z0 + z1) * 0.5f),
                Normal = facingPlusX ? Vector3.right : Vector3.left,

                // The tangent is Cross(up, normal): -Z for +X, +Z for -X.
                Min = facingPlusX ? -z1 : z0,
                Max = facingPlusX ? -z0 : z1,

                IsDoor = isDoor,
                IsConnected = isConnected
            };
        }

        [Test]
        public void EqualWalls_OnlyOneIsHidden()
        {
            var hidden = WallVisibility.ResolveHidden(new[] { Piece(true, -4f, 4f), Piece(false, -4f, 4f) });

            Assert.AreEqual(1, Count(hidden));
        }

        [Test]
        public void SmallerWall_CoveredByALargerOne_IsHidden()
        {
            var hidden = WallVisibility.ResolveHidden(new[] { Piece(true, -4f, 4f), Piece(false, -1f, 1f) });

            Assert.IsFalse(hidden[0]);
            Assert.IsTrue(hidden[1]);
        }

        [Test]
        public void ClosedDoor_NeverHidesALargerWall()
        {
            // A closed door is just a wall. The old "door always wins" rule made a hole here.
            var hidden = WallVisibility.ResolveHidden(new[]
            {
                Piece(true, -4f, 4f),
                Piece(false, -1f, 1f, isDoor: true, isConnected: false)
            });

            Assert.IsFalse(hidden[0]);
            Assert.IsTrue(hidden[1]);
        }

        [Test]
        public void PartlyOverlappingWalls_AreBothKept()
        {
            var hidden = WallVisibility.ResolveHidden(new[] { Piece(true, -4f, 0f), Piece(false, -2f, 2f) });

            Assert.AreEqual(0, Count(hidden));
        }

        [Test]
        public void ConnectedDoorPair_ShowsOnlyOneDoor()
        {
            var hidden = WallVisibility.ResolveHidden(new[]
            {
                Piece(true, -1f, 1f, isDoor: true, isConnected: true),
                Piece(false, -1f, 1f, isDoor: true, isConnected: true)
            });

            Assert.AreEqual(1, Count(hidden));
        }

        [Test]
        public void Wall_IsNeverHiddenByAnOpening()
        {
            var hidden = WallVisibility.ResolveHidden(new[]
            {
                Piece(true, -4f, 4f),
                Piece(false, -1f, 1f, isDoor: true, isConnected: true)
            });

            Assert.AreEqual(0, Count(hidden));
        }

        // ---------------------------------------------------------------- generated dungeons

        private static LayoutRequest StarterRequest()
        {
            const Direction N = Direction.North, E = Direction.East, S = Direction.South, W = Direction.West;

            return new LayoutRequest
            {
                Root = TestBlueprint.Rect("Start", 8f, 8f, N, E, S, W),
                Branches = new IRoomBlueprint[]
                {
                    TestBlueprint.Rect("Corridor", 4f, 8f, N, S),
                    TestBlueprint.Rect("Corner", 6f, 6f, N, E),
                    TestBlueprint.Rect("T Junction", 8f, 8f, N, E, W),
                    TestBlueprint.Rect("Hall", 12f, 12f, N, E, S, W)
                },
                Leaf = TestBlueprint.Rect("Dead End", 4f, 4f, S),
                MinRooms = 8,
                MaxRooms = 12,
                ConnectAdjacentDoors = true
            };
        }

        [Test]
        public void ReportedSeed_HasNoHoles()
        {
            AssertNoHoles(ReportedSeed);
        }

        [Test]
        public void GeneratedDungeons_NeverHaveHoles()
        {
            for (int seed = 0; seed < SeedCount; seed++)
                AssertNoHoles(seed);
        }

        [Test]
        public void GeneratedDungeons_HideSomethingButNotEverything()
        {
            var layout = new DungeonLayoutGenerator(1).Generate(StarterRequest());
            var pieces = WallGeometry.Build(layout);
            int hidden = Count(WallVisibility.ResolveHidden(pieces));

            Assert.Greater(hidden, 0);
            Assert.Less(hidden, pieces.Count);
        }

        private static void AssertNoHoles(int seed)
        {
            var layout = new DungeonLayoutGenerator(seed).Generate(StarterRequest());
            var pieces = WallGeometry.Build(layout);
            var hidden = WallVisibility.ResolveHidden(pieces);

            for (int i = 0; i < pieces.Count; i++)
            {
                if (!hidden[i])
                    continue;

                var piece = pieces[i];
                var cover = new List<Vector2>();

                for (int j = 0; j < pieces.Count; j++)
                {
                    var other = pieces[j];

                    if (!hidden[j] && other.IsOpening == piece.IsOpening && WallVisibility.Faces(piece, other))
                        cover.Add(new Vector2(-other.Max, -other.Min));
                }

                for (float t = piece.Min + SampleStep; t < piece.Max - SampleStep * 0.5f; t += SampleStep)
                {
                    Assert.IsTrue(Contains(cover, t),
                        $"seed {seed}: a hidden piece at {piece.Position} is not covered at {t:0.0}");
                }
            }
        }

        private static bool Contains(List<Vector2> intervals, float value)
        {
            foreach (var interval in intervals)
                if (value >= interval.x - 0.001f && value <= interval.y + 0.001f)
                    return true;

            return false;
        }

        private static int Count(bool[] flags)
        {
            int count = 0;

            foreach (bool flag in flags)
                if (flag)
                    count++;

            return count;
        }
    }
}
