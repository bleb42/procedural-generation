using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGen
{
    /// <summary>
    /// Places rooms by snapping door to door. Rooms are rotated in 90-degree steps and must not
    /// overlap on the floor. There is no grid: any room size works.
    /// </summary>
    public sealed class DungeonLayoutGenerator
    {
        private const float OverlapEpsilon = 0.01f;
        private const float DoorMatchTolerance = 0.05f;

        private readonly int _seed;
        private readonly System.Random _random;
        private readonly List<FloorRect> _occupied = new();
        private readonly List<string> _warnings = new();

        private struct OpenDoor
        {
            public PlacementNode Node;
            public int DoorIndex;
        }

        private struct DepthDoor
        {
            public OpenDoor Door;
            public int Depth;
            public int Order;
        }

        private sealed class RoomBudget
        {
            public int Deficit;
            public int ExtraUsed;
        }

        public DungeonLayoutGenerator(int seed)
        {
            _seed = seed;
            _random = new System.Random(seed);
        }

        /// <exception cref="ArgumentException">The request has no root room.</exception>
        /// <exception cref="InvalidOperationException">The rooms cannot be used to build a dungeon.</exception>
        public DungeonLayout Generate(LayoutRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Root == null)
                throw new ArgumentException("Root room is not assigned.", nameof(request));

            _occupied.Clear();
            _warnings.Clear();

            var branches = CollectBranches(request.Branches);

            if (branches.Count == 0 && request.Leaf == null)
            {
                throw new InvalidOperationException(
                    "No usable room: add a branch room with 2+ doors or a leaf room.");
            }

            int min = Mathf.Max(1, request.MinRooms);
            int max = Mathf.Max(min, request.MaxRooms);
            int target = _random.Next(min, max + 1);

            var root = new PlacementNode { Blueprint = request.Root };
            PlaceRoot(root);

            var openDoors = new List<OpenDoor>();
            AddOpenDoors(root, openDoors);

            var budget = new RoomBudget { Deficit = target - 1 };

            PlaceSpecialRooms(request, SpecialRoomPlacement.Anywhere, budget,
                blueprint => TryPlaceAnywhere(blueprint, openDoors));

            FillOpenDoors(openDoors, branches, request.Leaf, budget);

            PlaceSpecialRooms(request, SpecialRoomPlacement.DeepestPoint, budget,
                blueprint => TryPlaceAtDeepest(blueprint, root));

            if (request.ConnectAdjacentDoors)
                ConnectAdjacentDoors(root);

            return new DungeonLayout(root, target, _seed, _warnings.ToArray());
        }

        private static List<IRoomBlueprint> CollectBranches(IReadOnlyList<IRoomBlueprint> source)
        {
            var result = new List<IRoomBlueprint>();

            if (source == null)
                return result;

            foreach (var blueprint in source)
                if (blueprint != null && blueprint.Doors.Count >= 2)
                    result.Add(blueprint);

            return result;
        }

        private void FillOpenDoors(List<OpenDoor> openDoors, List<IRoomBlueprint> branches,
            IRoomBlueprint leaf, RoomBudget budget)
        {
            while (budget.Deficit > 0 && openDoors.Count > 0)
            {
                int index = _random.Next(openDoors.Count);
                var openDoor = openDoors[index];
                openDoors.RemoveAt(index);

                if (openDoor.Node.Doors[openDoor.DoorIndex].IsConnected)
                    continue;

                var placed = TryFillDoor(openDoor.Node, openDoor.DoorIndex, branches, leaf);

                if (placed == null)
                    continue;

                openDoor.Node.Children.Add(placed);
                budget.Deficit--;

                AddOpenDoors(placed, openDoors);
            }
        }

        private PlacementNode TryFillDoor(PlacementNode parent, int parentDoorIndex,
            List<IRoomBlueprint> branches, IRoomBlueprint leaf)
        {
            var order = new List<IRoomBlueprint>(branches);
            Shuffle(order);

            foreach (var blueprint in order)
            {
                var node = new PlacementNode { Blueprint = blueprint, Parent = parent };

                if (TryPlaceNode(node, parent, parentDoorIndex))
                    return node;
            }

            if (leaf != null)
            {
                var leafNode = new PlacementNode { Blueprint = leaf, Parent = parent };

                if (TryPlaceNode(leafNode, parent, parentDoorIndex))
                    return leafNode;
            }

            return null;
        }

        private void PlaceSpecialRooms(LayoutRequest request, SpecialRoomPlacement placement,
            RoomBudget budget, Func<IRoomBlueprint, bool> tryPlace)
        {
            if (request.SpecialRooms == null)
                return;

            foreach (var special in request.SpecialRooms)
            {
                if (special.Blueprint == null || special.Placement != placement)
                    continue;

                if (_random.NextDouble() >= special.Chance)
                    continue;

                bool isExtra = budget.Deficit <= 0;

                if (isExtra && request.MaxExtraRooms >= 0 && budget.ExtraUsed >= request.MaxExtraRooms)
                {
                    _warnings.Add($"Special room '{special.Blueprint.Name}' ({placement}) skipped: " +
                                  $"the extra room limit ({request.MaxExtraRooms}) is used up.");
                    continue;
                }

                if (!tryPlace(special.Blueprint))
                {
                    _warnings.Add($"Special room '{special.Blueprint.Name}' ({placement}) passed its chance roll " +
                                  "but no open door could fit it.");
                    continue;
                }

                budget.Deficit--;

                if (isExtra)
                    budget.ExtraUsed++;
            }
        }

        private bool TryPlaceAnywhere(IRoomBlueprint blueprint, List<OpenDoor> openDoors)
        {
            foreach (int index in ShuffledRange(openDoors.Count))
            {
                var candidate = openDoors[index];

                if (candidate.Node.Doors[candidate.DoorIndex].IsConnected)
                    continue;

                var node = new PlacementNode { Blueprint = blueprint, Parent = candidate.Node };

                if (!TryPlaceNode(node, candidate.Node, candidate.DoorIndex))
                    continue;

                candidate.Node.Children.Add(node);
                openDoors.RemoveAt(index);
                AddOpenDoors(node, openDoors);

                return true;
            }

            return false;
        }

        private bool TryPlaceAtDeepest(IRoomBlueprint blueprint, PlacementNode root)
        {
            var candidates = new List<DepthDoor>();
            CollectOpenDoorsWithDepth(root, candidates);

            candidates.Sort((a, b) =>
            {
                int byDepth = b.Depth.CompareTo(a.Depth);
                return byDepth != 0 ? byDepth : a.Order.CompareTo(b.Order);
            });

            foreach (var candidate in candidates)
            {
                var door = candidate.Door;

                if (door.Node.Doors[door.DoorIndex].IsConnected)
                    continue;

                var node = new PlacementNode { Blueprint = blueprint, Parent = door.Node };

                if (!TryPlaceNode(node, door.Node, door.DoorIndex))
                    continue;

                door.Node.Children.Add(node);
                return true;
            }

            return false;
        }

        private static void CollectOpenDoorsWithDepth(PlacementNode node, List<DepthDoor> result)
        {
            var stack = new Stack<PlacementNode>();
            stack.Push(node);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                int depth = current.Depth;

                for (int i = 0; i < current.Doors.Count; i++)
                {
                    if (current.Doors[i].IsConnected)
                        continue;

                    result.Add(new DepthDoor
                    {
                        Door = new OpenDoor { Node = current, DoorIndex = i },
                        Depth = depth,
                        Order = result.Count
                    });
                }

                for (int i = current.Children.Count - 1; i >= 0; i--)
                    stack.Push(current.Children[i]);
            }
        }

        private static void AddOpenDoors(PlacementNode node, List<OpenDoor> openDoors)
        {
            for (int i = 0; i < node.Doors.Count; i++)
                if (!node.Doors[i].IsConnected)
                    openDoors.Add(new OpenDoor { Node = node, DoorIndex = i });
        }

        private static void ConnectAdjacentDoors(PlacementNode root)
        {
            var all = new List<OpenDoor>();
            CollectAllDoors(root, all);

            for (int i = 0; i < all.Count; i++)
            {
                var a = all[i];
                var doorA = a.Node.Doors[a.DoorIndex];

                if (doorA.IsConnected)
                    continue;

                for (int j = i + 1; j < all.Count; j++)
                {
                    var b = all[j];
                    var doorB = b.Node.Doors[b.DoorIndex];

                    if (b.Node == a.Node || doorB.IsConnected)
                        continue;

                    if (doorB.Direction != DirectionUtil.Opposite(doorA.Direction))
                        continue;

                    if ((doorA.Position - doorB.Position).sqrMagnitude > DoorMatchTolerance * DoorMatchTolerance)
                        continue;

                    doorA.IsConnected = true;
                    doorB.IsConnected = true;
                    break;
                }
            }
        }

        private static void CollectAllDoors(PlacementNode root, List<OpenDoor> result)
        {
            var stack = new Stack<PlacementNode>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var node = stack.Pop();

                for (int i = 0; i < node.Doors.Count; i++)
                    result.Add(new OpenDoor { Node = node, DoorIndex = i });

                for (int i = node.Children.Count - 1; i >= 0; i--)
                    stack.Push(node.Children[i]);
            }
        }

        private void PlaceRoot(PlacementNode root)
        {
            var blueprint = root.Blueprint;

            if (blueprint.FloorRects == null || blueprint.FloorRects.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Root room '{blueprint.Name}' has no floor rectangles. Bake the room first.");
            }

            TryReserve(blueprint, Vector2.zero, 0, out var rects);
            Commit(root, Vector2.zero, 0, rects);
        }

        private bool TryPlaceNode(PlacementNode child, PlacementNode parent, int parentDoorIndex)
        {
            var blueprint = child.Blueprint;

            if (blueprint.FloorRects == null || blueprint.FloorRects.Count == 0)
            {
                _warnings.Add($"Room '{blueprint.Name}' has no floor rectangles and was skipped.");
                return false;
            }

            var parentDoor = parent.Doors[parentDoorIndex];
            var required = DirectionUtil.Opposite(parentDoor.Direction);

            foreach (int doorIndex in ShuffledRange(blueprint.Doors.Count))
            {
                var door = blueprint.Doors[doorIndex];

                // The only rotation that turns this door to face the parent's door.
                int steps = DirectionUtil.WrapSteps((int)required - (int)door.Direction);
                var origin = parentDoor.Position - DirectionUtil.RotateVector(door.Position, steps);

                if (!TryReserve(blueprint, origin, steps, out var rects))
                    continue;

                Commit(child, origin, steps, rects);

                parentDoor.IsConnected = true;
                child.Doors[doorIndex].IsConnected = true;

                return true;
            }

            return false;
        }

        private bool TryReserve(IRoomBlueprint blueprint, Vector2 origin, int steps, out List<FloorRect> worldRects)
        {
            worldRects = new List<FloorRect>(blueprint.FloorRects.Count);

            foreach (var local in blueprint.FloorRects)
            {
                var rect = local.Rotated(steps).Translated(origin);

                foreach (var occupied in _occupied)
                {
                    if (!rect.Overlaps(occupied, OverlapEpsilon))
                        continue;

                    worldRects = null;
                    return false;
                }

                worldRects.Add(rect);
            }

            return true;
        }

        private void Commit(PlacementNode node, Vector2 origin, int steps, List<FloorRect> rects)
        {
            node.Origin = origin;
            node.RotationSteps = steps;

            node.Rects.Clear();
            node.Rects.AddRange(rects);
            _occupied.AddRange(rects);

            node.Doors.Clear();

            for (int i = 0; i < node.Blueprint.Doors.Count; i++)
            {
                var door = node.Blueprint.Doors[i];

                node.Doors.Add(new PlacementDoor
                {
                    Position = origin + DirectionUtil.RotateVector(door.Position, steps),
                    Direction = DirectionUtil.Rotate(door.Direction, steps),
                    BlueprintDoorIndex = i
                });
            }
        }

        private List<int> ShuffledRange(int count)
        {
            var list = new List<int>(count);

            for (int i = 0; i < count; i++)
                list.Add(i);

            Shuffle(list);
            return list;
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
