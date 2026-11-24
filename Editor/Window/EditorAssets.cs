using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DungeonGen.Editor
{
    internal enum RoomStatus
    {
        Ready,
        NotBaked,
        Outdated,
        NoPrefab
    }

    /// <summary>Finding, checking, baking and wiring up the assets of the package.</summary>
    internal static class EditorAssets
    {
        public static List<RoomDefinition> FindRooms() => FindAll<RoomDefinition>();

        public static List<DungeonProfile> FindProfiles() => FindAll<DungeonProfile>();

        public static RoomStatus GetStatus(RoomDefinition definition)
        {
            if (definition.Prefab == null)
                return RoomStatus.NoPrefab;

            if (definition.FloorRects.Count == 0)
                return RoomStatus.NotBaked;

            var instance = definition.Prefab.GetComponent<RoomInstance>();

            if (instance == null || instance.DoorSockets.Count != definition.Doors.Count)
                return RoomStatus.Outdated;

            return RoomStatus.Ready;
        }

        /// <summary>Bakes a room by opening its prefab in the background.</summary>
        public static bool Bake(RoomDefinition definition, out string message)
        {
            string path = definition.Prefab != null ? AssetDatabase.GetAssetPath(definition.Prefab) : null;

            if (string.IsNullOrEmpty(path))
            {
                message = $"{definition.name}: no prefab assigned.";
                return false;
            }

            var root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                var baker = root.GetComponent<RoomBaker>();

                if (baker == null)
                {
                    message = $"{definition.name}: the prefab root has no RoomBaker.";
                    return false;
                }

                if (baker.Definition != definition)
                {
                    message = $"{definition.name}: the RoomBaker on the prefab points to a different RoomDefinition.";
                    return false;
                }

                bool baked = baker.Bake();
                message = baked ? $"{definition.name}: baked." : $"{definition.name}: bake failed, see the Console.";
                return baked;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static void FillProfile(DungeonProfile profile, List<RoomDefinition> rooms)
        {
            RoomDefinition root = null;
            RoomDefinition leaf = null;

            foreach (var room in rooms)
            {
                if (root == null || room.Doors.Count > root.Doors.Count)
                    root = room;

                if (leaf == null && room.Doors.Count == 1)
                    leaf = room;
            }

            var branches = new List<RoomDefinition>();

            foreach (var room in rooms)
                if (room != root && room.Doors.Count >= 2)
                    branches.Add(room);

            var serialized = new SerializedObject(profile);
            serialized.FindProperty("_root").objectReferenceValue = root;
            serialized.FindProperty("_leaf").objectReferenceValue = leaf;
            WriteList(serialized.FindProperty("_branches"), branches);
            serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(profile);
        }

        public static DungeonProfile CreateProfile(string folder, string name)
        {
            EnsureFolder(folder);

            var profile = ScriptableObject.CreateInstance<DungeonProfile>();
            AssetDatabase.CreateAsset(profile, AssetDatabase.GenerateUniqueAssetPath($"{folder}/{name}.asset"));
            AssetDatabase.SaveAssets();

            return profile;
        }

        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            string parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(folder);

            if (!string.IsNullOrEmpty(parent) && parent != "Assets")
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(string.IsNullOrEmpty(parent) ? "Assets" : parent, leaf);
        }

        public static void SetReference(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetList<T>(Object target, string property, IList<T> items) where T : Object
        {
            var serialized = new SerializedObject(target);
            WriteList(serialized.FindProperty(property), items);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }   

        /// <summary>Комнаты профиля: Root, Branches, Leaf (без дублей).</summary>
        public static List<RoomDefinition> GetProfileRooms(DungeonProfile profile)
        {
            var result = new List<RoomDefinition>();
            var so = new SerializedObject(profile);

            void Add(Object o)
            {
                if (o is RoomDefinition r && !result.Contains(r))
                    result.Add(r);
            }

            Add(so.FindProperty("_root").objectReferenceValue);

            var branches = so.FindProperty("_branches");
            for (int i = 0; i < branches.arraySize; i++)
                Add(branches.GetArrayElementAtIndex(i).objectReferenceValue);

            Add(so.FindProperty("_leaf").objectReferenceValue);
            return result;
        }

        public static bool UsesRoom(DungeonProfile profile, RoomDefinition room) =>
            ReplaceRoom(new SerializedObject(profile), room, false);

        /// <summary>Убирает комнату из профиля (root, leaf, branches, special rooms).</summary>
        public static void RemoveFromProfile(DungeonProfile profile, RoomDefinition room)
        {
            var so = new SerializedObject(profile);

            if (!ReplaceRoom(so, room, true))
                return;

            var branches = so.FindProperty("_branches");
            for (int i = branches.arraySize - 1; i >= 0; i--)
                if (branches.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    branches.DeleteArrayElementAtIndex(i);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(profile);
        }

        /// <summary>Очищает профиль. Сами комнаты остаются в проекте.</summary>
        public static void ClearProfile(DungeonProfile profile)
        {
            var so = new SerializedObject(profile);
            so.FindProperty("_root").objectReferenceValue = null;
            so.FindProperty("_leaf").objectReferenceValue = null;
            so.FindProperty("_branches").arraySize = 0;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(profile);
        }

        /// <summary>Удаляет RoomDefinition и префаб, предварительно убрав комнату из всех профилей.</summary>
        public static void DeleteRoom(RoomDefinition room)
        {
            foreach (var profile in FindProfiles())
                RemoveFromProfile(profile, room);

            string prefabPath = room.Prefab != null ? AssetDatabase.GetAssetPath(room.Prefab) : null;
            string definitionPath = AssetDatabase.GetAssetPath(room);

            if (!string.IsNullOrEmpty(prefabPath))
                AssetDatabase.DeleteAsset(prefabPath);

            AssetDatabase.DeleteAsset(definitionPath);
        }

        /// <summary>Удаляет все комнаты в папке вместе с самой папкой.</summary>
        public static void DeleteRoomsIn(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
                return;

            foreach (var guid in AssetDatabase.FindAssets("t:RoomDefinition", new[] { folder }))
            {
                var room = AssetDatabase.LoadAssetAtPath<RoomDefinition>(AssetDatabase.GUIDToAssetPath(guid));

                if (room != null)
                    DeleteRoom(room);
            }

            AssetDatabase.DeleteAsset(folder);
        }

        public static void DeleteProfile(DungeonProfile profile) =>
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(profile));

        public static void AddBranch(DungeonProfile profile, RoomDefinition room)
        {
            var so = new SerializedObject(profile);
            var branches = so.FindProperty("_branches");

            for (int i = 0; i < branches.arraySize; i++)
                if (branches.GetArrayElementAtIndex(i).objectReferenceValue == room)
                    return;

            branches.arraySize++;
            branches.GetArrayElementAtIndex(branches.arraySize - 1).objectReferenceValue = room;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(profile);
        }

        private static void WriteList<T>(SerializedProperty list, IList<T> items) where T : Object
        {
            list.arraySize = items.Count;

            for (int i = 0; i < items.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        private static List<T> FindAll<T>() where T : Object
        {
            var result = new List<T>();

            foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));

                if (asset != null)
                    result.Add(asset);
            }

            return result;
        }

        private static bool ReplaceRoom(SerializedObject so, Object room, bool remove)
        {
            bool found = false;
            var it = so.GetIterator();

            while (it.NextVisible(true))
            {
                if (it.propertyType != SerializedPropertyType.ObjectReference || it.objectReferenceValue != room)
                    continue;

                found = true;

                if (remove)
                    it.objectReferenceValue = null;
            }

            return found;
        }
    }
}
