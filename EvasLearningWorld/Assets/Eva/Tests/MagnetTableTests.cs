using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class MagnetTableTests
    {
        // A nail at x -300, a pencil at x 0, a paperclip at x 300, all at y 0.
        private static MagnetTable Table() => new MagnetTable(new (string Id, float X, float Y)[] { ("nail", -300f, 0f), ("pencil", 0f, 0f), ("paperclip", 300f, 0f) });

        private static void Hold(MagnetTable table, float x, float y, float seconds)
        {
            for (var t = 0f; t < seconds; t += 1f / 60f) table.Step(1f / 60f, x, y);
        }

        [Test]
        public void ASessionHasTwoRoundsOfThreeMagneticAndThreeOtherThingsNoThingTwice()
        {
            var session = MagnetTable.CreateSession(new System.Random(5));
            Assert.That(session.Length, Is.EqualTo(MagnetTable.RoundsPerSession));
            foreach (var round in session)
            {
                Assert.That(round.Length, Is.EqualTo(MagnetTable.ThingsPerRound));
                Assert.That(round.Count(MagnetTable.IsMagnetic), Is.EqualTo(3));
            }
            Assert.That(session.SelectMany(r => r).Distinct().Count(), Is.EqualTo(MagnetTable.ThingsPerRound * MagnetTable.RoundsPerSession));
        }

        [Test]
        public void TheTwelveThingsAreHalfMagneticWithArtForEach()
        {
            Assert.That(MagnetTable.Items.Count, Is.EqualTo(12));
            Assert.That(MagnetTable.Items.Count(i => i.Magnetic), Is.EqualTo(6));
            foreach (var item in MagnetTable.Items)
                Assert.IsNotNull(UnityEngine.Resources.Load<UnityEngine.Sprite>("Art/sciencelab/object_" + item.Id), item.Id);
        }

        [Test]
        public void AMagneticThingFarFromTheMagnetFeelsNothing()
        {
            var table = Table();
            Hold(table, -300f, MagnetTable.PullRange + 50f, 1f);
            Assert.That(table.Things[0].Pull, Is.EqualTo(0f));
            Assert.That(table.Things[0].State, Is.EqualTo(MagnetThingState.OnTable));
        }

        [Test]
        public void ThePullGrowsAsTheMagnetComesNearer()
        {
            var table = Table();
            Hold(table, -300f, MagnetTable.PullRange - 20f, 0.5f);
            var far = table.Things[0].Pull;
            Hold(table, -300f, MagnetTable.StickRange + 20f, 0.5f);
            var near = table.Things[0].Pull;
            Assert.That(far, Is.GreaterThan(0f));
            Assert.That(near, Is.GreaterThan(far));
            Assert.That(table.Things[0].State, Is.EqualTo(MagnetThingState.OnTable), "wiggling, not stuck yet");
        }

        [Test]
        public void AMagneticThingStaysOnTheTableUntilTheMagnetHasSatOnItAMoment()
        {
            var table = Table();
            Hold(table, -300f, 0f, MagnetTable.StickSeconds * 0.5f);
            Assert.That(table.Things[0].State, Is.EqualTo(MagnetThingState.OnTable));
            Hold(table, -300f, 0f, MagnetTable.StickSeconds);
            Assert.That(table.Things[0].State, Is.EqualTo(MagnetThingState.Stuck));
            Assert.That(table.StuckCount, Is.EqualTo(1));
        }

        [Test]
        public void TheJumpIsReportedOnceInTheStepItHappens()
        {
            var table = Table();
            var reported = 0;
            for (var t = 0f; t < 1f; t += 1f / 60f)
            {
                table.Step(1f / 60f, -300f, 0f);
                reported += table.JustStuck.Count;
            }
            Assert.That(reported, Is.EqualTo(1));
        }

        [Test]
        public void ThePassingMagnetThatNeverStopsLeavesTheThingWhereItWas()
        {
            var table = Table();
            for (var x = -600f; x <= 0f; x += 25f) table.Step(1f / 60f, x, 0f); // sweeps past at 1500 units a second
            Assert.That(table.Things[0].State, Is.EqualTo(MagnetThingState.OnTable));
        }

        [Test]
        public void AThingThatIsNotMagneticNeverMovesHoweverLongTheMagnetSitsOnIt()
        {
            var table = Table();
            Hold(table, 0f, 0f, 5f);
            Assert.That(table.Things[1].State, Is.EqualTo(MagnetThingState.OnTable));
            Assert.That(table.Things[1].Pull, Is.EqualTo(0f));
        }

        [Test]
        public void ThatTheMagnetDoesNotPullIsReportedOncePerVisit()
        {
            var table = Table();
            var reports = 0;
            for (var t = 0f; t < 4f; t += 1f / 60f)
            {
                table.Step(1f / 60f, 0f, 0f);
                reports += table.JustNotPulled.Count(i => i == 1);
            }
            Assert.That(reports, Is.EqualTo(1));
            Hold(table, 0f, 500f, 0.1f); // away, then back
            for (var t = 0f; t < 2f; t += 1f / 60f)
            {
                table.Step(1f / 60f, 0f, 0f);
                reports += table.JustNotPulled.Count(i => i == 1);
            }
            Assert.That(reports, Is.EqualTo(2));
        }

        [Test]
        public void StuckThingsHangInDifferentPlacesAndFallIntoTheBucketWhenDropped()
        {
            var table = Table();
            Hold(table, -300f, 0f, 0.6f);
            Hold(table, 300f, 0f, 0.6f);
            Assert.That(table.StuckCount, Is.EqualTo(2));
            Assert.That(table.Things[0].Slot, Is.Not.EqualTo(table.Things[2].Slot));
            Assert.That(table.Done, Is.False);
            Assert.That(table.DropStuck(), Is.EqualTo(2));
            Assert.That(table.StuckCount, Is.EqualTo(0));
            Assert.That(table.InBucketCount, Is.EqualTo(2));
            Assert.That(table.Done, Is.True, "both magnetic things are in; the pencil never needed to be");
        }

        [Test]
        public void ADroppedThingCannotBePulledAgain()
        {
            var table = Table();
            Hold(table, -300f, 0f, 0.6f);
            table.DropStuck();
            Hold(table, -300f, 0f, 1f);
            Assert.That(table.Things[0].State, Is.EqualTo(MagnetThingState.InBucket));
        }
    }
}
