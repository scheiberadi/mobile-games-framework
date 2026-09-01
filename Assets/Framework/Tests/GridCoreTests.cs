using System.Linq;
using NUnit.Framework;
using MobileGamesFramework.Grid;

namespace MobileGamesFramework.Tests
{
    public class GridCoreTests
    {
        [Test]
        public void NewGrid_AllCellsEmpty()
        {
            var grid = new GridCore<int>(4, 4);

            Assert.AreEqual(16, grid.GetEmptyPositions().Count());
        }

        [Test]
        public void SetThenGet_ReturnsValue()
        {
            var grid = new GridCore<int>(4, 4);
            var pos = new GridPosition(1, 2);

            grid.Set(pos, 2);

            Assert.AreEqual(2, grid.Get(pos));
        }

        [Test]
        public void SetCell_RemovesItFromEmptyPositions()
        {
            var grid = new GridCore<int>(4, 4);
            var pos = new GridPosition(0, 0);

            grid.Set(pos, 2);

            Assert.AreEqual(15, grid.GetEmptyPositions().Count());
        }
    }
}
