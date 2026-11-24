using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DungeonGen
{
    /// <summary>
    /// Put this on the root of a room prefab, assign its RoomDefinition and press Bake.
    /// It measures the FloorAreas and DoorSockets and stores the result in the definition.
    /// </summary>
    [RequireComponent(typeof(RoomInstance))]
    [DisallowMultipleComponent]
    public class RoomBaker : MonoBehaviour
    {
        [Tooltip("The RoomDefinition asset that receives the baked floor rectangles and doors.")]
        [SerializeField] private RoomDefinition _definition;

#if UNITY_EDITOR
        public RoomDefinition Definition => _definition;

        [ContextMenu("Bake")]
        public bool Bake()
        {
            if (_definition == null)
            {
                Debug.LogError($"{name}: no RoomDefinition assigned, nothing to bake into.", this);
                return false;
            }

            var floors = new List<FloorRect>();
            var doors = new List<DoorDef>();
            var problems = new List<string>();

            if (!RoomMeasure.Measure(GetComponent<RoomInstance>(), floors, doors, problems))
            {
                foreach (var problem in problems)
                    Debug.LogError($"{name}: {problem}", this);

                Debug.LogError($"{name}: bake aborted, nothing was written.", this);
                return false;
            }

            Undo.RecordObject(_definition, "Bake Room");
            _definition.SetBakedData(floors, doors);
            EditorUtility.SetDirty(_definition);
            AssetDatabase.SaveAssets();

            Debug.Log($"{name}: baked into '{_definition.name}' ({floors.Count} floor rects, {doors.Count} doors).", _definition);
            return true;
        }
#endif
    }
}
