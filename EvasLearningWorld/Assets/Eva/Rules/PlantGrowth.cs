using System;

namespace EvasLearningWorld.Rules
{
    // Plant Growth (M4.5 Science Lab), the building's one SEQUENCE game - see Rules/SequenceRoundBuilder.cs's class
    // comment. Five ordered growth stages; low levels play a 3-stage prefix (seed/sprout/flower), higher levels the
    // full 5 (adding seedling, then fruit) - same "prefix of an already-ordered list" shape Number Ordering uses for
    // its own numeral range, just growing which stages are in play instead of how many numbers.
    public static class PlantGrowthRoundGenerator
    {
        public const int RoundsPerSession = 5;

        private static readonly string[] Stages = { "seed", "sprout", "seedling", "flower", "fruit" };
        private static readonly int[] StageCountByLevel = { 3, 3, 4, 4, 5, 5 };

        public static SequenceRound Create(int level, Random rng) => SequenceRoundBuilder.Build(Stages, level, rng, StageCountByLevel);
    }
}
