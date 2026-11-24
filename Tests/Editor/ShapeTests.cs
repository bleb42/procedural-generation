using NUnit.Framework;
using UnityEngine;

namespace DungeonGen.Tests
{
    public class ShapeTests
    {
        [Test]
        public void FloorRect_TouchingRectsDoNotOverlap()
        {
            var a = new FloorRect(new Vector2(0f, 0f), new Vector2(4f, 4f));
            var b = new FloorRect(new Vector2(4f, 0f), new Vector2(4f, 4f));

            Assert.IsFalse(a.Overlaps(b, 0.01f));
        }

        [Test]
        public void FloorRect_OverlappingRectsOverlap()
        {
            var a = new FloorRect(new Vector2(0f, 0f), new Vector2(4f, 4f));
            var b = new FloorRect(new Vector2(3f, 1f), new Vector2(4f, 4f));

            Assert.IsTrue(a.Overlaps(b, 0.01f));
        }

        [Test]
        public void FloorRect_RotatedByOneStep_SwapsSizeAndTurnsCenter()
        {
            var rect = new FloorRect(new Vector2(0f, 4f), new Vector2(2f, 6f)).Rotated(1);

            Assert.AreEqual(new Vector2(4f, 0f), rect.Center);
            Assert.AreEqual(new Vector2(6f, 2f), rect.Size);
        }

        [Test]
        public void FloorRect_Intersection_IsTheSharedArea()
        {
            var a = new FloorRect(new Vector2(0f, 0f), new Vector2(4f, 4f));
            var b = new FloorRect(new Vector2(3f, 0f), new Vector2(4f, 4f));

            Assert.IsTrue(a.TryIntersect(b, out var shared));
            Assert.AreEqual(new Vector2(1.5f, 0f), shared.Center);
            Assert.AreEqual(new Vector2(1f, 4f), shared.Size);
        }

        [Test]
        public void Direction_RotateMatchesVectorRotation()
        {
            foreach (Direction direction in System.Enum.GetValues(typeof(Direction)))
            {
                for (int steps = -4; steps <= 8; steps++)
                {
                    var expected = DirectionUtil.ToVector(DirectionUtil.Rotate(direction, steps));
                    var actual = DirectionUtil.RotateVector(DirectionUtil.ToVector(direction), steps);

                    Assert.AreEqual(expected, actual, $"{direction}, {steps} steps");
                }
            }
        }

        [Test]
        public void Direction_OppositeIsTwoStepsAway()
        {
            Assert.AreEqual(Direction.South, DirectionUtil.Opposite(Direction.North));
            Assert.AreEqual(Direction.West, DirectionUtil.Opposite(Direction.East));
        }

        [Test]
        public void CoroutineUtility_RunsNestedRoutinesToTheEnd()
        {
            int counter = 0;

            System.Collections.IEnumerator Inner()
            {
                counter++;
                yield return null;
                counter++;
            }

            System.Collections.IEnumerator Outer()
            {
                yield return Inner();
                yield return null;
                counter++;
            }

            CoroutineUtility.RunImmediately(Outer());

            Assert.AreEqual(3, counter);
        }
    }
}
