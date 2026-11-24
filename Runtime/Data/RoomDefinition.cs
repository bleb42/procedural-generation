using System.Collections.Generic;
using UnityEngine;

namespace DungeonGen
{
    /// <summary>A room prefab together with its baked floor rectangles and doors.</summary>
    [CreateAssetMenu(fileName = "RoomDefinition", menuName = "DungeonGen/Room Definition")]
    public class RoomDefinition : ScriptableObject, IRoomBlueprint
    {
        [SerializeField] private GameObject _prefab;

        [Tooltip("Filled by the RoomBaker. Do not edit by hand.")]
        [SerializeField] private List<FloorRect> _floorRects = new();

        [Tooltip("Filled by the RoomBaker. The order matches RoomInstance.DoorSockets.")]
        [SerializeField] private List<DoorDef> _doors = new();

        [Tooltip("Free-form value for your own systems, for example an enemy budget per room.")]
        [SerializeField, Min(0f)] private float _weight = 1f;

        public GameObject Prefab => _prefab;
        public string Name => name;
        public IReadOnlyList<FloorRect> FloorRects => _floorRects;
        public IReadOnlyList<DoorDef> Doors => _doors;
        public float Weight => _weight;

#if UNITY_EDITOR
        public void SetBakedData(IEnumerable<FloorRect> floorRects, IEnumerable<DoorDef> doors)
        {
            _floorRects = new List<FloorRect>(floorRects);
            _doors = new List<DoorDef>(doors);
        }
#endif
    }
}
