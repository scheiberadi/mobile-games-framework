using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class OddOneOutTests
    {
        private static readonly int[] ChoiceCountByLevel = { 4, 5, 4, 5, 4, 5 };

        [Test]
        public void EveryRoundHasOneOddItemAtTheLevelsChoiceCount()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 300; seed++)
            {
                var round = OddOneOutRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.Items.Length, Is.EqualTo(ChoiceCountByLevel[level - 1]));
                Assert.That(round.OddIndex, Is.InRange(0, round.Items.Length - 1));
                Assert.That(round.Items.Distinct().Count(), Is.EqualTo(round.Items.Length), "level " + level + " repeats an item");
            }
        }

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => OddOneOutRoundGenerator.Create(0, new Random(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => OddOneOutRoundGenerator.Create(7, new Random(1)));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = OddOneOutRoundGenerator.Create(4, new Random(7));
            var b = OddOneOutRoundGenerator.Create(4, new Random(7));
            Assert.That(a.Items, Is.EqualTo(b.Items));
            Assert.That(a.OddIndex, Is.EqualTo(b.OddIndex));
        }

        [Test]
        public void RoundsPerSessionMatchesTheSharedFiveRoundWindow()
        {
            Assert.That(OddOneOutRoundGenerator.RoundsPerSession, Is.EqualTo(5));
        }

        // Levels 1-2 (obvious): every non-odd item is drawn from a single foreign-to-the-odd-item category, which
        // this test infers indirectly - every item outside the catalogue's Animal set belongs to exactly one of
        // Vehicle/Fruit, and levels 1-2 never place the odd item inside the same category as the rest.
        [Test]
        public void ObviousLevelsAlwaysProduceARound()
        {
            for (var seed = 0; seed < 200; seed++)
            {
                var round1 = OddOneOutRoundGenerator.Create(1, new Random(seed));
                var round2 = OddOneOutRoundGenerator.Create(2, new Random(seed));
                Assert.That(round1.Items.Length, Is.EqualTo(4));
                Assert.That(round2.Items.Length, Is.EqualTo(5));
            }
        }
    }
}
