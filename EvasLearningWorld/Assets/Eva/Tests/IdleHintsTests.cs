using EvasLearningWorld.App;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class IdleHintsTests
    {
        [Test]
        public void AGameLeftOpenGetsTwoIdleHintsAndThenWaitsForTheChild()
        {
            var used = 0;
            Assert.That(IdleHints.Take(ref used));
            Assert.That(IdleHints.Take(ref used));
            Assert.That(IdleHints.Take(ref used), Is.False);
            Assert.That(IdleHints.Take(ref used), Is.False);
            Assert.That(used, Is.EqualTo(IdleHints.PerSession));
        }
    }
}
