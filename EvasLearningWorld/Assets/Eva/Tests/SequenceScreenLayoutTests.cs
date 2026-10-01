using EvasLearningWorld.App;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    public class SequenceScreenLayoutTests
    {
        // The Art Studio and Brain Gym sequences ask for 1 to 6 tiles; every count must have a layout, with no two
        // tiles (220 units wide) touching.
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void EveryTileCountTheGeneratorsAskForHasANonOverlappingLayout(int count)
        {
            var positions = SequenceScreen.PositionsFor(count);
            Assert.That(positions.Length, Is.EqualTo(count));
            for (var i = 0; i < count; i++)
            for (var j = i + 1; j < count; j++)
            {
                var gap = Mathf.Max(Mathf.Abs(positions[i].x - positions[j].x), Mathf.Abs(positions[i].y - positions[j].y));
                Assert.That(gap, Is.GreaterThanOrEqualTo(220f), "tiles " + i + " and " + j + " at count " + count);
            }
        }
    }
}
