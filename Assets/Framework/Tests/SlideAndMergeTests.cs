using System.Linq;
using NUnit.Framework;
using MobileGamesFramework.Grid;

namespace MobileGamesFramework.Tests
{
    public class SlideAndMergeTests
    {
        private class EqualValueMergeRule : IMergeRule<int>
        {
            public bool CanMerge(int a, int b) => a == b;
            public int Merge(int a, int b) => a + b;
        }

        private static GridCore<int> GridFromRows(int[][] rows)
        {
            var grid = new GridCore<int>(rows[0].Length, rows.Length);
            for (var row = 0; row < rows.Length; row++)
            for (var col = 0; col < rows[row].Length; col++)
                if (rows[row][col] != 0)
                    grid.Set(new GridPosition(row, col), rows[row][col]);
            return grid;
        }

        private static int[][] ToRows(GridCore<int> grid)
        {
            var rows = new int[grid.Height][];
            for (var row = 0; row < grid.Height; row++)
            {
                rows[row] = new int[grid.Width];
                for (var col = 0; col < grid.Width; col++)
                    rows[row][col] = grid.Get(new GridPosition(row, col)) ?? 0;
            }
            return rows;
        }

        [Test]
        public void SlideLeft_CompactsTilesToLeftEdge()
        {
            var grid = GridFromRows(new[] { new[] { 0, 2, 0, 4 } });

            grid.SlideAndMerge(Direction.Left, new EqualValueMergeRule());

            CollectionAssert.AreEqual(new[] { 2, 4, 0, 0 }, ToRows(grid)[0]);
        }

        [Test]
        public void SlideLeft_MergesEqualAdjacentTiles()
        {
            var grid = GridFromRows(new[] { new[] { 2, 2, 0, 0 } });

            var result = grid.SlideAndMerge(Direction.Left, new EqualValueMergeRule());

            CollectionAssert.AreEqual(new[] { 4, 0, 0, 0 }, ToRows(grid)[0]);
            CollectionAssert.AreEqual(new[] { 4 }, result.MergedResults);
        }

        [Test]
        public void SlideLeft_MergesOnlyOncePerPair()
        {
            var grid = GridFromRows(new[] { new[] { 2, 2, 2, 2 } });

            grid.SlideAndMerge(Direction.Left, new EqualValueMergeRule());

            CollectionAssert.AreEqual(new[] { 4, 4, 0, 0 }, ToRows(grid)[0]);
        }

        [Test]
        public void SlideRight_CompactsTilesToRightEdge()
        {
            var grid = GridFromRows(new[] { new[] { 4, 0, 2, 0 } });

            grid.SlideAndMerge(Direction.Right, new EqualValueMergeRule());

            CollectionAssert.AreEqual(new[] { 0, 0, 4, 2 }, ToRows(grid)[0]);
        }

        [Test]
        public void SlideUp_CompactsTilesToTopEdge()
        {
            var grid = GridFromRows(new[]
            {
                new[] { 0 },
                new[] { 2 },
                new[] { 0 },
                new[] { 2 },
            });

            grid.SlideAndMerge(Direction.Up, new EqualValueMergeRule());

            var rows = ToRows(grid);
            Assert.AreEqual(4, rows[0][0]);
            Assert.AreEqual(0, rows[1][0]);
            Assert.AreEqual(0, rows[2][0]);
            Assert.AreEqual(0, rows[3][0]);
        }

        [Test]
        public void SlideDown_CompactsTilesToBottomEdge()
        {
            var grid = GridFromRows(new[]
            {
                new[] { 2 },
                new[] { 0 },
                new[] { 2 },
                new[] { 0 },
            });

            grid.SlideAndMerge(Direction.Down, new EqualValueMergeRule());

            var rows = ToRows(grid);
            Assert.AreEqual(0, rows[0][0]);
            Assert.AreEqual(0, rows[1][0]);
            Assert.AreEqual(0, rows[2][0]);
            Assert.AreEqual(4, rows[3][0]);
        }

        [Test]
        public void Slide_NoTilesMove_ReportsNotMoved()
        {
            var grid = GridFromRows(new[] { new[] { 2, 0, 0, 0 } });

            var result = grid.SlideAndMerge(Direction.Left, new EqualValueMergeRule());

            Assert.IsFalse(result.Moved);
        }

        [Test]
        public void Slide_TilesCompactButDontMerge_ReportsMoved()
        {
            var grid = GridFromRows(new[] { new[] { 0, 2, 0, 4 } });

            var result = grid.SlideAndMerge(Direction.Left, new EqualValueMergeRule());

            Assert.IsTrue(result.Moved);
            Assert.IsEmpty(result.MergedResults);
        }

        [Test]
        public void Clone_ProducesIndependentGrid()
        {
            var grid = GridFromRows(new[] { new[] { 2, 0, 0, 0 } });

            var clone = grid.Clone();
            clone.Set(new GridPosition(0, 1), 4);

            Assert.IsNull(grid.Get(new GridPosition(0, 1)));
            Assert.AreEqual(4, clone.Get(new GridPosition(0, 1)));
        }
    }
}
