using System;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    // Zoo & Farm Covering as "dress the animal": one animal with a shadow over its trunk and a row of coverings (fur, wool, feathers, skin,
    // shell) to put on it. Only animals that have a trunk shadow (TrunkShadows, made by tools/art-import/make-shadows.js) are used, so the fish,
    // the snake and the other whole-body animals - and with them the scales - are not part of this game. The round is the shared MatchRound:
    // the animal is the target and the coverings are the choices, one of them the animal's own.
    public static class CoveringRules
    {
        public const int RoundsPerSession = ZooFarmRoundGenerator.RoundsPerSession;
        public static readonly int[] ChoiceCountByLevel = { 3, 3, 4, 4, 5, 5 };

        public static MatchRound Create(int level, Random rng, string previousTargetId)
        {
            var items = ZooFarmAnimals.All.Where(a => TrunkShadows.Centres.ContainsKey(a.Id)).Select(a => (a.Id, a.Covering)).ToList();
            return MatchRoundBuilder.Build(items, level, rng, previousTargetId, ZooFarmRoundGenerator.PoolSizeByLevel, ChoiceCountByLevel,
                "zoofarm/covering_", "zoofarm/animal_", "zoofarm_prompt_covering", "zoofarm_animal_");
        }
    }
}
