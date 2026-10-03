using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    // What an item does when it is dropped on a wrong partner in a pairing game (PairingGame). A wrong drop is not
    // refused: the item reacts, then returns to the child. The animals' reactions say what is wrong with the place.
    public enum PairReaction
    {
        Sink,    // a land animal in the water: splash, bubbles
        Flop,    // a water animal out of the water: flops about
        Shiver,  // an animal anywhere else: teeth chattering, "this is not my home"
        Refuse,  // a baby with the wrong mother: the mother shakes her head
    }

    // Zoo & Farm's Habitat rules: which habitats are water, which animals are water animals, and which wrong
    // habitats would still be a believable home (kept off the screen next to the animal, see PairingGame).
    public static class HabitatRules
    {
        private static readonly string[] WaterHabitats = { "ocean", "pond" };

        // Habitats that would also be a believable home for the animal (a duck is at home on a farm and a fish in a pond too).
        private static readonly Dictionary<string, string[]> AlsoBelievable = new Dictionary<string, string[]>
        {
            { "duck", new[] { "farm", "ocean" } },
            { "fish", new[] { "pond" } },
            { "frog", new[] { "jungle" } },
            { "elephant", new[] { "jungle" } },
            { "eagle", new[] { "forest" } },
            { "owl", new[] { "mountain" } },
            { "snake", new[] { "forest" } },
            { "lion", new[] { "jungle" } },
        };

        // Every habitat some animal lives in, in the order the animals list them.
        public static IReadOnlyList<string> Habitats { get; } = ZooFarmAnimals.All.Select(a => a.Habitat).Distinct().ToArray();

        // True when `habitatId` is not the animal's own habitat but would pass for one.
        public static bool IsBelievableButWrong(string animalId, string habitatId) =>
            AlsoBelievable.TryGetValue(animalId, out var list) && list.Contains(habitatId);

        // What the animal does in a habitat that is not its own.
        public static PairReaction ReactionFor(string animalId, string habitatId)
        {
            var animal = ZooFarmAnimals.All.First(a => a.Id == animalId);
            var water = WaterHabitats.Contains(habitatId);
            if (water && animal.RealmOf != Realm.Sea) return PairReaction.Sink;
            if (!water && animal.RealmOf == Realm.Sea) return PairReaction.Flop;
            return PairReaction.Shiver;
        }
    }
}
