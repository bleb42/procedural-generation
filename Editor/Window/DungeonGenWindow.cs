using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DungeonGen.Editor
{
    /// <summary>One window for everything: rooms, profiles, generation, the map and help.</summary>
    public sealed class DungeonGenWindow : EditorWindow
    {
        private const string DefaultFolder = "Assets/DungeonGen";
        private const string Version = "0.1.0";
        private const string RepositoryUrl = "https://github.com/bleb42";
        private const string ActiveProfileKey = "DungeonGen.ActiveProfile";

        private static readonly string[] TabNames = { "Generate", "Rooms", "Profile", "Map", "About" };

        private enum Tab
        {
            Generate,
            Rooms,
            Profile,
            Map,
            About
        }

        [SerializeField] private Tab _tab;
        [SerializeField] private RoomSpec _spec = new RoomSpec();
        [SerializeField] private string _folder = DefaultFolder;
        [SerializeField] private MapCorner _miniCorner = MapCorner.TopRight;
        [SerializeField] private float _miniSize = 220f;
        [SerializeField] private Transform _player;

        private DungeonBuilder _builder;
        private SerializedObject _builderObject;
        private UnityEditor.Editor _profileEditor;
        private Vector2 _scroll;
        private string _message;
        private MessageType _messageType;
        private List<string> _problems;
        private List<RoomDefinition> _rooms;
        private List<DungeonProfile> _profiles;

        private List<RoomDefinition> Rooms => _rooms ??= EditorAssets.FindRooms();
        private List<DungeonProfile> Profiles => _profiles ??= EditorAssets.FindProfiles();

        [MenuItem("Tools/Dungeon Gen/Open Window")]
        public static void Open()
        {
            var window = GetWindow<DungeonGenWindow>();
            window.titleContent = new GUIContent("Dungeon Gen");
            window.minSize = new Vector2(400f, 560f);
            window.Show();
        }

        private void OnEnable() => minSize = new Vector2(400f, 560f);

        private void OnDisable() => DestroyProfileEditor();

        private void OnProjectChange()
        {
            _rooms = null;
            _profiles = null;
            Repaint();
        }

        private void OnHierarchyChange()
        {
            _builder = null;
            Repaint();
        }

        private void OnGUI()
        {
            DungeonGenStyles.Ensure();

            DrawHeader();
            DrawTabs();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            GUILayout.Space(8f);

            switch (_tab)
            {
                case Tab.Generate: DrawGenerateTab(); break;
                case Tab.Rooms: DrawRoomsTab(); break;
                case Tab.Profile: DrawProfileTab(); break;
                case Tab.Map: DrawMapTab(); break;
                default: DrawAboutTab(); break;
            }

            GUILayout.Space(8f);
            EditorGUILayout.EndScrollView();

            DrawMessage();
        }

        // ------------------------------------------------------------------ chrome

        private void DrawHeader()
        {
            var rect = GUILayoutUtility.GetRect(0f, 58f, GUILayout.ExpandWidth(true));

            EditorGUI.DrawRect(rect, DungeonGenStyles.HeaderColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 4f, rect.height), DungeonGenStyles.Accent);

            GUI.Label(new Rect(rect.x + 16f, rect.y + 8f, rect.width - 32f, 26f), "DUNGEON GEN", DungeonGenStyles.Title);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 33f, rect.width - 32f, 18f),
                "Grid-free dungeon generator", DungeonGenStyles.Subtitle);

            var versionStyle = new GUIStyle(DungeonGenStyles.Subtitle) { alignment = TextAnchor.MiddleRight };
            GUI.Label(new Rect(rect.x + 16f, rect.y + 20f, rect.width - 32f, 18f), "v" + Version, versionStyle);
        }

        private void DrawTabs()
        {
            var rect = GUILayoutUtility.GetRect(0f, 34f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, DungeonGenStyles.TabBarColor);

            float width = rect.width / TabNames.Length;

            for (int i = 0; i < TabNames.Length; i++)
            {
                var tabRect = new Rect(rect.x + i * width, rect.y, width, rect.height);
                bool active = i == (int)_tab;

                if (GUI.Button(tabRect, TabNames[i], active ? DungeonGenStyles.TabActive : DungeonGenStyles.Tab))
                {
                    _tab = (Tab)i;
                    _scroll = Vector2.zero;
                    GUI.FocusControl(null);
                }

                if (active)
                    EditorGUI.DrawRect(new Rect(tabRect.x + 10f, tabRect.yMax - 3f, tabRect.width - 20f, 3f), DungeonGenStyles.Accent);
            }
        }

        private void DrawMessage()
        {
            if (string.IsNullOrEmpty(_message))
                return;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox(_message, _messageType);

            if (GUILayout.Button("x", GUILayout.Width(22f), GUILayout.Height(38f)))
                _message = null;

            EditorGUILayout.EndHorizontal();
        }

        private void Notify(string message, MessageType type)
        {
            _message = message;
            _messageType = type;
            Repaint();
        }

        // ------------------------------------------------------------------ generate

        private void DrawGenerateTab()
        {
            FindBuilder();
            DrawQuickStartCard();
            DrawSceneCard();

            if (_builder == null)
                return;

            DrawSettingsCard();
            DrawActionsCard();
            DrawResultCard();
        }

        private void DrawQuickStartCard()
        {
            bool hasRooms = Rooms.Count > 0;
            bool hasProfile = Profiles.Count > 0;
            bool hasBuilder = _builder != null;

            if (hasRooms && hasProfile && hasBuilder && _builder.Profile != null)
                return;

            DungeonGenStyles.BeginCard("Quick start");

            GUILayout.Label("Everything you need for a first dungeon:", DungeonGenStyles.Hint);
            GUILayout.Label((hasRooms ? "[x] " : "[ ] ") + "Rooms (prefab + RoomDefinition, baked)", DungeonGenStyles.Hint);
            GUILayout.Label((hasProfile ? "[x] " : "[ ] ") + "Dungeon profile", DungeonGenStyles.Hint);
            GUILayout.Label((hasBuilder ? "[x] " : "[ ] ") + "Dungeon Builder in the scene", DungeonGenStyles.Hint);

            GUILayout.Space(6f);

            if (DungeonGenStyles.Button("Quick Setup (starter rooms, profile, builder)", ButtonKind.Primary, GUILayout.Height(34f)))
                QuickSetup(hasRooms, hasProfile);

            DungeonGenStyles.EndCard();
        }

        private void QuickSetup(bool hasRooms, bool hasProfile)
        {
            DungeonProfile profile = null;

            if (!hasRooms || !hasProfile)
            {
                profile = RoomFactory.CreateStarterSet(_spec, _folder, out string error);

                if (profile == null)
                {
                    Notify(error, MessageType.Error);
                    return;
                }
            }
            else
            {
                profile = Profiles[0];
            }

            if (_builder == null)
                _builder = SceneSetup.CreateBuilder(profile);
            else if (_builder.Profile == null)
                EditorAssets.SetReference(_builder, "_profile", profile);

            _builderObject = null;
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(profile);
            Notify("Setup done. Press Generate.", MessageType.Info);
        }

        private void DrawSceneCard()
        {
            DungeonGenStyles.BeginCard("Dungeon Builder");

            if (_builder == null)
            {
                GUILayout.Label("There is no Dungeon Builder in the open scene.", DungeonGenStyles.Hint);

                if (DungeonGenStyles.Button("Create Dungeon Builder", ButtonKind.Secondary))
                {
                    _builder = SceneSetup.CreateBuilder(Profiles.Count > 0 ? Profiles[0] : null);
                    _builderObject = null;
                }
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(_builder, typeof(DungeonBuilder), true);

                if (GUILayout.Button("Select", GUILayout.Width(60f)))
                    Selection.activeGameObject = _builder.gameObject;

                EditorGUILayout.EndHorizontal();
            }

            DungeonGenStyles.EndCard();
        }

        private void DrawSettingsCard()
        {
            if (_builderObject == null || _builderObject.targetObject != _builder)
                _builderObject = new SerializedObject(_builder);

            _builderObject.Update();

            DungeonGenStyles.BeginCard("Settings");

            EditorGUILayout.PropertyField(_builderObject.FindProperty("_profile"));
            EditorGUILayout.PropertyField(_builderObject.FindProperty("_roomsRoot"));

            var useFixedSeed = _builderObject.FindProperty("_useFixedSeed");
            var fixedSeed = _builderObject.FindProperty("_fixedSeed");

            EditorGUILayout.PropertyField(useFixedSeed, new GUIContent("Fixed Seed"));

            if (useFixedSeed.boolValue)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(fixedSeed, new GUIContent("Seed"));

                if (GUILayout.Button("Random", GUILayout.Width(64f)))
                    fixedSeed.intValue = NewSeed();

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.PropertyField(_builderObject.FindProperty("_msPerFrame"), new GUIContent("Ms Per Frame"));
            EditorGUILayout.PropertyField(_builderObject.FindProperty("_generateOnStart"));

            DungeonGenStyles.EndCard();

            _builderObject.ApplyModifiedProperties();
        }

        private void DrawActionsCard()
        {
            DungeonGenStyles.BeginCard("Generate");

            EditorGUILayout.BeginHorizontal();

            if (DungeonGenStyles.Button("Generate", ButtonKind.Primary, GUILayout.Height(34f)))
                Generate(null);

            if (DungeonGenStyles.Button("Random Seed", ButtonKind.Secondary, GUILayout.Height(34f)))
                Generate(NewSeed());

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (DungeonGenStyles.Button("Validate Profile", ButtonKind.Secondary))
                ValidateProfile();

            if (DungeonGenStyles.Button("Clear", ButtonKind.Danger))
                ClearDungeon();

            EditorGUILayout.EndHorizontal();

            if (!Application.isPlaying)
            {
                GUILayout.Space(4f);
                GUILayout.Label("In edit mode the dungeon is built in one go and saved with the scene. " +
                                "Press Clear before you commit.", DungeonGenStyles.Hint);
            }

            DrawProblems();

            DungeonGenStyles.EndCard();
        }

        private void DrawProblems()
        {
            if (_problems == null)
                return;

            GUILayout.Space(4f);

            if (_problems.Count == 0)
            {
                EditorGUILayout.HelpBox("The profile is valid.", MessageType.Info);
                return;
            }

            foreach (var problem in _problems)
                EditorGUILayout.HelpBox(problem, MessageType.Error);
        }

        private void DrawResultCard()
        {
            var result = _builder.Current;

            DungeonGenStyles.BeginCard("Result");

            if (result == null)
            {
                GUILayout.Label("No dungeon generated yet.", DungeonGenStyles.Hint);
                DungeonGenStyles.EndCard();
                return;
            }

            int deepest = 0;

            foreach (var room in result.Rooms)
                deepest = Mathf.Max(deepest, room.Depth);

            EditorGUILayout.BeginHorizontal();
            DrawStat($"{result.Layout.PlacedRoomCount}/{result.Layout.RequestedRoomCount}", "rooms");
            DrawStat(deepest.ToString(), "depth");
            DrawStat(result.Seed.ToString(), "seed");
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();

            if (DungeonGenStyles.Button("Copy Seed", ButtonKind.Secondary))
                GUIUtility.systemCopyBuffer = result.Seed.ToString();

            if (DungeonGenStyles.Button("Use This Seed", ButtonKind.Secondary))
                UseSeed(result.Seed);

            if (DungeonGenStyles.Button("Frame", ButtonKind.Secondary))
                Frame(result.Root);

            EditorGUILayout.EndHorizontal();

            foreach (var warning in result.Layout.Warnings)
                EditorGUILayout.HelpBox(warning, MessageType.Warning);

            DungeonGenStyles.EndCard();
        }

        private static void DrawStat(string value, string label)
        {
            EditorGUILayout.BeginVertical();
            GUILayout.Label(value, DungeonGenStyles.StatValue);
            GUILayout.Label(label, DungeonGenStyles.StatLabel);
            EditorGUILayout.EndVertical();
        }

        private void Generate(int? seed)
        {
            if (!HasUsableProfile())
                return;

            if (Application.isPlaying)
            {
                var routine = seed.HasValue ? _builder.Generate(seed.Value) : _builder.Generate();
                Notify(routine == null ? "Generation is already running." : "Generating over several frames...",
                    routine == null ? MessageType.Warning : MessageType.Info);
                return;
            }

            var result = _builder.GenerateImmediate(seed);

            if (result == null)
            {
                Notify("Generation failed. See the Console.", MessageType.Error);
                return;
            }

            EditorSceneManager.MarkSceneDirty(_builder.gameObject.scene);
            Notify($"Generated {result.Layout.PlacedRoomCount} rooms (seed {result.Seed}).", MessageType.Info);
        }

        private void ClearDungeon()
        {
            _builder.Clear();

            if (!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(_builder.gameObject.scene);

            Notify("Dungeon cleared.", MessageType.Info);
        }

        private bool HasUsableProfile()
        {
            if (_builder.Profile == null)
            {
                Notify("Assign a Dungeon Profile first (Settings, or the Profile tab).", MessageType.Warning);
                return false;
            }

            return true;
        }

        private void ValidateProfile()
        {
            _problems = new List<string>();

            if (_builder.Profile == null)
                _problems.Add("No Dungeon Profile is assigned.");
            else
                _builder.Profile.Validate(_problems);
        }

        private void UseSeed(int seed)
        {
            var serialized = new SerializedObject(_builder);
            serialized.FindProperty("_useFixedSeed").boolValue = true;
            serialized.FindProperty("_fixedSeed").intValue = seed;
            serialized.ApplyModifiedProperties();
        }

        private static void Frame(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0 || SceneView.lastActiveSceneView == null)
                return;

            var bounds = renderers[0].bounds;

            foreach (var renderer in renderers)
                bounds.Encapsulate(renderer.bounds);

            SceneView.lastActiveSceneView.Frame(bounds, false);
        }

        private static int NewSeed() => System.Guid.NewGuid().GetHashCode();

        private void FindBuilder()
        {
            if (_builder == null)
                _builder = FindFirstObjectByType<DungeonBuilder>();
        }

        // ------------------------------------------------------------------ rooms

        private void DrawRoomsTab()
        {
            DrawCreateRoomCard();
            DrawStarterCard();
            DrawLibraryCard();
        }

        private void DrawCreateRoomCard()
        {
            DungeonGenStyles.BeginCard("Create a room");

            _spec.Name = EditorGUILayout.TextField("Name", _spec.Name);

            EditorGUILayout.BeginHorizontal();
            _spec.SizeX = EditorGUILayout.FloatField("Size X / Z", _spec.SizeX);
            _spec.SizeZ = EditorGUILayout.FloatField(_spec.SizeZ, GUILayout.Width(60f));
            EditorGUILayout.EndHorizontal();

            _spec.Height = EditorGUILayout.FloatField("Height", _spec.Height);
            _spec.DoorWidth = EditorGUILayout.FloatField("Door Width", _spec.DoorWidth);

            GUILayout.Space(4f);
            GUILayout.Label("Doors", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _spec.North = GUILayout.Toggle(_spec.North, "North", GUILayout.Width(70f));
            _spec.East = GUILayout.Toggle(_spec.East, "East", GUILayout.Width(70f));
            _spec.South = GUILayout.Toggle(_spec.South, "South", GUILayout.Width(70f));
            _spec.West = GUILayout.Toggle(_spec.West, "West", GUILayout.Width(70f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Straight")) _spec.SetDoors(true, false, true, false);
            if (GUILayout.Button("Corner")) _spec.SetDoors(true, true, false, false);
            if (GUILayout.Button("T")) _spec.SetDoors(true, true, false, true);
            if (GUILayout.Button("Cross")) _spec.SetDoors(true, true, true, true);
            if (GUILayout.Button("Dead End")) _spec.SetDoors(false, false, true, false);

            EditorGUILayout.EndHorizontal();

            _folder = EditorGUILayout.TextField("Folder", _folder);

            GUILayout.Space(6f);

            if (DungeonGenStyles.Button("Create Room", ButtonKind.Primary))
                CreateRoom();

            GUILayout.Space(2f);
            GUILayout.Label("Makes a box room with a floor, walls and door sockets, then bakes it. " +
                            "For other shapes build the prefab yourself (see the README).", DungeonGenStyles.Hint);

            DungeonGenStyles.EndCard();
        }

        private void CreateRoom()
        {
            var room = RoomFactory.Create(_spec, _folder + "/Rooms", out string error);

            if (room == null)
            {
                Notify(error, MessageType.Error);
                return;
            }

            AssetDatabase.Refresh();
            Selection.activeObject = room;
            EditorGUIUtility.PingObject(room);
            Notify($"Room '{room.name}' created and baked.", MessageType.Info);
        }

        private void DrawStarterCard()
        {
            DungeonGenStyles.BeginCard("Starter set");

            GUILayout.Label("Six rooms of different sizes and a profile that uses them. " +
                "Pressing the button again replaces the previous starter set (folder 'Starter'); " +
                "your own rooms are not touched.", DungeonGenStyles.Hint);

            GUILayout.Space(4f);

            if (DungeonGenStyles.Button("Create Starter Set", ButtonKind.Secondary))
            {
                var profile = RoomFactory.CreateStarterSet(_spec, _folder, out string error);

                if (profile == null)
                {
                    Notify(error, MessageType.Error);
                }
                else
                {
                    AssetDatabase.Refresh();
                    RefreshAssets();
                    ActiveProfile = profile;
                    Selection.activeObject = profile;
                    Notify("Starter set created and selected as the active profile.", MessageType.Info);
                }
            }

            DungeonGenStyles.EndCard();
        }

        private void DrawLibraryCard()
        {
            var rooms = Rooms;

            DungeonGenStyles.BeginCard($"Room library ({rooms.Count})");

            if (rooms.Count == 0)
            {
                GUILayout.Label("No RoomDefinition assets in the project yet.", DungeonGenStyles.Hint);
                DungeonGenStyles.EndCard();
                return;
            }

            foreach (var room in rooms)
                DrawRoomRow(room);

            GUILayout.Space(6f);

            if (DungeonGenStyles.Button("Bake All", ButtonKind.Secondary))
                BakeAll(rooms);

            if (DungeonGenStyles.Button("Delete Unused Rooms", ButtonKind.Danger))
                DeleteUnusedRooms(rooms);

            DungeonGenStyles.EndCard();
        }

        private void DrawRoomRow(RoomDefinition room)
        {
            var status = EditorAssets.GetStatus(room);

            EditorGUILayout.BeginHorizontal(DungeonGenStyles.Row);

            if (GUILayout.Button(room.name, EditorStyles.label, GUILayout.MinWidth(100f)))
                EditorGUIUtility.PingObject(room);

            GUILayout.FlexibleSpace();

            GUILayout.Label($"{room.Doors.Count} doors", DungeonGenStyles.Hint, GUILayout.Width(60f));

            switch (status)
            {
                case RoomStatus.Ready: DungeonGenStyles.Pill("READY", DungeonGenStyles.Accent); break;
                case RoomStatus.NotBaked: DungeonGenStyles.Pill("NOT BAKED", DungeonGenStyles.Warning); break;
                case RoomStatus.Outdated: DungeonGenStyles.Pill("OUTDATED", DungeonGenStyles.Warning); break;
                default: DungeonGenStyles.Pill("NO PREFAB", DungeonGenStyles.Danger); break;
            }

            using (new EditorGUI.DisabledScope(room.Prefab == null))
            {
                if (GUILayout.Button("Bake", GUILayout.Width(46f)))
                    BakeOne(room);

                if (GUILayout.Button("Open", GUILayout.Width(46f)))
                    AssetDatabase.OpenAsset(room.Prefab);
            }

            if (GUILayout.Button("Delete", GUILayout.Width(52f)))
                DeleteRoom(room);

            EditorGUILayout.EndHorizontal();
        }

        private void BakeOne(RoomDefinition room)
        {
            bool ok = EditorAssets.Bake(room, out string message);
            Notify(message, ok ? MessageType.Info : MessageType.Error);
        }

        private void BakeAll(List<RoomDefinition> rooms)
        {
            int baked = 0;
            var failures = new List<string>();

            foreach (var room in rooms)
            {
                if (EditorAssets.Bake(room, out string message))
                    baked++;
                else
                    failures.Add(message);
            }

            if (failures.Count == 0)
                Notify($"Baked {baked} rooms.", MessageType.Info);
            else
                Notify($"Baked {baked}, failed {failures.Count}:\n" + string.Join("\n", failures), MessageType.Warning);
        }

        // ------------------------------------------------------------------ profile

        private void DrawProfileTab()
        {
            FindBuilder();

            var profiles = Profiles;
            var profile = ActiveProfile;

            DungeonGenStyles.BeginCard("Active profile");

            if (profiles.Count > 0)
            {
                var names = new string[profiles.Count];
                int selected = -1;

                for (int i = 0; i < profiles.Count; i++)
                {
                    names[i] = profiles[i].name;

                    if (profiles[i] == profile)
                        selected = i;
                }

                int picked = EditorGUILayout.Popup("Profile", selected, names);

                if (picked >= 0 && picked != selected)
                {
                    ActiveProfile = profiles[picked];
                    profile = profiles[picked];
                }
            }

            if (profile != null)
                GUILayout.Label(AssetDatabase.GetAssetPath(profile), DungeonGenStyles.Hint);

            GUILayout.Label(_builder == null
                    ? "No Dungeon Builder in the scene: the profile is not used by anything yet."
                    : "Dungeon Builder in the scene uses the active profile.",
                DungeonGenStyles.Hint);

            EditorGUILayout.BeginHorizontal();

            if (DungeonGenStyles.Button("New", ButtonKind.Secondary))
            {
                var created = EditorAssets.CreateProfile(_folder, "Dungeon Profile");
                RefreshAssets();
                ActiveProfile = created;
                profile = created;
                Selection.activeObject = created;
            }

            using (new EditorGUI.DisabledScope(profile == null))
            {
                if (DungeonGenStyles.Button("Fill From Rooms", ButtonKind.Secondary))
                {
                    EditorAssets.FillProfile(profile, EditorAssets.FindRooms());
                    Notify("Root = room with most doors, branches = rooms with 2+ doors, leaf = a one-door room.", MessageType.Info);
                }

                if (DungeonGenStyles.Button("Validate", ButtonKind.Secondary))
                {
                    _problems = new List<string>();
                    profile.Validate(_problems);
                }
            }

            EditorGUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(profile == null))
            {
                EditorGUILayout.BeginHorizontal();

                if (DungeonGenStyles.Button("Clear Rooms", ButtonKind.Secondary) &&
                    EditorUtility.DisplayDialog("Clear profile",
                        $"Remove all rooms from '{profile.name}'? The room assets stay in the project.", "Clear", "Cancel"))
                {
                    EditorAssets.ClearProfile(profile);
                    GUIUtility.ExitGUI();
                }

                if (DungeonGenStyles.Button("Delete Profile", ButtonKind.Danger) &&
                    EditorUtility.DisplayDialog("Delete profile",
                        $"Delete the asset '{profile.name}'? The rooms stay in the project.", "Delete", "Cancel"))
                {
                    DestroyProfileEditor();
                    EditorAssets.DeleteProfile(profile);
                    RefreshAssets();
                    ActiveProfile = null;
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }

            DrawProblems();
            DungeonGenStyles.EndCard();

            if (profile == null)
            {
                DestroyProfileEditor();
                GUILayout.Label("No active profile. Pick one above or create it with New.", DungeonGenStyles.Hint);
                return;
            }

            DungeonGenStyles.BeginCard($"Rooms in '{profile.name}'");

            var inProfile = EditorAssets.GetProfileRooms(profile);

            if (inProfile.Count == 0)
                GUILayout.Label("Empty. Use Fill From Rooms or add rooms below.", DungeonGenStyles.Hint);

            foreach (var room in inProfile)
            {
                EditorGUILayout.BeginHorizontal(DungeonGenStyles.Row);

                if (GUILayout.Button(room.name, EditorStyles.label))
                    EditorGUIUtility.PingObject(room);

                if (GUILayout.Button("Remove", GUILayout.Width(64f)))
                {
                    EditorAssets.RemoveFromProfile(profile, room);
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(4f);
            GUILayout.Label("Add to branches", EditorStyles.boldLabel);

            foreach (var room in Rooms)
            {
                if (inProfile.Contains(room))
                    continue;

                EditorGUILayout.BeginHorizontal(DungeonGenStyles.Row);
                GUILayout.Label(room.name);

                if (GUILayout.Button("Add", GUILayout.Width(44f)))
                {
                    EditorAssets.AddBranch(profile, room);
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }

            DungeonGenStyles.EndCard();

            DungeonGenStyles.BeginCard("Edit profile");

            if (_profileEditor == null || _profileEditor.target != profile)
            {
                DestroyProfileEditor();
                _profileEditor = UnityEditor.Editor.CreateEditor(profile);
            }

            _profileEditor.OnInspectorGUI();

            DungeonGenStyles.EndCard();
        }

        private void DestroyProfileEditor()
        {
            if (_profileEditor == null)
                return;

            DestroyImmediate(_profileEditor);
            _profileEditor = null;
        }

        // ------------------------------------------------------------------ map

        private void DrawMapTab()
        {
            FindBuilder();

            DungeonGenStyles.BeginCard("Minimap");

            GUILayout.Label("Creates a Canvas, the map views and a MapPanel that connects the builder with the player.",
                DungeonGenStyles.Hint);

            GUILayout.Space(4f);

            if (_player == null)
            {
                var found = GameObject.FindGameObjectWithTag("Player");

                if (found != null)
                    _player = found.transform;
            }

            _player = (Transform)EditorGUILayout.ObjectField("Player", _player, typeof(Transform), true);
            _miniCorner = (MapCorner)EditorGUILayout.EnumPopup("Mini map corner", _miniCorner);
            _miniSize = EditorGUILayout.Slider("Mini map size", _miniSize, 120f, 400f);

            GUILayout.Space(4f);

            if (_builder == null)
                EditorGUILayout.HelpBox("There is no Dungeon Builder in the scene yet.", MessageType.Warning);

            if (_player == null)
                EditorGUILayout.HelpBox("Pick the player object. The map follows it.", MessageType.Warning);

            using (new EditorGUI.DisabledScope(_builder == null || _player == null))
            {
                EditorGUILayout.BeginHorizontal();

                if (DungeonGenStyles.Button("Create Mini Map", ButtonKind.Primary))
                {
                    SceneSetup.CreateMiniMap(_builder, _player, _miniCorner, _miniSize);
                    Notify("Mini map created.", MessageType.Info);
                }

                if (DungeonGenStyles.Button("Create Full Map", ButtonKind.Secondary))
                {
                    SceneSetup.CreateFullMap(_builder, _player);
                    Notify("Full map created. It starts hidden; enable it from your own code (for example on the M key).",
                        MessageType.Info);
                }

                EditorGUILayout.EndHorizontal();
            }

            DungeonGenStyles.EndCard();

            DungeonGenStyles.BeginCard("Style");

            GUILayout.Label("Colors, scale and gaps live in a Map Style asset. Assign it on the map views.",
                DungeonGenStyles.Hint);

            if (DungeonGenStyles.Button("Create Map Style Asset", ButtonKind.Secondary))
            {
                EditorAssets.EnsureFolder(_folder);

                var style = CreateInstance<MapStyleData>();
                AssetDatabase.CreateAsset(style, AssetDatabase.GenerateUniqueAssetPath(_folder + "/Map Style.asset"));
                AssetDatabase.SaveAssets();
                Selection.activeObject = style;
            }

            DungeonGenStyles.EndCard();
        }

        // ------------------------------------------------------------------ about

        private void DrawAboutTab()
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(DungeonBuilder).Assembly);

            DungeonGenStyles.BeginCard("Dungeon Gen " + Version);

            GUILayout.Label("Grid-free procedural dungeons from your own room prefabs. " +
                            "Rooms are measured, not snapped to a grid, so any size works.", DungeonGenStyles.Hint);

            DungeonGenStyles.EndCard();

            DungeonGenStyles.BeginCard("Links");

            if (DungeonGenStyles.Button("GitHub", ButtonKind.Secondary))
                Application.OpenURL(RepositoryUrl);

            if (DungeonGenStyles.Button("Report an issue", ButtonKind.Secondary))
                Application.OpenURL(RepositoryUrl);

            using (new EditorGUI.DisabledScope(package == null))
            {
                if (DungeonGenStyles.Button("Open README", ButtonKind.Secondary))
                    EditorUtility.OpenWithDefaultApp(package.resolvedPath + "/README.md");

                if (DungeonGenStyles.Button("Show package folder", ButtonKind.Secondary))
                    EditorUtility.RevealInFinder(package.resolvedPath);
            }

            DungeonGenStyles.EndCard();

            DungeonGenStyles.BeginCard("Development");

            if (DungeonGenStyles.Button("Open Test Runner", ButtonKind.Secondary))
                EditorApplication.ExecuteMenuItem("Window/General/Test Runner");

            DungeonGenStyles.EndCard();
        }

        /// <summary>Профиль, который мы сейчас настраиваем. Если в сцене есть Builder с профилем, это он.</summary>
        private DungeonProfile ActiveProfile
        {
            get
            {
                FindBuilder();

                if (_builder != null && _builder.Profile != null)
                    return _builder.Profile;

                string guid = EditorPrefs.GetString(ActiveProfileKey, "");

                return string.IsNullOrEmpty(guid)
                    ? null
                    : AssetDatabase.LoadAssetAtPath<DungeonProfile>(AssetDatabase.GUIDToAssetPath(guid));
            }
            set
            {
                EditorPrefs.SetString(ActiveProfileKey,
                    value != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value)) : "");

                if (_builder != null)
                    EditorAssets.SetReference(_builder, "_profile", value);

                _problems = null;
            }
        }

        private void RefreshAssets()
        {
            _rooms = null;
            _profiles = null;
        }

        private void DeleteRoom(RoomDefinition room)
        {
            var usedBy = new List<string>();

            foreach (var profile in Profiles)
                if (EditorAssets.UsesRoom(profile, room))
                    usedBy.Add(profile.name);

            string text = usedBy.Count == 0
                ? $"Delete '{room.name}' and its prefab?"
                : $"Delete '{room.name}' and its prefab? It will be removed from: {string.Join(", ", usedBy)}.";

            if (!EditorUtility.DisplayDialog("Delete room", text, "Delete", "Cancel"))
                return;

            EditorAssets.DeleteRoom(room);
            RefreshAssets();
            GUIUtility.ExitGUI();
        }

        private void DeleteUnusedRooms(List<RoomDefinition> rooms)
        {
            var unused = new List<RoomDefinition>();

            foreach (var room in rooms)
            {
                bool used = false;

                foreach (var profile in Profiles)
                {
                    if (!EditorAssets.UsesRoom(profile, room))
                        continue;

                    used = true;
                    break;
                }

                if (!used)
                    unused.Add(room);
            }

            if (unused.Count == 0)
            {
                Notify("Every room is used by some profile.", MessageType.Info);
                return;
            }

            if (!EditorUtility.DisplayDialog("Delete unused rooms",
                    $"Delete {unused.Count} rooms that no profile uses, with their prefabs?", "Delete", "Cancel"))
                return;

            foreach (var room in unused)
                EditorAssets.DeleteRoom(room);

            RefreshAssets();
            GUIUtility.ExitGUI();
        }
    }
}
