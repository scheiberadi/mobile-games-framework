using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class SensesTests
    {
        [Test]
        public void EverySenseHasTwoThings()
        {
            foreach (Sense sense in Enum.GetValues(typeof(Sense)))
                Assert.AreEqual(2, Senses.Things.Count(t => t.Sense == sense), sense.ToString());
        }

        [Test]
        public void ASessionHasThreeRoundsOfThreeThingsForThreeDifferentSensesAndNoRepeats()
        {
            for (var seed = 0; seed < 50; seed++)
            {
                var session = Senses.CreateSession(new Random(seed));
                Assert.AreEqual(Senses.RoundsPerSession, session.Length);
                foreach (var round in session)
                {
                    Assert.AreEqual(Senses.ThingsPerRound, round.Length);
                    Assert.AreEqual(round.Length, round.Select(Senses.SenseOf).Distinct().Count(), "different senses in a round");
                }
                var all = session.SelectMany(r => r).ToArray();
                Assert.AreEqual(all.Length, all.Distinct().Count(), "no thing twice");
            }
        }

        [Test]
        public void TheRightOrganUsesTheThingAndTheRoundEndsWhenAllAreUsed()
        {
            var round = new SensesRound(new[] { "rainbow", "bell", "lemon" });
            Assert.AreEqual(SenseTry.Right, round.Try(0, Sense.Sight));
            Assert.IsTrue(round.IsUsed(0));
            Assert.AreEqual(SenseTry.AlreadyUsed, round.Try(0, Sense.Sight));
            Assert.IsFalse(round.Done);
            Assert.AreEqual(1, round.NextUnused());
            round.Try(1, Sense.Hearing);
            round.Try(2, Sense.Taste);
            Assert.IsTrue(round.Done);
            Assert.AreEqual(-1, round.NextUnused());
        }

        [Test]
        public void AWrongOrganChangesNothingAndTwoInARowCallForAHint()
        {
            var round = new SensesRound(new[] { "roses", "drum", "ice" });
            Assert.AreEqual(SenseTry.Wrong, round.Try(0, Sense.Taste));
            Assert.IsFalse(round.IsUsed(0));
            Assert.IsFalse(round.NeedsHint);
            Assert.AreEqual(SenseTry.Wrong, round.Try(0, Sense.Touch));
            Assert.IsTrue(round.NeedsHint);
            round.HintGiven();
            Assert.IsFalse(round.NeedsHint);
            Assert.AreEqual(2, round.TotalWrongs);
        }

        [Test]
        public void ARightTryClearsTheWrongsInARow()
        {
            var round = new SensesRound(new[] { "roses", "drum", "ice" });
            round.Try(0, Sense.Taste);
            round.Try(1, Sense.Hearing);
            round.Try(0, Sense.Taste);
            Assert.IsFalse(round.NeedsHint);
        }
    }
}
