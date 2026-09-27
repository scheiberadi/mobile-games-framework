using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class ParentGateTests
    {
        [Test]
        public void ThePoolHasAboutTwentyQuestions()
        {
            Assert.That(ParentGate.Count, Is.InRange(18, 24));
        }

        [Test]
        public void EveryQuestionHasThreeDistinctAnswersAndExactlyOneCorrectAtEveryRotation()
        {
            for (var i = 0; i < ParentGate.Count; i++)
            for (var rotation = 0; rotation < 3; rotation++)
            {
                var question = ParentGate.Build(i, rotation);
                Assert.IsNotEmpty(question.Text);
                Assert.That(question.Answers.Length, Is.EqualTo(3));
                Assert.That(question.Answers[0], Is.Not.EqualTo(question.Answers[1]));
                Assert.That(question.Answers[0], Is.Not.EqualTo(question.Answers[2]));
                Assert.That(question.Answers[1], Is.Not.EqualTo(question.Answers[2]));
                Assert.That(question.CorrectIndex, Is.InRange(0, 2));
            }
        }

        [Test]
        public void TheCorrectAnswerMovesWithTheRotationSoItIsNotAlwaysInTheSameSlot()
        {
            var seen = new System.Collections.Generic.HashSet<int>();
            for (var rotation = 0; rotation < 3; rotation++) seen.Add(ParentGate.Build(0, rotation).CorrectIndex);
            Assert.That(seen.Count, Is.EqualTo(3));
        }

        [Test]
        public void TheCorrectAnswerIsActuallyCorrect()
        {
            // "13 x 7" is the first pool entry.
            var question = ParentGate.Build(0, 0);
            Assert.That(question.Text, Does.Contain("13").And.Contain("7"));
            Assert.That(question.Answers[question.CorrectIndex], Is.EqualTo(91));
        }

        [Test]
        public void IndexesWrapAroundThePool()
        {
            Assert.That(ParentGate.Build(ParentGate.Count, 0).Text, Is.EqualTo(ParentGate.Build(0, 0).Text));
            Assert.That(ParentGate.Build(-1, 0).Text, Is.EqualTo(ParentGate.Build(ParentGate.Count - 1, 0).Text));
        }
    }
}
