using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace DungeonGen.Tests
{
    public class DungeonLayoutGeneratorTests
    {
        private const float Tolerance = 0.05f;
        private const int SeedCount = 100;

        private static readonly Direction[] AllSides =
            { Direction.North, Direction.East, Direction.South, Direction.West };

        private static LayoutRequest CreateRequest(int rooms, params float[] branchSizes)
        {
            var branches = new List<IRoomBlueprint>();

            foreach (float size in branchSizes)
            {
                branches.Add(TestBlueprint.Square($"Cross {size}", size, AllSides));
                branches.Add(TestBlueprint.Square($"Corridor {size}", size, Direction.North, Direction.South));
                branches.Add(TestBlueprint.Square($"Corner {size}", size, Direction.North, Direction.East));
            }

            return new LayoutRequest
            {
                Root = TestBlueprint.Square("Root", 8f, AllSides),
                Branches = branches,
                Leaf = TestBlueprint.Square("Leaf", 4f, Direction.South),
                MinRooms = rooms,
                MaxRooms = rooms,
                ConnectAdjacentDoors = true
            };
        }

        [Test]
        public void SameSeed_ProducesTheSameLayout()
        {
            var request = CreateRequest(12, 8f, 6f);

            var a = new DungeonLayoutGenerator(1234).Generate(request);
            var b = new DungeonLayoutGenerator(1234).Generate(request);

            Assert.AreEqual(a.Nodes.Count, b.Nodes.Count);

            for (int i = 0; i < a.Nodes.Count; i++)
            {
                Assert.AreEqual(a.Nodes[i].Blueprint.Name, b.Nodes[i].Blueprint.Name);
                Assert.AreEqual(a.Nodes[i].RotationSteps, b.Nodes[i].RotationSteps);
                Assert.AreEqual(a.Nodes[i].Origin, b.Nodes[i].Origin);
            }
        }

        [Test]
        public void Rooms_NeverOverlap()
        {
            var request = CreateRequest(15, 8f, 6f);

            for (int seed = 0; seed < SeedCount; seed++)
                AssertNoOverlap(new DungeonLayoutGenerator(seed).Generate(request), seed);
        }

        [Test]
        public void Rooms_OfAnySize_LineUpWithoutAGrid()
        {
            var request = CreateRequest(15, 3.5f, 9f, 6.25f);

            for (int seed = 0; seed < SeedCount; seed++)
            {
                var layout = new DungeonLayoutGenerator(seed).Generate(request);

                AssertNoOverlap(layout, seed);
                AssertChildrenTouchParents(layout, seed);
            }
        }

        [Test]
        public void EveryRoomIsConnectedToItsParent()
        {
            var request = CreateRequest(12, 8f, 6f);

            for (int seed = 0; seed < SeedCount; seed++)
                AssertChildrenTouchParents(new DungeonLayoutGenerator(seed).Generate(request), seed);
        }

        [Test]
        public void ReachesTheRequestedRoomCount_WhenThereIsSpace()
        {
            var request = CreateRequest(10, 8f, 6f);

            for (int seed = 0; seed < SeedCount; seed++)
            {
                var layout = new DungeonLayoutGenerator(seed).Generate(request);

                Assert.IsTrue(layout.ReachedTarget, $"seed {seed}: {layout.PlacedRoomCount}/{layout.RequestedRoomCount}");
            }
        }

        [Test]
        public void RoomCount_StaysInsideTheRequestedRange()
        {
            var request = CreateRequest(8, 8f);
            request.MinRooms = 6;
            request.MaxRooms = 9;

            for (int seed = 0; seed < SeedCount; seed++)
            {
                var layout = new DungeonLayoutGenerator(seed).Generate(request);

                Assert.GreaterOrEqual(layout.RequestedRoomCount, 6);
                Assert.LessOrEqual(layout.RequestedRoomCount, 9);
            }
        }

        [Test]
        public void SpecialRoom_AtDeepestPoint_IsPlaced()
        {
            var request = CreateRequest(10, 8f);

            request.SpecialRooms = new[]
            {
                new SpecialRoomRequest
                {
                    Blueprint = TestBlueprint.Square("Boss", 8f, Direction.South),
                    Chance = 1f,
                    Placement = SpecialRoomPlacement.DeepestPoint
                }
            };

            int placed = 0;

            for (int seed = 0; seed < SeedCount; seed++)
            {
                var layout = new DungeonLayoutGenerator(seed).Generate(request);

                if (layout.Nodes.Any(n => n.Blueprint.Name == "Boss"))
                    placed++;
            }

            // A special room may legitimately find no free door on a crowded layout.
            Assert.GreaterOrEqual(placed, SeedCount * 9 / 10);
        }

        [Test]
        public void ThrowsWithoutRootRoom()
        {
            var request = CreateRequest(5, 8f);
            request.Root = null;

            Assert.Throws<ArgumentException>(() => new DungeonLayoutGenerator(1).Generate(request));
        }

        [Test]
        public void ThrowsWhenNoRoomCanBeUsedBesidesTheRoot()
        {
            var request = new LayoutRequest { Root = TestBlueprint.Square("Root", 8f, AllSides) };

            Assert.Throws<InvalidOperationException>(() => new DungeonLayoutGenerator(1).Generate(request));
        }

        private static void AssertNoOverlap(DungeonLayout layout, int seed)
        {
            for (int i = 0; i < layout.Nodes.Count; i++)
            {
                for (int j = i + 1; j < layout.Nodes.Count; j++)
                {
                    foreach (var a in layout.Nodes[i].Rects)
                        foreach (var b in layout.Nodes[j].Rects)
                            Assert.IsFalse(a.Overlaps(b, 0.01f),
                                $"seed {seed}: '{layout.Nodes[i].Blueprint.Name}' overlaps '{layout.Nodes[j].Blueprint.Name}'");
                }
            }
        }

        private static void AssertChildrenTouchParents(DungeonLayout layout, int seed)
        {
            foreach (var node in layout.Nodes)
            {
                if (node.Parent == null)
                    continue;

                Assert.IsTrue(SharesADoor(node, node.Parent),
                    $"seed {seed}: '{node.Blueprint.Name}' has no door that meets its parent");
            }
        }

        private static bool SharesADoor(PlacementNode child, PlacementNode parent)
        {
            foreach (var a in child.Doors)
            {
                if (!a.IsConnected)
                    continue;

                foreach (var b in parent.Doors)
                {
                    if (b.IsConnected
                        && b.Direction == DirectionUtil.Opposite(a.Direction)
                        && (a.Position - b.Position).magnitude <= Tolerance)
                        return true;
                }
            }

            return false;
        }
    }
}
