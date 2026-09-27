using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class TutorialFlowTests
    {
        [TestCase(TutorialStep.CreateCharacter, TutorialEvent.LookConfirmed, TutorialStep.PlaceStarter)]
        [TestCase(TutorialStep.PlaceStarter, TutorialEvent.ItemPlaced, TutorialStep.GoToSchool)]
        [TestCase(TutorialStep.GoToSchool, TutorialEvent.EnteredSchool, TutorialStep.FirstGame)]
        [TestCase(TutorialStep.FirstGame, TutorialEvent.RoundsFinished, TutorialStep.GoToStore)]
        [TestCase(TutorialStep.GoToStore, TutorialEvent.EnteredStore, TutorialStep.FirstPurchase)]
        [TestCase(TutorialStep.FirstPurchase, TutorialEvent.ItemBought, TutorialStep.PlacePurchase)]
        [TestCase(TutorialStep.PlacePurchase, TutorialEvent.ItemPlaced, TutorialStep.Done)]
        public void TheSevenTransitionsAdvance(TutorialStep from, TutorialEvent e, TutorialStep to)
        {
            Assert.That(TutorialFlow.Next(from, e), Is.EqualTo(to));
        }

        [TestCase(TutorialStep.CreateCharacter, TutorialEvent.ItemBought)]
        [TestCase(TutorialStep.PlaceStarter, TutorialEvent.EnteredStore)]
        [TestCase(TutorialStep.GoToSchool, TutorialEvent.ItemPlaced)]
        [TestCase(TutorialStep.FirstGame, TutorialEvent.EnteredSchool)]
        [TestCase(TutorialStep.GoToStore, TutorialEvent.ItemBought)]
        [TestCase(TutorialStep.FirstPurchase, TutorialEvent.ItemPlaced)]
        [TestCase(TutorialStep.PlacePurchase, TutorialEvent.ItemBought)]
        [TestCase(TutorialStep.Done, TutorialEvent.LookConfirmed)]
        public void OtherEventsLeaveTheStepUnchanged(TutorialStep step, TutorialEvent e)
        {
            Assert.That(TutorialFlow.Next(step, e), Is.EqualTo(step));
        }
    }
}
