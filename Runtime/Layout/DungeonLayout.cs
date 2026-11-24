using System.Collections.Generic;

namespace DungeonGen
{
    /// <summary>Result of the layout generator: a tree of placed rooms.</summary>
    public sealed class DungeonLayout
    {
        public PlacementNode Root { get; }

        /// <summary>All rooms, parents before children.</summary>
        public IReadOnlyList<PlacementNode> Nodes { get; }

        public int Seed { get; }
        public int RequestedRoomCount { get; }

        /// <summary>Non-fatal problems, for example a special room that did not fit anywhere.</summary>
        public IReadOnlyList<string> Warnings { get; }

        public int PlacedRoomCount => Nodes.Count;
        public bool ReachedTarget => PlacedRoomCount >= RequestedRoomCount;

        public DungeonLayout(PlacementNode root, int requestedRoomCount, int seed, IReadOnlyList<string> warnings)
        {
            Root = root;
            RequestedRoomCount = requestedRoomCount;
            Seed = seed;
            Warnings = warnings;
            Nodes = Flatten(root);
        }

        private static List<PlacementNode> Flatten(PlacementNode root)
        {
            var result = new List<PlacementNode>();
            var stack = new Stack<PlacementNode>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                result.Add(node);

                for (int i = node.Children.Count - 1; i >= 0; i--)
                    stack.Push(node.Children[i]);
            }

            return result;
        }
    }
}
