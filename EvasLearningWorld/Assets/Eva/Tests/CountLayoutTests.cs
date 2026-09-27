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
        public void HitSizeShrinksWithTotalAndStaysAtLeast100()
        {
            var previous = float.MaxValue;
            for (var total = 1; total <= 20; total++)
            {
                var hit = CountLayout.HitSize(total);
                Assert.That(hit, Is.GreaterThanOrEqualTo(100f));
                Assert.That(hit, Is.LessThanOrEqualTo(previous), "total " + total);
                previous = hit;
            }
            Assert.That(CountLayout.HitSize(1), Is.EqualTo(200f));
            Assert.That(CountLayout.HitSize(20), Is.EqualTo(100f));
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
        public void ScatterNeverFallsBackToTheGridForAnyTotalOrSeed()
        {
            for (var total = 1; total <= 20; total++)
            for (var seed = 0; seed < Seeds; seed++)
            {
                CountLayout.Scatter(total, seed, out var usedFallback);
                Assert.IsFalse(usedFallback, "total " + total + " seed " + seed);
            }
        }

        [Test]
        public void GridFallbackIsValidAndStaggeredNotAxisAligned()
        {
            for (var total = 1; total <= 20; total++)
            for (var seed = 0; seed < 50; seed++)
            {
                var hit = CountLayout.HitSize(total);
                var half = hit / 2f;
                var positions = CountLayout.GridFallback(total, seed);
                Assert.That(positions.Length, Is.EqualTo(total));
                for (var i = 0; i < positions.Length; i++)
                {
                    Assert.That(positions[i].x - half, Is.GreaterThanOrEqualTo(CountLayout.FieldMinX - 0.01f));
                    Assert.That(positions[i].x + half, Is.LessThanOrEqualTo(CountLayout.FieldMaxX + 0.01f));
                    Assert.That(positions[i].y - half, Is.GreaterThanOrEqualTo(CountLayout.FieldMinY - 0.01f));
                    Assert.That(positions[i].y + half, Is.LessThanOrEqualTo(CountLayout.FieldMaxY + 0.01f));
                    for (var j = i + 1; j < positions.Length; j++)
                    {
                        var d = positions[i] - positions[j];
                        Assert.That(Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y)), Is.GreaterThanOrEqualTo(hit - 0.01f),
                            "grid total " + total + " seed " + seed);
                    }
                }
            }
        }

        // Not a few rows/columns: on average the rounded (10 unit) x and y values are mostly distinct, and only
        // a minority of items have a neighbour lined up on the same row/column at neighbour spacing. The
        // densest totals (19-20 items at the 100 floor) are ~55% of the board, so lined-up neighbours are more
        // likely by chance there and get a looser bound.
        [Test]
        public void ScatterIsNotRowsAndColumns()
        {
            for (var total = 6; total <= 20; total++)
            {
                var hit = CountLayout.HitSize(total);
                double distinctX = 0, distinctY = 0, alignedShare = 0;
                for (var seed = 0; seed < Seeds; seed++)
                {
                    var positions = CountLayout.Scatter(total, seed);
                    var xs = new System.Collections.Generic.HashSet<int>();
                    var ys = new System.Collections.Generic.HashSet<int>();
                    var aligned = 0;
                    for (var i = 0; i < positions.Length; i++)
                    {
                        xs.Add(Mathf.RoundToInt(positions[i].x / 10f)); ys.Add(Mathf.RoundToInt(positions[i].y / 10f));
                        for (var j = 0; j < positions.Length; j++)
                        {
                            if (j == i) continue;
                            var d = positions[i] - positions[j];
                            if ((Mathf.Abs(d.y) <= 8f && Mathf.Abs(d.x) < 1.3f * hit) || (Mathf.Abs(d.x) <= 8f && Mathf.Abs(d.y) < 1.3f * hit)) { aligned++; break; }
                        }
                    }
                    distinctX += xs.Count; distinctY += ys.Count; alignedShare += aligned / (double)total;
                }
                var minDistinct = System.Math.Max(3, total / 2);
                Assert.That(distinctX / Seeds, Is.GreaterThanOrEqualTo(minDistinct), "distinct x, total " + total);
                Assert.That(distinctY / Seeds, Is.GreaterThanOrEqualTo(minDistinct), "distinct y, total " + total);
                Assert.That(alignedShare / Seeds, Is.LessThanOrEqualTo(total >= 19 ? 0.65 : 0.4), "aligned share, total " + total);
            }
        }

        // Even spread: no clumps with empty regions. The field is split into a 2x2 grid and, for 8+ items,
        // every region must hold at least one item.
        [Test]
        public void ScatterCoversEveryRegionOfTheBoard()
        {
            const int cols = 2, rows = 2;
            var w = (CountLayout.FieldMaxX - CountLayout.FieldMinX) / cols;
            var h = (CountLayout.FieldMaxY - CountLayout.FieldMinY) / rows;
            for (var total = 8; total <= 20; total++)
            for (var seed = 0; seed < Seeds; seed++)
            {
                var counts = new int[cols * rows];
                foreach (var p in CountLayout.Scatter(total, seed))
                {
                    var c = Mathf.Clamp((int)((p.x - CountLayout.FieldMinX) / w), 0, cols - 1);
                    var r = Mathf.Clamp((int)((p.y - CountLayout.FieldMinY) / h), 0, rows - 1);
                    counts[r * cols + c]++;
                }
                for (var i = 0; i < counts.Length; i++)
                    Assert.That(counts[i], Is.GreaterThan(0), "empty region " + i + ", total " + total + " seed " + seed);
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
            var expectedChoices = new[] { 3, 3, 5, 5, 6, 6 };
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
