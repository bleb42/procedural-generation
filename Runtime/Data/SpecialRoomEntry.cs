using System;
using UnityEngine;

namespace DungeonGen
{
    [Serializable]
    public class SpecialRoomEntry
    {
        public RoomDefinition Room;

        [Tooltip("1 = always attempted (still not guaranteed to fit somewhere). Lower = rolled once per generation.")]
        [Range(0f, 1f)] public float Chance = 1f;

        public SpecialRoomPlacement Placement = SpecialRoomPlacement.Anywhere;
    }
}
