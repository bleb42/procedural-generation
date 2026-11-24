using System.Collections.Generic;
using UnityEngine;

namespace DungeonGen.Tests
{
    internal sealed class TestBlueprint : IRoomBlueprint
    {
        public string Name { get; }
        public IReadOnlyList<FloorRect> FloorRects { get; }
        public IReadOnlyList<DoorDef> Doors { get; }
        public float Weight => 1f;

        public TestBlueprint(string name, IReadOnlyList<FloorRect> floorRects, IReadOnlyList<DoorDef> doors)
        {
            Name = name;
            FloorRects = floorRects;
            Doors = doors;
        }

        /// <summary>A rectangular room centered on its origin. Pass the sides in the order North, East, South, West.</summary>
        public static TestBlueprint Rect(string name, float sizeX, float sizeZ, params Direction[] sides)
        {
            var doors = new List<DoorDef>();

            foreach (var side in sides)
            {
                var direction = DirectionUtil.ToVector(side);
                doors.Add(new DoorDef(new Vector2(direction.x * sizeX * 0.5f, direction.y * sizeZ * 0.5f), side));
            }

            return new TestBlueprint(name, new[] { new FloorRect(Vector2.zero, new Vector2(sizeX, sizeZ)) }, doors);
        }

        /// <summary>A square room centered on its origin with a door in the middle of each given side.</summary>
        public static TestBlueprint Square(string name, float size, params Direction[] sides)
        {
            var doors = new List<DoorDef>();

            foreach (var side in sides)
                doors.Add(new DoorDef(DirectionUtil.ToVector(side) * (size * 0.5f), side));

            return new TestBlueprint(name, new[] { new FloorRect(Vector2.zero, new Vector2(size, size)) }, doors);
        }
    }
}
