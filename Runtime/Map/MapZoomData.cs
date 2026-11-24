using UnityEngine;

namespace DungeonGen
{
    [CreateAssetMenu(fileName = "MapZoom", menuName = "DungeonGen/Map Zoom")]
    public class MapZoomData : ScriptableObject
    {
        [Header("Zoom")]
        [field: SerializeField] public float StartZoom { get; private set; } = 1f;
        [field: SerializeField] public float MinZoom { get; private set; } = 0.4f;
        [field: SerializeField] public float MaxZoom { get; private set; } = 3f;
        [field: SerializeField] public float ZoomFactor { get; private set; } = 1.15f;

        [Header("Fit")]
        [field: SerializeField, Range(0.3f, 1f)] public float FitMargin { get; private set; } = 0.85f;
    }
}
