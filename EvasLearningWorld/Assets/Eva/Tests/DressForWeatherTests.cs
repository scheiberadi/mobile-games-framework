using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class DressForWeatherTests
    {
        [Test]
        public void EachOutfitBelongsToOneWeatherAndBack()
        {
            foreach (Outfit outfit in Enum.GetValues(typeof(Outfit)))
                Assert.That(DressForWeather.OutfitFor(DressForWeather.WeatherFor(outfit)), Is.EqualTo(outfit));
            Assert.Throws<ArgumentException>(() => DressForWeather.OutfitFor(WeatherKind.Stormy));
        }

        [Test]
        public void ASessionIsThreeDifferentWeathersThatHaveAnOutfit()
        {
            for (var seed = 0; seed < 30; seed++)
            {
                var session = DressForWeather.CreateSession(new Random(seed));
                Assert.That(session.Length, Is.EqualTo(DressForWeather.RoundsPerSession));
                Assert.That(session.Distinct().Count(), Is.EqualTo(session.Length));
                foreach (var weather in session) Assert.That(DressForWeather.Weathers, Does.Contain(weather));
            }
        }

        [Test]
        public void TheRightOutfitStaysOnAndFinishesTheRound()
        {
            var round = new DressRound(WeatherKind.Rainy);
            Assert.That(round.Drop(Outfit.Rain), Is.EqualTo(DressDrop.Right));
            Assert.That(round.Done);
            Assert.That(round.Worn, Is.EqualTo(Outfit.Rain));
            round.TakeOff();
            Assert.That(round.Worn, Is.EqualTo(Outfit.Rain), "the right outfit is not taken off");
        }

        [Test]
        public void AWrongOutfitIsTriedOnThenTakenOffAndTwoInARowCallForAHint()
        {
            var round = new DressRound(WeatherKind.Snowy);
            Assert.That(round.Drop(Outfit.Sun), Is.EqualTo(DressDrop.Wrong));
            Assert.That(round.Worn, Is.EqualTo(Outfit.Sun));
            round.TakeOff();
            Assert.That(round.Worn, Is.Null);
            Assert.That(round.Done, Is.False);
            Assert.That(round.NeedsHint, Is.False);
            round.Drop(Outfit.Wind);
            Assert.That(round.NeedsHint);
            round.HintGiven();
            Assert.That(round.NeedsHint, Is.False);
            Assert.That(round.TotalWrongs, Is.EqualTo(2));
        }
    }
}
