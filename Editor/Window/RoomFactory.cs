using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DungeonGen.Editor
{
    /// <summary>Builds simple box rooms (floor, walls, door sockets), bakes them and saves them as assets.</summary>
    internal static class RoomFactory
    {
        public static string Validate(RoomSpec spec)
        {
            if (string.IsNullOrWhiteSpace(spec.Name))
                return "Give the room a name.";

            if (!spec.North && !spec.East && !spec.South && !spec.West)
                return "A room needs at least one door.";

            if (spec.SizeX < 1f || spec.SizeZ < 1f)
                return "The room is too small.";

            if (spec.DoorWidth < 0.5f || spec.DoorWidth >= Mathf.Min(spec.SizeX, spec.SizeZ))
                return "The door width must be smaller than the room.";

            if (spec.Height <= spec.DoorHeight)
                return "The room must be taller than its doors.";

            return null;
        }

        /// <summary>Creates the prefab and the RoomDefinition, and bakes them. Returns null and sets the error on failure.</summary>
        public static RoomDefinition Create(RoomSpec spec, string folder, out string error)
        {
            error = Validate(spec);

            if (error != null)
                return null;

            EditorAssets.EnsureFolder(folder);

            string prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{spec.Name}.prefab");
            string definitionPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{spec.Name}_Definition.asset");

            var definition = ScriptableObject.CreateInstance<RoomDefinition>();
            AssetDatabase.CreateAsset(definition, definitionPath);

            var root = Build(spec, definition);
            bool saved = false;

            try
            {
                if (root.GetComponent<RoomBaker>().Bake())
                {
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    saved = true;
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            if (!saved)
            {
                AssetDatabase.DeleteAsset(definitionPath);
                error = $"Baking '{spec.Name}' failed. See the Console.";
                return null;
            }

            EditorAssets.SetReference(definition, "_prefab", AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();

            return definition;
        }

        /// <summary>Creates six rooms of different sizes and a profile that uses them.</summary>
        public static DungeonProfile CreateStarterSet(RoomSpec template, string folder, out string error)
        {
            string starterFolder = folder + "/Starter";
            string roomsFolder = starterFolder + "/Rooms";

            EditorAssets.DeleteRoomsIn(roomsFolder);

            var specs = new[]
            {
                template.Copy("Start", 8f, 8f, true, true, true, true),
                template.Copy("Corridor", 4f, 8f, true, false, true, false),
                template.Copy("Corner", 6f, 6f, true, true, false, false),
                template.Copy("T Junction", 8f, 8f, true, true, false, true),
                template.Copy("Hall", 12f, 12f, true, true, true, true),
                template.Copy("Dead End", 4f, 4f, false, false, true, false)
            };

            var rooms = new List<RoomDefinition>();

            foreach (var spec in specs)
            {
                var room = Create(spec, roomsFolder, out error);

                if (room == null)
                {
                    EditorAssets.DeleteRoomsIn(roomsFolder);
                    return null;
                }

                rooms.Add(room);
            }

            string profilePath = starterFolder + "/Starter Profile.asset";
            var profile = AssetDatabase.LoadAssetAtPath<DungeonProfile>(profilePath)
                          ?? EditorAssets.CreateProfile(starterFolder, "Starter Profile");

            EditorAssets.FillProfile(profile, rooms);

            error = null;
            return profile;
        }

        private static GameObject Build(RoomSpec spec, RoomDefinition definition)
        {
            var root = new GameObject(spec.Name);
            var instance = root.AddComponent<RoomInstance>();
            var baker = root.AddComponent<RoomBaker>();

            EditorAssets.SetReference(baker, "_definition", definition);

            var floor = new GameObject("Floor");
            floor.transform.SetParent(root.transform, false);
            floor.AddComponent<FloorArea>().Size = new Vector3(spec.SizeX, 0.2f, spec.SizeZ);
            AddCube("Floor Visual", floor.transform, new Vector3(0f, -0.1f, 0f), new Vector3(spec.SizeX, 0.2f, spec.SizeZ));

            var doors = new List<DoorSocket>();
            var walls = new List<WallSocket>();

            foreach (Direction direction in new[] { Direction.North, Direction.East, Direction.South, Direction.West })
                BuildSide(root.transform, spec, direction, doors, walls);

            EditorAssets.SetList(instance, "_doorSockets", doors);
            EditorAssets.SetList(instance, "_wallSockets", walls);

            return root;
        }

        private static void BuildSide(Transform parent, RoomSpec spec, Direction direction,
            List<DoorSocket> doors, List<WallSocket> walls)
        {
            var forward2 = DirectionUtil.ToVector(direction);
            var forward = new Vector3(forward2.x, 0f, forward2.y);

            bool alongX = direction == Direction.North || direction == Direction.South;
            float length = alongX ? spec.SizeX : spec.SizeZ;
            float depth = alongX ? spec.SizeZ : spec.SizeX;

            var edge = forward * (depth * 0.5f);
            var rotation = Quaternion.Euler(0f, (int)direction * 90f, 0f);
            var right = rotation * Vector3.right;

            if (!spec.HasDoor(direction))
            {
                walls.Add(CreateWall(parent, $"Wall {direction}", edge, rotation, length, spec));
                return;
            }

            float segment = (length - spec.DoorWidth) * 0.5f;

            if (segment > 0.01f)
            {
                float offset = spec.DoorWidth * 0.5f + segment * 0.5f;

                walls.Add(CreateWall(parent, $"Wall {direction} A", edge - right * offset, rotation, segment, spec));
                walls.Add(CreateWall(parent, $"Wall {direction} B", edge + right * offset, rotation, segment, spec));
            }

            doors.Add(CreateDoor(parent, $"Door {direction}", edge, rotation, spec));
        }

        private static WallSocket CreateWall(Transform parent, string name, Vector3 position, Quaternion rotation,
            float length, RoomSpec spec)
        {
            var go = CreateSocketObject(parent, name, position, rotation);

            AddCube("Wall", go.transform,
                new Vector3(0f, spec.Height * 0.5f, 0f),
                new Vector3(length, spec.Height, spec.WallThickness));

            return go.AddComponent<WallSocket>();
        }

        private static DoorSocket CreateDoor(Transform parent, string name, Vector3 position, Quaternion rotation,
            RoomSpec spec)
        {
            var go = CreateSocketObject(parent, name, position, rotation);
            var socket = go.AddComponent<DoorSocket>();

            var closed = AddCube("Closed", go.transform,
                new Vector3(0f, spec.Height * 0.5f, 0f),
                new Vector3(spec.DoorWidth, spec.Height, spec.WallThickness));

            float lintel = spec.Height - spec.DoorHeight;

            var open = AddCube("Open", go.transform,
                new Vector3(0f, spec.DoorHeight + lintel * 0.5f, 0f),
                new Vector3(spec.DoorWidth, lintel, spec.WallThickness));

            open.SetActive(false);

            EditorAssets.SetReference(socket, "_wallVisual", closed);
            EditorAssets.SetReference(socket, "_doorVisual", open);

            return socket;
        }

        private static GameObject CreateSocketObject(Transform parent, string name, Vector3 position, Quaternion rotation)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;

            return go;
        }

        private static GameObject AddCube(string name, Transform parent, Vector3 localPosition, Vector3 localScale)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = localScale;

            return cube;
        }
    }
}
