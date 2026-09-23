using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class ProgressTests
    {
        [Test]
        public void ABuyDeductsCoinsAndAddsTheItem()
        {
            var p = new PlayerProgress();
            p.AddCoins(12);
            Assert.That(p.TryBuy("rug"), Is.EqualTo(BuyResult.Bought));
            Assert.That(p.Coins, Is.EqualTo(7));
            Assert.That(p.Owned, Contains.Item("rug"));
        }

        [Test]
        public void NotEnoughCoinsChangesNothing()
        {
            var p = new PlayerProgress();
            p.AddCoins(4);
            Assert.That(p.TryBuy("rug"), Is.EqualTo(BuyResult.NotEnoughCoins));
            Assert.That(p.Coins, Is.EqualTo(4));
            Assert.That(p.Owned, Is.Empty);
        }

        [Test]
        public void ASecondBuyOfTheSameItemIsAlreadyOwned()
        {
            var p = new PlayerProgress();
            p.AddCoins(20);
            p.TryBuy("rug");
            Assert.That(p.TryBuy("rug"), Is.EqualTo(BuyResult.AlreadyOwned));
            Assert.That(p.Coins, Is.EqualTo(15));
        }

        [Test]
        public void AnUnknownIdIsUnknownItem()
        {
            var p = new PlayerProgress();
            p.AddCoins(20);
            Assert.That(p.TryBuy("nope"), Is.EqualTo(BuyResult.UnknownItem));
            Assert.That(p.Coins, Is.EqualTo(20));
        }

        [Test]
        public void TheStarterIsNeverSold()
        {
            var p = new PlayerProgress();
            p.AddCoins(20);
            Assert.That(p.TryBuy("sofa"), Is.EqualTo(BuyResult.UnknownItem));
            Assert.That(p.Owned, Does.Not.Contain("sofa"));
            Assert.That(p.Coins, Is.EqualTo(20));
        }

        [Test]
        public void GrantStarterAddsTheSofaOnce()
        {
            var p = new PlayerProgress();
            p.GrantStarter();
            p.GrantStarter();
            Assert.That(p.Owned, Is.EqualTo(new[] { "sofa" }));
        }

        [Test]
        public void AddCoinsAccumulates()
        {
            var p = new PlayerProgress();
            p.AddCoins(3);
            p.AddCoins(2);
            Assert.That(p.Coins, Is.EqualTo(5));
        }

        [Test]
        public void AdvanceReturnsTrueOnlyWhenTheStepChanged()
        {
            var p = new PlayerProgress();
            Assert.That(p.Tutorial, Is.EqualTo(TutorialStep.CreateCharacter));
            Assert.That(p.Advance(TutorialEvent.ItemBought), Is.False);
            Assert.That(p.Tutorial, Is.EqualTo(TutorialStep.CreateCharacter));
            Assert.That(p.Advance(TutorialEvent.LookConfirmed), Is.True);
            Assert.That(p.Tutorial, Is.EqualTo(TutorialStep.PlaceStarter));
        }

        [Test]
        public void ANewProgressStartsEmpty()
        {
            var p = new PlayerProgress();
            Assert.That(p.HasCharacter, Is.False);
            Assert.That(p.Look, Is.Not.Null);
            Assert.That(p.Owned, Is.Empty);
            Assert.That(p.House, Is.Not.Null);
            Assert.That(p.CountIntroSeen, Is.False);
            Assert.That(CharacterLook.HeadCount, Is.EqualTo(4));
            Assert.That(CharacterLook.ColorCount, Is.EqualTo(5));
        }
    }
}
