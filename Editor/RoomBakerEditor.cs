using UnityEditor;
using UnityEngine;

namespace DungeonGen.Editor
{
    [CustomEditor(typeof(RoomBaker))]
    public class RoomBakerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var baker = (RoomBaker)target;

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(baker.Definition == null))
            {
                if (GUILayout.Button("Bake", GUILayout.Height(28f)))
                    baker.Bake();
            }

            EditorGUILayout.HelpBox(
                "Bake measures every FloorArea and DoorSocket of this prefab and writes the result into the " +
                "RoomDefinition. Bake again after you change the room.",
                MessageType.Info);
        }
    }
}
