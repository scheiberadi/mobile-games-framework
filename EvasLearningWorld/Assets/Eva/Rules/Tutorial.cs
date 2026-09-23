namespace EvasLearningWorld.Rules
{
    // Serialised as an int, so keep the order stable.
    public enum TutorialStep { CreateCharacter, PlaceStarter, GoToSchool, FirstGame, GoToStore, FirstPurchase, PlacePurchase, Done }

    public enum TutorialEvent { LookConfirmed, ItemPlaced, EnteredSchool, RoundsFinished, EnteredStore, ItemBought }

    public static class TutorialFlow
    {
        // Any event other than the one the current step waits for leaves the step unchanged.
        public static TutorialStep Next(TutorialStep step, TutorialEvent e)
        {
            switch (step)
            {
                case TutorialStep.CreateCharacter: return e == TutorialEvent.LookConfirmed ? TutorialStep.PlaceStarter : step;
                case TutorialStep.PlaceStarter: return e == TutorialEvent.ItemPlaced ? TutorialStep.GoToSchool : step;
                case TutorialStep.GoToSchool: return e == TutorialEvent.EnteredSchool ? TutorialStep.FirstGame : step;
                case TutorialStep.FirstGame: return e == TutorialEvent.RoundsFinished ? TutorialStep.GoToStore : step;
                case TutorialStep.GoToStore: return e == TutorialEvent.EnteredStore ? TutorialStep.FirstPurchase : step;
                case TutorialStep.FirstPurchase: return e == TutorialEvent.ItemBought ? TutorialStep.PlacePurchase : step;
                case TutorialStep.PlacePurchase: return e == TutorialEvent.ItemPlaced ? TutorialStep.Done : step;
                default: return step;
            }
        }
    }
}
