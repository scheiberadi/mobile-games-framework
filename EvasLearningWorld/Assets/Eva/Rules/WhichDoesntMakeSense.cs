using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    public sealed class WhichDoesntMakeSenseRound
    {
        // In final (shuffled) display order; sprite key suffix is "whichdoesntmakesense/<item>" (see the screen).
        public string[] Items;
        public int OddIndex;
        // The pool entry this round drew from, for the caller's own previous-round repeat-avoidance.
        public int PoolIndex;
    }

    // Which Doesn't Make Sense? (Playground, spec 4.1): Logic/spatial, classification. A curated pool of scenario
    // pictures, one of them impossible (the plan's own examples: cow in ocean, fish in tree); the child taps the
    // impossible one. The plan gives this game no explicit set-size/content progression (unlike every other
    // Playground game), so unlike them the round shape (4 pictures) and the pool are the same at every level -
    // matching the parent-gate question pool (Rules/ParentGate.cs) it's explicitly modelled on, which has no
    // levels either. It still reports into the shared DifficultyLadder/PlayerProgress.WhichDoesntMakeSenseLevel
    // for consistency with every other game, ready for a future content pass to add real per-level tiers.
    public static class WhichDoesntMakeSenseRoundGenerator
    {
        public const int RoundsPerSession = 5;

        // Each entry: three scenarios that make sense together, plus one impossible one. Content is a placeholder
        // pool (sprite art is a later authoring/art pass, same open item the M4 plan flags for every game).
        private static readonly (string[] normal, string impossible)[] Pool =
        {
            (new[] { "cow_in_field", "dog_in_yard", "duck_in_pond" }, "fish_in_tree"),
            (new[] { "bird_in_nest", "bee_in_hive", "ant_in_anthill" }, "fish_in_desert"),
            (new[] { "boat_on_water", "fish_in_water", "duck_on_water" }, "cow_in_ocean"),
            (new[] { "car_on_road", "bike_on_road", "bus_on_road" }, "fish_on_road"),
            (new[] { "bird_flying_sky", "plane_flying_sky", "kite_flying_sky" }, "elephant_flying_sky"),
            (new[] { "penguin_on_ice", "polar_bear_on_ice", "seal_on_ice" }, "camel_on_ice"),
            (new[] { "cactus_in_desert", "camel_in_desert", "snake_in_desert" }, "penguin_in_desert"),
            (new[] { "monkey_in_jungle", "parrot_in_jungle", "snake_in_jungle" }, "polar_bear_in_jungle"),
            (new[] { "sheep_in_pasture", "goat_in_pasture", "horse_in_pasture" }, "shark_in_pasture"),
            (new[] { "frog_in_pond", "turtle_in_pond", "duck_in_pond2" }, "lion_in_pond"),
        };

        public static WhichDoesntMakeSenseRound Create(int level, Random rng, int? previousPoolIndex)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));

            int poolIndex;
            do { poolIndex = rng.Next(0, Pool.Length); } while (previousPoolIndex.HasValue && poolIndex == previousPoolIndex.Value && Pool.Length > 1);
            var entry = Pool[poolIndex];

            var items = new List<(string key, bool odd)>();
            foreach (var normal in entry.normal) items.Add((normal, false));
            items.Add((entry.impossible, true));
            Shuffle(items, rng);

            return new WhichDoesntMakeSenseRound
            {
                Items = items.Select(i => i.key).ToArray(),
                OddIndex = items.FindIndex(i => i.odd),
                PoolIndex = poolIndex,
            };
        }

        private static void Shuffle<T>(List<T> list, Random rng)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
