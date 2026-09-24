using System.IO;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    // CountLayout.Scatter: seeded scattered placement for any total 1-20 with the invariants below.
    public class CountLayoutTests
    {
        private const int Seeds = 200;

        [Test]
        public void ScatterReturnsOneSlotPerItemForEveryTotalAndSeedWithoutThrowing()
        {
            for (var total = 1; total <= 20; total++)
            for (var seed = 0; seed < Seeds; seed++)
            {
                Vector2[] positions = null;
                Assert.DoesNotThrow(() => positions = CountLayout.Scatter(total, seed), "total " + total + " seed " + seed);
                Assert.That(positions.Length, Is.EqualTo(total));
            }
        }

        [Test]
        public void HitSizeShrinksWithTotalAndStaysAtLeast130()
        {
            var previous = float.MaxValue;
            for (var total = 1; total <= 20; total++)
            {
                var hit = CountLayout.HitSize(total);
                Assert.That(hit, Is.GreaterThanOrEqualTo(130f));
                Assert.That(hit, Is.LessThanOrEqualTo(previous), "total " + total);
                previous = hit;
            }
            Assert.That(CountLayout.HitSize(5), Is.EqualTo(200f));
        }

        [Test]
        public void ScatteredHitAreasNeverOverlap()
        {
            for (var total = 1; total <= 20; total++)
            for (var seed = 0; seed < Seeds; seed++)
            {
                var hit = CountLayout.HitSize(total);
                var positions = CountLayout.Scatter(total, seed);
                for (var i = 0; i < positions.Length; i++)
                for (var j = i + 1; j < positions.Length; j++)
                {
                    var d = positions[i] - positions[j];
                    Assert.That(Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y)), Is.GreaterThanOrEqualTo(hit - 0.01f),
                        "total " + total + " seed " + seed + " slots " + i + " and " + j);
                }
            }
        }

        [Test]
        public void EveryHitAreaLiesInsideTheBoard()
        {
            for (var total = 1; total <= 20; total++)
            for (var seed = 0; seed < Seeds; seed++)
            {
                var half = CountLayout.HitSize(total) / 2f;
                foreach (var p in CountLayout.Scatter(total, seed))
                {
                    Assert.That(p.x - half, Is.GreaterThanOrEqualTo(CountLayout.FieldMinX - 0.01f), "total " + total + " left");
                    Assert.That(p.x + half, Is.LessThanOrEqualTo(CountLayout.FieldMaxX + 0.01f), "total " + total + " right");
                    Assert.That(p.y - half, Is.GreaterThanOrEqualTo(CountLayout.FieldMinY - 0.01f), "total " + total + " bottom");
                    Assert.That(p.y + half, Is.LessThanOrEqualTo(CountLayout.FieldMaxY + 0.01f), "total " + total + " top");
                }
            }
        }

        [Test]
        public void ScatterIsDeterministicPerSeedAndVariesAcrossSeeds()
        {
            for (var total = 1; total <= 20; total++)
                Assert.That(CountLayout.Scatter(total, 42), Is.EqualTo(CountLayout.Scatter(total, 42)), "total " + total);
            Assert.That(CountLayout.Scatter(6, 1), Is.Not.EqualTo(CountLayout.Scatter(6, 2)));
        }

        [Test]
        public void SlotsAreInReadingOrder()
        {
            for (var total = 1; total <= 20; total++)
            for (var seed = 0; seed < Seeds; seed++)
            {
                var positions = CountLayout.Scatter(total, seed);
                for (var i = 1; i < positions.Length; i++)
                    Assert.That(positions[i].x, Is.GreaterThanOrEqualTo(positions[i - 1].x), "total " + total + " seed " + seed);
            }
        }

        [Test]
        public void ScatterIsNotAStraightLine()
        {
            for (var total = 3; total <= 20; total++)
            for (var seed = 0; seed < Seeds; seed++)
            {
                var positions = CountLayout.Scatter(total, seed);
                var allSameY = true; var allSameX = true;
                foreach (var p in positions)
                {
                    if (Mathf.Abs(p.y - positions[0].y) > 1f) allSameY = false;
                    if (Mathf.Abs(p.x - positions[0].x) > 1f) allSameX = false;
                }
                Assert.IsFalse(allSameY, "row: total " + total + " seed " + seed);
                Assert.IsFalse(allSameX, "column: total " + total + " seed " + seed);
            }
        }

        [Test]
        public void ScatterOutOfRangeThrows()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => CountLayout.Scatter(0, 1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => CountLayout.Scatter(21, 1));
        }

        // Regression for the level 3-4 crash: every round the generator can produce must have a layout, the
        // level table's choice count, and a recorded number clip for the quantity and every choice.
        [Test]
        public void EveryGeneratedRoundHasALayoutTheRightChoiceCountAndNumberClips()
        {
            var expectedChoices = new[] { 3, 3, 4, 4, 4, 4 };
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 300; seed++)
            {
                var round = CountRoundGenerator.Create(level, new System.Random(seed), null);
                Assert.DoesNotThrow(() => CountLayout.Scatter(round.TotalItems, seed), "level " + level + " seed " + seed);
                Assert.That(round.Choices.Length, Is.EqualTo(expectedChoices[level - 1]), "level " + level);
                AssertNumberClip(round.Quantity);
                Assert.That(round.TotalItems, Is.LessThanOrEqualTo(20));
                foreach (var choice in round.Choices) AssertNumberClip(choice);
            }
        }

        private static void AssertNumberClip(int number) =>
            Assert.IsTrue(File.Exists(Path.Combine(Application.dataPath, "Eva/Resources/Voice/en/num_" + number + ".mp3")), "missing clip num_" + number);
    }
}
