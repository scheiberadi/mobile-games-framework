using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class ShortestPathTests
    {
        private static readonly int[] RouteCountByLevel = { 2, 2, 2, 3, 3, 3 };

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ShortestPathRoundGenerator.Create(0, new Random(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => ShortestPathRoundGenerator.Create(7, new Random(1)));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = ShortestPathRoundGenerator.Create(4, new Random(7));
            var b = ShortestPathRoundGenerator.Create(4, new Random(7));
            Assert.That(a.Lengths, Is.EqualTo(b.Lengths));
            Assert.That(a.ShortestIndex, Is.EqualTo(b.ShortestIndex));
        }

        [Test]
        public void RoundsPerSessionMatchesTheSharedFiveRoundWindow()
        {
            Assert.That(ShortestPathRoundGenerator.RoundsPerSession, Is.EqualTo(5));
        }

        [Test]
        public void RouteCountMatchesTheLevelTable()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = ShortestPathRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.Routes.Length, Is.EqualTo(RouteCountByLevel[level - 1]), "level " + level + " seed " + seed);
                Assert.That(round.Lengths.Length, Is.EqualTo(round.Routes.Length));
            }
        }

        // Every route is its own 3-point start/bulge/finish polyline, and every one shares the same start and
        // finish X (only Y/lane and the bulge amplitude vary) - what makes them directly comparable at a glance.
        [Test]
        public void EveryRouteHasItsOwnThreePointBulgeSharingTheSameStartAndFinishX()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = ShortestPathRoundGenerator.Create(level, new Random(seed));
                foreach (var route in round.Routes)
                {
                    Assert.That(route.Length, Is.EqualTo(3), "level " + level + " seed " + seed);
                    Assert.That(route[0].X, Is.EqualTo(ShortestPathRoundGenerator.StartX).Within(0.01f));
                    Assert.That(route[2].X, Is.EqualTo(ShortestPathRoundGenerator.FinishX).Within(0.01f));
                }
            }
        }

        // ShortestIndex must always name the route with the smallest Lengths entry - the tap that counts as
        // correct in ShortestPathScreen.
        [Test]
        public void ShortestIndexNamesTheRouteWithTheMinimumLength()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = ShortestPathRoundGenerator.Create(level, new Random(seed));
                var minLength = round.Lengths.Min();
                Assert.That(round.Lengths[round.ShortestIndex], Is.EqualTo(minLength).Within(0.01f), "level " + level + " seed " + seed);
                for (var i = 0; i < round.Lengths.Length; i++)
                    if (i != round.ShortestIndex) Assert.Greater(round.Lengths[i], round.Lengths[round.ShortestIndex]);
            }
        }

        // Higher levels bring the routes' lengths closer together (a narrower amplitude gap), making the
        // comparison harder - the real difficulty lever this game has, since route count alone tops out at 3.
        [Test]
        public void HigherLevelsNarrowTheGapBetweenTheShortestAndLongestRoute()
        {
            for (var seed = 0; seed < 100; seed++)
            {
                var easy = ShortestPathRoundGenerator.Create(1, new Random(seed));
                var hard = ShortestPathRoundGenerator.Create(6, new Random(seed));
                var easyGap = easy.Lengths.Max() - easy.Lengths.Min();
                var hardGap = hard.Lengths.Max() - hard.Lengths.Min();
                Assert.Less(hardGap, easyGap, "seed " + seed);
            }
        }

        // The shortest route never has a zero bulge - it is always a genuine route with its own visible curve,
        // never simply "the flat line among curved ones".
        [Test]
        public void TheShortestRouteStillHasANonZeroBulge()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = ShortestPathRoundGenerator.Create(level, new Random(seed));
                var shortest = round.Routes[round.ShortestIndex];
                var bulge = Math.Abs(shortest[1].Y - shortest[0].Y);
                Assert.Greater(bulge, 0f, "level " + level + " seed " + seed);
            }
        }
    }
}
