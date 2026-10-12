using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class WakeUpTests
    {
        // A dog at x -300, a stone at x 0, a cat at x 300, all at y 0.
        private static WakeUp Table() => new WakeUp(new (string Id, float X, float Y)[] { ("dog", -300f, 0f), ("stone", 0f, 0f), ("cat", 300f, 0f) });

        private static void Hold(WakeUp table, float x, float y, float seconds)
        {
            for (var t = 0f; t < seconds; t += 1f / 60f) table.Step(1f / 60f, x, y);
        }

        [Test]
        public void ASessionHasTwoRoundsOfThreeLivingAndThreeOtherThingsNoThingTwice()
        {
            var session = WakeUp.CreateSession(new System.Random(5));
            Assert.That(session.Length, Is.EqualTo(WakeUp.RoundsPerSession));
            foreach (var round in session)
            {
                Assert.That(round.Length, Is.EqualTo(WakeUp.ThingsPerRound));
                Assert.That(round.Count(WakeUp.IsAlive), Is.EqualTo(3));
            }
            Assert.That(session.SelectMany(r => r).Distinct().Count(), Is.EqualTo(WakeUp.ThingsPerRound * WakeUp.RoundsPerSession));
        }

        [Test]
        public void TheFourteenThingsAreHalfAliveWithArtForEach()
        {
            Assert.That(WakeUp.Items.Count, Is.EqualTo(14));
            Assert.That(WakeUp.Items.Count(i => i.Alive), Is.EqualTo(7));
            foreach (var item in WakeUp.Items)
                Assert.IsNotNull(UnityEngine.Resources.Load<UnityEngine.Sprite>("Art/sciencelab/object_" + item.Id), item.Id);
        }

        [Test]
        public void ALivingThingWakesWhenTheStarStaysOnIt()
        {
            var table = Table();
            Hold(table, -300f, 0f, WakeUp.WakeSeconds * 0.5f);
            Assert.That(table.Things[0].State, Is.EqualTo(WakeState.Asleep));
            Assert.That(table.Things[0].Progress, Is.GreaterThan(0f));
            Hold(table, -300f, 0f, WakeUp.WakeSeconds);
            Assert.That(table.Things[0].State, Is.EqualTo(WakeState.Awake));
        }

        [Test]
        public void FlyingPastDoesNotWakeIt()
        {
            var table = Table();
            for (var i = 0; i < 20; i++)
            {
                Hold(table, -300f, 0f, 0.1f);
                Hold(table, -300f, 600f, 0.4f);
            }
            Assert.That(table.Things[0].State, Is.EqualTo(WakeState.Asleep));
        }

        [Test]
        public void AThingThatIsNotAliveNeverWakesButIsReportedOnce()
        {
            var table = Table();
            Hold(table, 0f, 0f, WakeUp.NotAliveSeconds + 1f);
            Assert.That(table.Things[1].State, Is.EqualTo(WakeState.Asleep));
            Assert.That(table.Things[1].Dozing);
            Assert.That(table.Things[1].Told);
            Hold(table, 0f, 0f, 3f);
            Assert.That(table.JustNotAlive, Is.Empty, "told once per visit");
        }

        [Test]
        public void TheRoundIsDoneWhenEveryLivingThingIsInTheBasket()
        {
            var table = Table();
            Hold(table, -300f, 0f, 1f);
            Hold(table, 300f, 0f, 1f);
            Assert.That(table.Done, Is.False, "awake, not yet in the basket");
            table.Collect(0);
            Assert.That(table.Done, Is.False);
            table.Collect(2);
            Assert.That(table.Done);
            table.Collect(1);
            Assert.That(table.Things[1].State, Is.EqualTo(WakeState.Asleep), "a sleeping thing cannot be collected");
        }
    }
}
