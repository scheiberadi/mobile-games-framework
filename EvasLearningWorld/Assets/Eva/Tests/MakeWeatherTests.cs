using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class MakeWeatherTests
    {
        [Test]
        public void EveryPieceMakesItsOwnWeatherAndBack()
        {
            foreach (SkyPiece piece in Enum.GetValues(typeof(SkyPiece)))
                Assert.AreEqual(piece, MakeWeather.PieceFor(MakeWeather.Made(piece)));
            Assert.AreEqual(6, Enum.GetValues(typeof(WeatherKind)).Length);
        }

        [Test]
        public void ASessionAsksForThreeDifferentWeathers()
        {
            for (var seed = 0; seed < 50; seed++)
            {
                var session = MakeWeather.CreateSession(new Random(seed));
                Assert.AreEqual(MakeWeather.RoundsPerSession, session.Length);
                Assert.AreEqual(session.Length, session.Distinct().Count());
            }
        }

        [Test]
        public void TheRightPieceMakesTheAskedWeatherAndEndsTheRound()
        {
            var round = new MakeWeatherRound(WeatherKind.Rainy);
            Assert.IsNull(round.Made);
            Assert.AreEqual(WeatherDrop.Right, round.Drop(SkyPiece.RainCloud));
            Assert.AreEqual(WeatherKind.Rainy, round.Made);
            Assert.IsTrue(round.Done);
        }

        [Test]
        public void AWrongPieceMakesOtherWeatherAndCountsAsATry()
        {
            var round = new MakeWeatherRound(WeatherKind.Rainy);
            Assert.AreEqual(WeatherDrop.Wrong, round.Drop(SkyPiece.Sun));
            Assert.AreEqual(WeatherKind.Sunny, round.Made);
            Assert.IsFalse(round.Done);
            Assert.AreEqual(1, round.TotalWrongs);
        }

        [Test]
        public void TwoWrongPiecesCallForAHintAndTheHintStartsTheCountAgain()
        {
            var round = new MakeWeatherRound(WeatherKind.Snowy);
            round.Drop(SkyPiece.Sun);
            Assert.IsFalse(round.NeedsHint);
            round.Drop(SkyPiece.Wind);
            Assert.IsTrue(round.NeedsHint);
            round.HintGiven();
            Assert.IsFalse(round.NeedsHint);
            Assert.AreEqual(2, round.TotalWrongs);
        }
    }
}
