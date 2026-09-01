using NUnit.Framework;
using MobileGamesFramework.Grid;

namespace MobileGamesFramework.Tests
{
    public class SwipeInputInterpreterTests
    {
        [Test]
        public void LargeRightwardDelta_ReturnsRight()
        {
            Assert.AreEqual(Direction.Right, SwipeInputInterpreter.FromDelta(100, 0));
        }

        [Test]
        public void LargeLeftwardDelta_ReturnsLeft()
        {
            Assert.AreEqual(Direction.Left, SwipeInputInterpreter.FromDelta(-100, 0));
        }

        [Test]
        public void LargeDownwardDelta_ReturnsDown()
        {
            Assert.AreEqual(Direction.Down, SwipeInputInterpreter.FromDelta(0, 100));
        }

        [Test]
        public void LargeUpwardDelta_ReturnsUp()
        {
            Assert.AreEqual(Direction.Up, SwipeInputInterpreter.FromDelta(0, -100));
        }

        [Test]
        public void DeltaBelowThreshold_ReturnsNull()
        {
            Assert.IsNull(SwipeInputInterpreter.FromDelta(5, -5));
        }

        [Test]
        public void DominantAxisWins_WhenBothExceedThreshold()
        {
            Assert.AreEqual(Direction.Right, SwipeInputInterpreter.FromDelta(100, 30));
        }
    }
}
