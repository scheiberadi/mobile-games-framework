using System.IO;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    // CountLayout.Positions: quantities 1-5 are a fixed hand-tuned table, 6-20 a generated grid with a smaller
    // per-quantity hit size. These tests confirm the invariants: the right count of positions, no two hit areas
    // overlapping, and every hit area inside the object field.
    public class CountLayoutTests
    {
        // Region the objects may occupy (hit rects for the grids): clear of the home button, the coins, Eva and
        // the answer tiles. Quantities 1-5 keep their original hand-placed field, checked by centre.
        private const float FieldMinX = -410f, FieldMaxX = 275f, FieldMinY = -105f, FieldMaxY = 430f;
        private const float SmallFieldMinX = -330f, SmallFieldMaxX = 190f, SmallFieldMinY = 40f, SmallFieldMaxY = 280f;
        private const float MinSeparation = 220f;

        [Test]
        public void PositionCountMatchesQuantity()
        {
            for (var quantity = 1; quantity <= 20; quantity++)
                Assert.That(CountLayout.Positions(quantity).Length, Is.EqualTo(quantity), "quantity " + quantity);
        }

        [Test]
        public void SmallQuantitiesUse200HitAreasThatAreNeverCloserThan220Units()
        {
            for (var quantity = 1; quantity <= 5; quantity++)
            {
                Assert.That(CountLayout.HitSize(quantity), Is.EqualTo(200f), "quantity " + quantity);
                var positions = CountLayout.Positions(quantity);
                for (var i = 0; i < positions.Length; i++)
                for (var j = i + 1; j < positions.Length; j++)
                    Assert.That(Vector2.Distance(positions[i], positions[j]), Is.GreaterThanOrEqualTo(MinSeparation),
                        "quantity " + quantity + " positions " + i + " and " + j);
            }
        }

        [Test]
        public void GridHitAreasNeverOverlapAndShrinkAsQuantityGrows()
        {
            var previous = float.MaxValue;
            for (var quantity = 6; quantity <= 20; quantity++)
            {
                var hit = CountLayout.HitSize(quantity);
                Assert.That(hit, Is.LessThanOrEqualTo(previous), "quantity " + quantity);
                previous = hit;
                var positions = CountLayout.Positions(quantity);
                for (var i = 0; i < positions.Length; i++)
                for (var j = i + 1; j < positions.Length; j++)
                {
                    var d = positions[i] - positions[j];
                    Assert.That(Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y)), Is.GreaterThanOrEqualTo(hit - 0.01f),
                        "quantity " + quantity + " positions " + i + " and " + j);
                }
            }
        }

        [Test]
        public void GridsReadLeftToRightThenTopToBottom()
        {
            for (var quantity = 6; quantity <= 20; quantity++)
            {
                var positions = CountLayout.Positions(quantity);
                for (var i = 1; i < positions.Length; i++)
                {
                    var newRow = positions[i].y < positions[i - 1].y - 0.01f;
                    var sameRowToTheRight = Mathf.Abs(positions[i].y - positions[i - 1].y) < 0.01f && positions[i].x > positions[i - 1].x;
                    Assert.IsTrue(newRow || sameRowToTheRight, "quantity " + quantity + " index " + i);
                }
            }
        }

        [Test]
        public void EveryPositionLiesInsideTheObjectField()
        {
            for (var quantity = 1; quantity <= 20; quantity++)
            {
                var half = CountLayout.HitSize(quantity) / 2f;
                foreach (var position in CountLayout.Positions(quantity))
                    if (quantity <= 5)
                    {
                        Assert.That(position.x, Is.InRange(SmallFieldMinX, SmallFieldMaxX), "quantity " + quantity + " x");
                        Assert.That(position.y, Is.InRange(SmallFieldMinY, SmallFieldMaxY), "quantity " + quantity + " y");
                    }
                    else
                    {
                        Assert.That(position.x - half, Is.GreaterThanOrEqualTo(FieldMinX), "quantity " + quantity + " left");
                        Assert.That(position.x + half, Is.LessThanOrEqualTo(FieldMaxX), "quantity " + quantity + " right");
                        Assert.That(position.y - half, Is.GreaterThanOrEqualTo(FieldMinY), "quantity " + quantity + " bottom");
                        Assert.That(position.y + half, Is.LessThanOrEqualTo(FieldMaxY), "quantity " + quantity + " top");
                    }
            }
        }

        [Test]
        public void QuantityOutOfRangeThrows()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => CountLayout.Positions(0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => CountLayout.Positions(21));
        }

        // Regression for the level 3-4 crash: every round the generator can produce must have a layout, the
        // level table's choice count, and a recorded number clip for the quantity and every choice.
        [Test]
        public void EveryGeneratedRoundHasALayoutTheRightChoiceCountAndNumberClips()
        {
            var expectedChoices = new[] { 3, 3, 4, 4 };
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 300; seed++)
            {
                var round = CountRoundGenerator.Create(level, new System.Random(seed), null);
                Assert.DoesNotThrow(() => CountLayout.Positions(round.Quantity), "level " + level + " seed " + seed);
                Assert.That(round.Choices.Length, Is.EqualTo(expectedChoices[level - 1]), "level " + level);
                AssertNumberClip(round.Quantity);
                foreach (var choice in round.Choices) AssertNumberClip(choice);
            }
        }

        private static void AssertNumberClip(int number) =>
            Assert.IsTrue(File.Exists(Path.Combine(Application.dataPath, "Eva/Resources/Voice/en/num_" + number + ".mp3")), "missing clip num_" + number);
    }
}
