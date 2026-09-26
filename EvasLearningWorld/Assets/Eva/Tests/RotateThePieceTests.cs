using System;
using NUnit.Framework;
using EvasLearningWorld.Rules;

namespace EvasLearningWorld.Tests
{
    public class RotateThePieceTests
    {
        private static readonly float[] StepDegreesByLevel = { 90f, 90f, 45f, 45f, 0f, 0f };

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RotateThePieceRoundGenerator.Create(0, new Random(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => RotateThePieceRoundGenerator.Create(7, new Random(1)));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = RotateThePieceRoundGenerator.Create(4, new Random(7));
            var b = RotateThePieceRoundGenerator.Create(4, new Random(7));
            Assert.That(a.TargetAngle, Is.EqualTo(b.TargetAngle));
            Assert.That(a.StartAngle, Is.EqualTo(b.StartAngle));
            Assert.That(a.PieceSprite, Is.EqualTo(b.PieceSprite));
        }

        [Test]
        public void RoundsPerSessionMatchesTheSharedFiveRoundWindow()
        {
            Assert.That(RotateThePieceRoundGenerator.RoundsPerSession, Is.EqualTo(5));
        }

        [Test]
        public void StepDegreesMatchesTheLevelTable()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 50; seed++)
            {
                var round = RotateThePieceRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.StepDegrees, Is.EqualTo(StepDegreesByLevel[level - 1]), "level " + level + " seed " + seed);
            }
        }

        // At a stepped level, both the target and the start angle must land exactly on a multiple of the step -
        // the piece (and its rotate handle) should never rest at an angle the level doesn't allow.
        [Test]
        public void SteppedLevelsAlwaysLandOnAMultipleOfTheStep()
        {
            for (var level = 1; level <= 4; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = RotateThePieceRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.TargetAngle % round.StepDegrees, Is.EqualTo(0f).Within(0.001f), "level " + level + " seed " + seed);
                Assert.That(round.StartAngle % round.StepDegrees, Is.EqualTo(0f).Within(0.001f), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void StartAngleIsNeverTheSameAsTargetAngle()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = RotateThePieceRoundGenerator.Create(level, new Random(seed));
                Assert.Greater(RotateThePieceRoundGenerator.AngleDifference(round.StartAngle, round.TargetAngle), round.ToleranceDegrees, "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void EveryAngleStaysWithinZeroToThreeSixty()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = RotateThePieceRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.TargetAngle, Is.InRange(0f, 360f), "level " + level + " seed " + seed);
                Assert.That(round.StartAngle, Is.InRange(0f, 360f), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void IsMatchIsTrueExactlyAtTheTargetAndFalseWellOutsideTolerance()
        {
            var round = RotateThePieceRoundGenerator.Create(4, new Random(9));
            Assert.IsTrue(RotateThePieceRoundGenerator.IsMatch(round, round.TargetAngle));
            Assert.IsFalse(RotateThePieceRoundGenerator.IsMatch(round, RotateThePieceRoundGenerator.Normalize(round.TargetAngle + 90f)));
        }

        [Test]
        public void AngleDifferenceWrapsAroundThreeSixty()
        {
            Assert.That(RotateThePieceRoundGenerator.AngleDifference(10f, 350f), Is.EqualTo(20f).Within(0.001f));
            Assert.That(RotateThePieceRoundGenerator.AngleDifference(0f, 180f), Is.EqualTo(180f).Within(0.001f));
            Assert.That(RotateThePieceRoundGenerator.AngleDifference(0f, 0f), Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void NormalizeAlwaysReturnsAValueInZeroToThreeSixty()
        {
            Assert.That(RotateThePieceRoundGenerator.Normalize(-30f), Is.EqualTo(330f).Within(0.001f));
            Assert.That(RotateThePieceRoundGenerator.Normalize(370f), Is.EqualTo(10f).Within(0.001f));
            Assert.That(RotateThePieceRoundGenerator.Normalize(0f), Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void PieceSpriteIsAlwaysWithinTheCatalogueRange()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = RotateThePieceRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.PieceSprite, Does.StartWith("rotatepiece/piece_"));
                var index = int.Parse(round.PieceSprite.Substring("rotatepiece/piece_".Length));
                Assert.That(index, Is.InRange(0, RotateThePieceRoundGenerator.PieceCount - 1), "level " + level + " seed " + seed);
            }
        }
    }
}
