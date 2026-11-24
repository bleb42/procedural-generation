using System.Collections.Generic;
using UnityEngine;

namespace DungeonGen
{
    /// <summary>Root component of a room prefab. Holds its door and wall sockets.</summary>
    public class RoomInstance : MonoBehaviour
    {
        [Tooltip("The order must match the doors baked into the RoomDefinition.")]
        [SerializeField] private List<DoorSocket> _doorSockets = new();

        [SerializeField] private List<WallSocket> _wallSockets = new();

        public IReadOnlyList<DoorSocket> DoorSockets => _doorSockets;
        public IReadOnlyList<WallSocket> WallSockets => _wallSockets;

#if UNITY_EDITOR
        [ContextMenu("Collect Sockets From Children")]
        private void CollectSockets()
        {
            UnityEditor.Undo.RecordObject(this, "Collect Sockets");

            _doorSockets = new List<DoorSocket>(GetComponentsInChildren<DoorSocket>(true));
            _wallSockets = new List<WallSocket>(GetComponentsInChildren<WallSocket>(true));

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
