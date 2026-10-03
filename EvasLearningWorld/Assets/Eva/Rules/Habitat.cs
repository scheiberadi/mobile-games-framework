using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    // One round of Zoo & Farm's Habitat as a "take the animal home" game: one animal, 3-4 habitats, the child
    // drags the animal to one. HabitatIds are shown left to right; CorrectIndex is the animal's own habitat.
    public sealed class HabitatRound
    {
        public string AnimalId;
        public string[] HabitatIds;
        public int CorrectIndex;
    }

    // What the animal does when it is put into a habitat. A wrong habitat is not refused, it is funny and says why
    // it is wrong (the animal returns to the child afterwards).
    public enum HabitatReaction
    {
        Home,    // its own habitat: settles in and is happy
        Sink,    // a land animal in the water: splash, bubbles
        Flop,    // a water animal out of the water: flops about
        Shiver,  // anywhere else: "this is not my home"
    }

    public static class HabitatRoundBuilder
    {
        public const int RoundsPerSession = 5;

        private static readonly int[] PoolSizeByLevel = { 6, 8, 10, 12, 15, 15 };
        private static readonly int[] ChoiceCountByLevel = { 3, 3, 4, 4, 4, 4 };

        private static readonly string[] WaterHabitats = { "ocean", "pond" };

        // Habitats that would also be a believable home for the animal, left out of its wrong choices so that a
        // wrong drop is always really wrong (a duck is at home on a farm and a fish in a pond too).
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

        public static HabitatRound Create(int level, Random rng, string previousAnimalId)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var index = level - DifficultyLadder.MinLevel;
            var pool = ZooFarmAnimals.All.Take(Math.Min(PoolSizeByLevel[index], ZooFarmAnimals.All.Length)).ToList();

            var animal = pool[rng.Next(pool.Count)];
            if (previousAnimalId != null && pool.Count > 1)
                while (animal.Id == previousAnimalId) animal = pool[rng.Next(pool.Count)];

            var believable = AlsoBelievable.TryGetValue(animal.Id, out var list) ? list : new string[0];
            var wrong = Habitats.Where(h => h != animal.Habitat && !believable.Contains(h)).OrderBy(_ => rng.Next()).ToList();
            var choices = new List<string> { animal.Habitat };
            choices.AddRange(wrong.Take(Math.Min(ChoiceCountByLevel[index], 1 + wrong.Count) - 1));
            choices = choices.OrderBy(_ => rng.Next()).ToList();

            return new HabitatRound
            {
                AnimalId = animal.Id,
                HabitatIds = choices.ToArray(),
                CorrectIndex = choices.IndexOf(animal.Habitat),
            };
        }

        public static HabitatReaction ReactionFor(string animalId, string habitatId)
        {
            var animal = ZooFarmAnimals.All.First(a => a.Id == animalId);
            if (animal.Habitat == habitatId) return HabitatReaction.Home;
            var water = WaterHabitats.Contains(habitatId);
            if (water && animal.RealmOf != Realm.Sea) return HabitatReaction.Sink;
            if (!water && animal.RealmOf == Realm.Sea) return HabitatReaction.Flop;
            return HabitatReaction.Shiver;
        }
    }
}
