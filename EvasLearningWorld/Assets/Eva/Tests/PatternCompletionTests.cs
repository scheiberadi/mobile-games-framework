using System;
using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class PatternCompletionTests
    {
        private static readonly int[] PeriodByLevel = { 2, 2, 3, 3, 4, 4 };
        private static readonly int[] ChoiceCountByLevel = { 3, 3, 4, 4, 4, 4 };

        [Test]
        public void EveryRoundRepeatsItsPeriodAndTheAnswerContinuesIt()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 300; seed++)
            {
                var period = PeriodByLevel[level - 1];
                var round = PatternCompletionRoundGenerator.Create(level, new Random(seed));

                Assert.That(round.Sequence.Length, Is.LessThanOrEqualTo(PatternCompletionRoundGenerator.MaxShownLength));
                for (var i = period; i < round.Sequence.Length; i++)
                    Assert.That(round.Sequence[i], Is.EqualTo(round.Sequence[i - period]), "level " + level + " breaks its own period at index " + i);
                Assert.That(round.Answer, Is.EqualTo(round.Sequence[round.Sequence.Length - period]), "the answer continues the same period");
            }
        }

        [Test]
        public void ChoicesAreUniqueContainTheAnswerAndMatchTheLevelsCount()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 300; seed++)
            {
                var round = PatternCompletionRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.Choices.Length, Is.EqualTo(ChoiceCountByLevel[level - 1]));
                Assert.That(round.Choices, Is.Unique);
                Assert.That(round.Choices, Does.Contain(round.Answer));
                foreach (var choice in round.Choices) Assert.That(PatternCompletionRoundGenerator.Symbols, Does.Contain(choice));
            }
        }

        [Test]
        public void TheUnitWithinOnePeriodUsesDistinctSymbols()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var period = PeriodByLevel[level - 1];
                var round = PatternCompletionRoundGenerator.Create(level, new Random(seed));
                var unit = round.Sequence.Take(period).ToList();
                Assert.That(unit.Distinct().Count(), Is.EqualTo(period), "level " + level + " unit repeats a symbol");
            }
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = PatternCompletionRoundGenerator.Create(3, new Random(7));
            var b = PatternCompletionRoundGenerator.Create(3, new Random(7));
            Assert.That(a.Sequence, Is.EqualTo(b.Sequence));
            Assert.That(a.Answer, Is.EqualTo(b.Answer));
            Assert.That(a.Choices, Is.EqualTo(b.Choices));
        }

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PatternCompletionRoundGenerator.Create(0, new Random(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => PatternCompletionRoundGenerator.Create(7, new Random(1)));
        }

        [Test]
        public void PeriodGrowsFromLevelOneToLevelSix()
        {
            for (var level = 2; level <= 6; level++)
                Assert.That(PeriodByLevel[level - 1], Is.GreaterThanOrEqualTo(PeriodByLevel[level - 2]));
        }

        [Test]
        public void RoundsPerSessionMatchesTheSharedFiveRoundWindow()
        {
            Assert.That(PatternCompletionRoundGenerator.RoundsPerSession, Is.EqualTo(5));
        }
    }
}
