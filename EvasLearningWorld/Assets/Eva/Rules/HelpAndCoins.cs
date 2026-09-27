namespace EvasLearningWorld.Rules
{
    public enum HelpStep { None = 0, Retry = 1, Hint = 2, Demonstrate = 3 }

    // 1st mistake: gentle retry. 2nd: contextual hint. 3rd: Eva demonstrates and the child does the correct action.
    public sealed class HelpLadder
    {
        public int Mistakes { get; private set; }
        public HelpStep Step => Mistakes >= 3 ? HelpStep.Demonstrate : (HelpStep)Mistakes;

        public HelpStep RecordMistake()
        {
            if (Step != HelpStep.Demonstrate) Mistakes++;
            return Step;
        }
    }

    public static class CoinPayout
    {
        public const int Clean = 3;
        public const int Assisted = 2;
        public const int Demonstrated = 1;
        public static int MinSessionPayout => CountRoundGenerator.RoundsPerSession * Demonstrated;

        public static int ForStep(HelpStep step)
        {
            if (step == HelpStep.None) return Clean;
            return step == HelpStep.Demonstrate ? Demonstrated : Assisted;
        }
    }
}
