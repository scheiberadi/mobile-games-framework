using EvasLearningWorld.App;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    // CountLayout.Positions is a fixed hand-tuned table (see the M1 plan's Task 7), not a generator, so these
    // tests just confirm the table's own invariants: the right count of positions, no two closer than 220 units
    // (the object hit area is 240 wide, so anything closer would make adjacent objects impossible to tap apart),
    // and every position inside the object field (x -620..280, y -40..340).
    public class CountLayoutTests
    {
        private const float FieldMinX = -620f, FieldMaxX = 280f, FieldMinY = -40f, FieldMaxY = 340f;
        private const float MinSeparation = 220f;

        [Test]
        public void PositionCountMatchesQuantity()
        {
            for (var quantity = 1; quantity <= 5; quantity++)
                Assert.That(CountLayout.Positions(quantity).Length, Is.EqualTo(quantity), "quantity " + quantity);
        }

        [Test]
        public void NoTwoPositionsAreCloserThan220Units()
        {
            for (var quantity = 1; quantity <= 5; quantity++)
            {
                var positions = CountLayout.Positions(quantity);
                for (var i = 0; i < positions.Length; i++)
                for (var j = i + 1; j < positions.Length; j++)
                    Assert.That(Vector2.Distance(positions[i], positions[j]), Is.GreaterThanOrEqualTo(MinSeparation),
                        "quantity " + quantity + " positions " + i + " and " + j);
            }
        }

        [Test]
        public void EveryPositionLiesInsideTheObjectField()
        {
            for (var quantity = 1; quantity <= 5; quantity++)
                foreach (var position in CountLayout.Positions(quantity))
                {
                    Assert.That(position.x, Is.InRange(FieldMinX, FieldMaxX), "quantity " + quantity + " x");
                    Assert.That(position.y, Is.InRange(FieldMinY, FieldMaxY), "quantity " + quantity + " y");
                }
        }

        [Test]
        public void QuantityOutOfRangeThrows()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => CountLayout.Positions(0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => CountLayout.Positions(6));
        }
    }
}
