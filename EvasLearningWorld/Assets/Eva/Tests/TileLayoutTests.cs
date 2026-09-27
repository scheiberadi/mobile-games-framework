using System;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class TileLayoutTests
    {
        [TestCase(1, 480f)]
        [TestCase(2, 440f)]
        [TestCase(3, 380f)]
        [TestCase(4, 270f)]
        [TestCase(5, 250f)]
        [TestCase(8, 250f)]
        [TestCase(14, 250f)]
        [TestCase(21, 250f)]
        public void TilesHaveTheDocumentedSide(int count, float side)
        {
            foreach (var tile in TileLayout.Compute(count)) Assert.That(tile.Side, Is.EqualTo(side));
        }

        [Test]
        public void EveryCountFromOneToTwentyOneFitsWithinXAndDoesNotOverlap()
        {
            for (var count = 1; count <= 21; count++)
            {
                var tiles = TileLayout.Compute(count);
                Assert.That(tiles.Length, Is.EqualTo(count));
                for (var i = 0; i < count; i++)
                {
                    Assert.That(tiles[i].Side, Is.GreaterThanOrEqualTo(240f), "side, count " + count);
                    Assert.That(tiles[i].XMin, Is.GreaterThanOrEqualTo(TileLayout.AreaXMin), "xmin, count " + count);
                    Assert.That(tiles[i].XMax, Is.LessThanOrEqualTo(TileLayout.AreaXMax), "xmax, count " + count);
                    Assert.That(tiles[i].YMax, Is.LessThanOrEqualTo(0f), "a tile never rises above the content top, count " + count);
                    for (var j = i + 1; j < count; j++)
                    {
                        var apart = tiles[i].XMax <= tiles[j].XMin || tiles[j].XMax <= tiles[i].XMin
                            || tiles[i].YMax <= tiles[j].YMin || tiles[j].YMax <= tiles[i].YMin;
                        Assert.IsTrue(apart, "tiles " + i + " and " + j + " overlap, count " + count);
                    }
                }
            }
        }

        [Test]
        public void ASingleTileSitsAtTheTopCentredAndIsTheLargest()
        {
            var tile = TileLayout.Compute(1)[0];
            Assert.That(tile.X, Is.EqualTo(0f).Within(0.01f));
            Assert.That(tile.YMax, Is.EqualTo(0f).Within(0.01f), "top edge sits at the content's own top");
            Assert.That(tile.Side, Is.EqualTo(480f));
        }

        [Test]
        public void TilesRunLeftToRightThenTopToBottomAndTheLastRowIsCentred()
        {
            var five = TileLayout.Compute(5);
            Assert.That(five[0].X, Is.LessThan(five[1].X));
            Assert.That(five[4].Y, Is.LessThan(five[0].Y), "second row is below the first");
            Assert.That(five[4].X, Is.EqualTo(0f).Within(0.01f), "a single tile in the last row is centred");
        }

        [Test]
        public void ContentHeightGrowsByARowAtATimeAndMatchesTheLowestTile()
        {
            for (var count = 1; count <= 21; count++)
            {
                var tiles = TileLayout.Compute(count);
                var lowest = float.MaxValue;
                foreach (var tile in tiles) lowest = Math.Min(lowest, tile.YMin);
                Assert.That(TileLayout.ContentHeight(count), Is.EqualTo(-lowest).Within(0.01f), "count " + count);
            }
            Assert.That(TileLayout.ContentHeight(5), Is.GreaterThan(TileLayout.ContentHeight(4)));
        }

        [Test]
        public void ZeroOrFewerThrows() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => TileLayout.Compute(0));
    }
}
