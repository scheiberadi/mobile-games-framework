using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class HelpAndCoinsTests
    {
        [Test]
        public void MistakesClimbTheLadderAndStopAtDemonstrate()
        {
            var ladder = new HelpLadder();
            Assert.That(ladder.Step, Is.EqualTo(HelpStep.None));
            Assert.That(ladder.RecordMistake(), Is.EqualTo(HelpStep.Retry));
            Assert.That(ladder.RecordMistake(), Is.EqualTo(HelpStep.Hint));
            Assert.That(ladder.RecordMistake(), Is.EqualTo(HelpStep.Demonstrate));
            Assert.That(ladder.RecordMistake(), Is.EqualTo(HelpStep.Demonstrate));
            Assert.That(ladder.Mistakes, Is.EqualTo(3));
        }

        [Test]
        public void PayoutFallsWithHelpButNeverReachesZero()
        {
            Assert.That(CoinPayout.ForStep(HelpStep.None), Is.EqualTo(3));
            Assert.That(CoinPayout.ForStep(HelpStep.Retry), Is.EqualTo(2));
            Assert.That(CoinPayout.ForStep(HelpStep.Hint), Is.EqualTo(2));
            Assert.That(CoinPayout.ForStep(HelpStep.Demonstrate), Is.EqualTo(1));
            Assert.That(CoinPayout.MinSessionPayout, Is.EqualTo(CountRoundGenerator.RoundsPerSession));
        }
    }
}
