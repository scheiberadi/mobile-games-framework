namespace EvasLearningWorld.App
{
    // A game left open does not play itself: Eva's hand helps by itself at most twice per session; after that she waits for the child.
    public static class IdleHints
    {
        public const int PerSession = 2;

        // True (and counted) while this session still has an idle hint to give.
        public static bool Take(ref int used)
        {
            if (used >= PerSession) return false;
            used++;
            return true;
        }
    }
}
