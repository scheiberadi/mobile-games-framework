using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class WhatsMissingTests
    {
        private static readonly int[] SetSizeByLevel = { 3, 3, 4, 4, 5, 5 };
        private static readonly int[] ChoiceCountByLevel = { 3, 3, 4, 4, 4, 4 };

        [Test]
        public void EveryRoundShowsASetOfTheLevelsSizeWithNoRepeats()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 300; seed++)
            {
                var round = WhatsMissingRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.Shown.Length, Is.EqualTo(SetSizeByLevel[level - 1]));
                Assert.That(round.Shown.Distinct().Count(), Is.EqualTo(round.Shown.Length));
                Assert.That(round.MissingIndex, Is.InRange(0, round.Shown.Length - 1));
            }
        }

        [Test]
        public void ChoicesAreUniqueContainTheMissingItemAndMatchTheLevelsCount()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 300; seed++)
            {
                var round = WhatsMissingRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.Choices.Length, Is.EqualTo(ChoiceCountByLevel[level - 1]));
                Assert.That(round.Choices, Is.Unique);
                Assert.That(round.Choices, Does.Contain(round.Missing));
                Assert.That(round.Missing, Is.EqualTo(round.Shown[round.MissingIndex]));
            }
        }

        [Test]
        public void ExposureSecondsShrinksAsLevelRises()
        {
            var previous = WhatsMissingRoundGenerator.ExposureSecondsFor(DifficultyLadder.MinLevel);
            for (var level = DifficultyLadder.MinLevel + 1; level <= DifficultyLadder.MaxLevel; level++)
            {
                var current = WhatsMissingRoundGenerator.ExposureSecondsFor(level);
                Assert.That(current, Is.LessThanOrEqualTo(previous), "level " + level + " should not expose longer than the level before it");
                previous = current;
            }
        }

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => WhatsMissingRoundGenerator.Create(0, new Random(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => WhatsMissingRoundGenerator.Create(7, new Random(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => WhatsMissingRoundGenerator.ExposureSecondsFor(0));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = WhatsMissingRoundGenerator.Create(4, new Random(7));
            var b = WhatsMissingRoundGenerator.Create(4, new Random(7));
            Assert.That(a.Shown, Is.EqualTo(b.Shown));
            Assert.That(a.MissingIndex, Is.EqualTo(b.MissingIndex));
            Assert.That(a.Choices, Is.EqualTo(b.Choices));
        }

        [Test]
        public void RoundsPerSessionMatchesTheSharedFiveRoundWindow()
        {
            Assert.That(WhatsMissingRoundGenerator.RoundsPerSession, Is.EqualTo(5));
        }
    }
}
