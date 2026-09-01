using NUnit.Framework;
using Game01_2048;

namespace Game01_2048.Tests
{
    public class Game2048MergeRuleTests
    {
        [Test]
        public void EqualValues_CanMerge()
        {
            var rule = new Game2048MergeRule();

            Assert.IsTrue(rule.CanMerge(4, 4));
        }

        [Test]
        public void DifferentValues_CannotMerge()
        {
            var rule = new Game2048MergeRule();

            Assert.IsFalse(rule.CanMerge(4, 2));
        }

        [Test]
        public void Merge_DoublesTheValue()
        {
            var rule = new Game2048MergeRule();

            Assert.AreEqual(16, rule.Merge(8, 8));
        }
    }
}
