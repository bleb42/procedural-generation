using System.Collections.Generic;
using UnityEngine;

namespace DungeonGen
{
    /// <summary>Describes one kind of dungeon: which rooms it uses and how big it gets.</summary>
    [CreateAssetMenu(fileName = "DungeonProfile", menuName = "DungeonGen/Dungeon Profile")]
    public class DungeonProfile : ScriptableObject
    {
        [Header("Rooms")]
        [Tooltip("The first room. Placed at the origin of the dungeon.")]
        [SerializeField] private RoomDefinition _root;

        [Tooltip("Rooms with two or more doors that make up the body of the dungeon.")]
        [SerializeField] private List<RoomDefinition> _branches = new();

        [Tooltip("Dead-end room, used when no branch room fits. Needs at least one door.")]
        [SerializeField] private RoomDefinition _leaf;

        [SerializeField] private List<SpecialRoomEntry> _specialRooms = new();

        [Header("Size")]
        [SerializeField, Min(1)] private int _roomCountMin = 8;
        [SerializeField, Min(1)] private int _roomCountMax = 12;

        [Header("Layout")]
        [Tooltip("Connect doors of neighbouring rooms that line up exactly, which creates loops.")]
        [SerializeField] private bool _connectAdjacentDoors = true;

        [Tooltip("How many special rooms may be added beyond the room count. -1 = unlimited.")]
        [SerializeField] private int _maxExtraRooms = -1;

        public RoomDefinition Root => _root;
        public IReadOnlyList<RoomDefinition> Branches => _branches;
        public RoomDefinition Leaf => _leaf;
        public IReadOnlyList<SpecialRoomEntry> SpecialRooms => _specialRooms;
        public int RoomCountMin => _roomCountMin;
        public int RoomCountMax => _roomCountMax;
        public bool ConnectAdjacentDoors => _connectAdjacentDoors;
        public int MaxExtraRooms => _maxExtraRooms;

        /// <summary>Builds the input for <see cref="DungeonLayoutGenerator"/>.</summary>
        public LayoutRequest CreateRequest()
        {
            var branches = new List<IRoomBlueprint>();

            foreach (var branch in _branches)
                if (branch != null)
                    branches.Add(branch);

            var specialRooms = new List<SpecialRoomRequest>();

            foreach (var entry in _specialRooms)
            {
                if (entry?.Room == null)
                    continue;

                specialRooms.Add(new SpecialRoomRequest
                {
                    Blueprint = entry.Room,
                    Chance = entry.Chance,
                    Placement = entry.Placement
                });
            }

            return new LayoutRequest
            {
                Root = _root,
                Branches = branches,
                Leaf = _leaf,
                SpecialRooms = specialRooms,
                MinRooms = _roomCountMin,
                MaxRooms = _roomCountMax,
                ConnectAdjacentDoors = _connectAdjacentDoors,
                MaxExtraRooms = _maxExtraRooms
            };
        }

        /// <summary>Checks that every room is assigned, baked and consistent with its prefab.</summary>
        public bool Validate(List<string> errors)
        {
            int errorsBefore = errors.Count;

            if (_root == null)
                errors.Add("Root room is not assigned.");
            else
                ValidateRoom(_root, "Root", errors);

            for (int i = 0; i < _branches.Count; i++)
            {
                if (_branches[i] == null)
                    errors.Add($"Branch #{i} is empty.");
                else
                    ValidateRoom(_branches[i], $"Branch '{_branches[i].name}'", errors);
            }

            if (_leaf != null)
            {
                ValidateRoom(_leaf, $"Leaf '{_leaf.name}'", errors);

                if (_leaf.Doors.Count < 1)
                    errors.Add($"Leaf '{_leaf.name}' needs at least one door.");
            }

            foreach (var entry in _specialRooms)
                if (entry?.Room != null)
                    ValidateRoom(entry.Room, $"Special room '{entry.Room.name}'", errors);

            return errors.Count == errorsBefore;
        }

        private static void ValidateRoom(RoomDefinition room, string label, List<string> errors)
        {
            if (room.Prefab == null)
            {
                errors.Add($"{label}: prefab is not assigned.");
                return;
            }

            if (room.FloorRects.Count == 0)
                errors.Add($"{label}: no floor rectangles baked. Bake the room first.");

            var instance = room.Prefab.GetComponent<RoomInstance>();

            if (instance == null)
            {
                errors.Add($"{label}: prefab '{room.Prefab.name}' has no RoomInstance component.");
                return;
            }

            if (instance.DoorSockets.Count != room.Doors.Count)
            {
                errors.Add($"{label}: prefab has {instance.DoorSockets.Count} door sockets but the definition " +
                           $"has {room.Doors.Count} baked doors. Bake the room again.");
            }
        }
    }
}
