using UnityEngine;

namespace DungeonGen
{
    [CreateAssetMenu(fileName = "MapStyle", menuName = "DungeonGen/Map Style")]
    public class MapStyleData : ScriptableObject
    {
        [Header("Size")]
        [Tooltip("How many UI pixels one world unit takes on the map.")]
        [field: SerializeField] public float PixelsPerUnit { get; private set; } = 6f;

        [Tooltip("Gap between neighbouring rooms, in pixels.")]
        [field: SerializeField] public float RoomGap { get; private set; } = 2f;

        [Tooltip("Length of a door marker along the wall, in world units.")]
        [field: SerializeField] public float DoorMarkerLength { get; private set; } = 1.6f;

        [Header("Unknown rooms")]
        [Tooltip("How far an unvisited room is revealed behind a door, in world units.")]
        [field: SerializeField] public float UnknownDepth { get; private set; } = 4f;

        [Tooltip("How wide the revealed part is, in world units.")]
        [field: SerializeField] public float UnknownWidth { get; private set; } = 4f;

        [Header("Colors")]
        [field: SerializeField] public Color VisitedColor { get; private set; } = new(0.55f, 0.55f, 0.62f, 1f);
        [field: SerializeField] public Color CurrentColor { get; private set; } = new(0.9f, 0.9f, 0.95f, 1f);
        [field: SerializeField] public Color DoorColor { get; private set; } = new(0.9f, 0.9f, 0.95f, 1f);
        [field: SerializeField] public Color UnknownColor { get; private set; } = new(0.3f, 0.3f, 0.36f, 0.8f);
        [field: SerializeField] public Color StartColor { get; private set; } = new(0.3f, 0.8f, 0.4f);
    }
}
