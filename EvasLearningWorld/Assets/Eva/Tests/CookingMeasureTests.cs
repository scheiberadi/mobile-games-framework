using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class CookingMeasureTests
    {
        [Test]
        public void ASessionHasEachLevelOnceWithDifferentIngredients()
        {
            for (var seed = 0; seed < 60; seed++)
            {
                var session = CookingMeasure.CreateSession(new System.Random(seed));
                Assert.That(session.Length, Is.EqualTo(CookingMeasure.RoundsPerSession));
                Assert.That(session.Select(r => r.Target).Distinct().Count(), Is.EqualTo(3));
                Assert.That(session.Select(r => r.Ingredient).Distinct().Count(), Is.EqualTo(3));
            }
        }

        [Test]
        public void ACupToEmptyStartsFullAndTheOthersStartEmpty()
        {
            Assert.That(new CookRound("milk", CupLevel.Empty).Level, Is.EqualTo(1f));
            Assert.That(new CookRound("milk", CupLevel.Half).Level, Is.EqualTo(0f));
            Assert.That(new CookRound("milk", CupLevel.Full).Level, Is.EqualTo(0f));
        }

        [Test]
        public void TheLevelStaysBetweenEmptyAndFull()
        {
            var round = new CookRound("water", CupLevel.Half);
            round.Add(5f);
            Assert.That(round.Level, Is.EqualTo(1f));
            round.Add(-5f);
            Assert.That(round.Level, Is.EqualTo(0f));
        }

        [Test]
        public void HalfFullIsJudgedWithinATolerance()
        {
            var round = new CookRound("flour", CupLevel.Half);
            round.Add(0.3f);
            Assert.That(round.Judge(), Is.EqualTo(CookJudge.TooLittle));
            round.Add(0.2f);
            Assert.That(round.Look(), Is.EqualTo(CookJudge.Right));
            round.Add(0.3f);
            Assert.That(round.Judge(), Is.EqualTo(CookJudge.TooMuch));
            Assert.That(round.Done, Is.False);
            round.Add(-0.3f);
            Assert.That(round.Judge(), Is.EqualTo(CookJudge.Right));
            Assert.That(round.Done);
        }

        [Test]
        public void FullAndEmptyAreJudgedAtTheirEnds()
        {
            var full = new CookRound("water", CupLevel.Full);
            full.Add(0.85f);
            Assert.That(full.Judge(), Is.EqualTo(CookJudge.TooLittle));
            full.Add(0.2f);
            Assert.That(full.Judge(), Is.EqualTo(CookJudge.Right));

            var empty = new CookRound("milk", CupLevel.Empty);
            empty.Add(-0.5f);
            Assert.That(empty.Judge(), Is.EqualTo(CookJudge.TooMuch));
            empty.Add(-0.45f);
            Assert.That(empty.Judge(), Is.EqualTo(CookJudge.Right));
        }

        [Test]
        public void TwoMissesInARowCallForAHintAndARightCupEndsIt()
        {
            var round = new CookRound("water", CupLevel.Full);
            round.Judge();
            Assert.That(round.NeedsHint, Is.False);
            round.Judge();
            Assert.That(round.NeedsHint);
            round.HintGiven();
            Assert.That(round.NeedsHint, Is.False);
            Assert.That(round.TotalWrongs, Is.EqualTo(2));
        }

        [Test]
        public void TheRecipeCardUsesTheRightCupPicture()
        {
            Assert.That(CookingMeasure.CupPicture("water", CupLevel.Full), Is.EqualTo("cup_a"));
            Assert.That(CookingMeasure.CupPicture("flour", CupLevel.Half), Is.EqualTo("cup_e"));
            Assert.That(CookingMeasure.CupPicture("milk", CupLevel.Empty), Is.EqualTo("cup_i"));
        }
    }
}
