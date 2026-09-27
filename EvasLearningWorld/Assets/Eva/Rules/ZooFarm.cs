using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    public enum Realm { Land, Sea, Air }

    // One animal in the shared Zoo & Farm dataset (docs/kids-games/full-catalogue-plan.md "6. Zoo & Farm"):
    // every one of the building's ten animal games reads the same table, each through its own column (or,
    // for Mother/Baby/Sound, through the animal's own Id - see ZooFarmRoundGenerator.Config). Footprint is
    // null for animals with no authored footprint sprite (birds' talons, fish, a snake's trail), which
    // ZooFarmRoundGenerator filters out rather than fake a footprint that doesn't exist.
    public sealed class Animal
    {
        public Animal(string id, string habitat, bool domestic, Realm realm, string food, string covering, string footprint)
        {
            Id = id;
            Habitat = habitat;
            Domestic = domestic;
            RealmOf = realm;
            Food = food;
            Covering = covering;
            Footprint = footprint;
        }

        public string Id { get; }
        public string Habitat { get; }
        public bool Domestic { get; }
        public Realm RealmOf { get; }
        public string Food { get; }
        public string Covering { get; }
        public string Footprint { get; }
    }

    // Placeholder content, same as every catalogue built this milestone (flagged in
    // docs/kids-games/full-catalogue-plan.md pending a real art/content pass). Ordered so the first six already
    // span both Domestic vs Wild and every Land/Sea/Air bucket - PoolSizeByLevel's early levels would otherwise
    // hand the two SORT games a run of animals that are all "domestic"/"land" and never show the other bucket.
    public static class ZooFarmAnimals
    {
        public static readonly Animal[] All =
        {
            new Animal("cow", "farm", true, Realm.Land, "grass", "fur", "cow"),
            new Animal("lion", "savanna", false, Realm.Land, "meat", "fur", "lion"),
            new Animal("duck", "pond", true, Realm.Sea, "seeds", "feathers", "duck"),
            new Animal("owl", "forest", false, Realm.Air, "mice", "feathers", null),
            new Animal("sheep", "farm", true, Realm.Land, "grass", "wool", "sheep"),
            new Animal("fish", "ocean", false, Realm.Sea, "plankton", "scales", null),
            new Animal("horse", "farm", true, Realm.Land, "hay", "fur", "horse"),
            new Animal("eagle", "mountain", false, Realm.Air, "meat", "feathers", null),
            new Animal("pig", "farm", true, Realm.Land, "feed", "skin", "pig"),
            new Animal("snake", "jungle", false, Realm.Land, "mice", "scales", null),
            new Animal("chicken", "farm", true, Realm.Land, "seeds", "feathers", "chicken"),
            new Animal("frog", "pond", false, Realm.Land, "insects", "skin", "frog"),
            new Animal("dog", "farm", true, Realm.Land, "kibble", "fur", "dog"),
            new Animal("cat", "farm", true, Realm.Land, "catfood", "fur", "cat"),
            new Animal("elephant", "savanna", false, Realm.Land, "leaves", "skin", "elephant"),
        };
    }

    // The ten Zoo & Farm animal games (Geography, the building's eleventh, has its own dataset - see
    // GeographyRoundGenerator - but reuses the same MatchRoundBuilder). Mother/Baby/Sound key off the animal's
    // own Id (the "value" a round matches on is just "which animal", same as Habitat/Food/Covering keying off
    // a shared attribute) - see the class comment on MatchRoundBuilder for why one algorithm covers both the
    // MATCH aspect games and the SORT bucket games.
    public enum ZooFarmGameKind { Habitat, Mother, Food, Sound, Footprint, Covering, Babies, DomesticVsWild, LandSeaAir, Classification }

    public static class ZooFarmRoundGenerator
    {
        public const int RoundsPerSession = 5;

        // Same growth shape as School's pool tables (e.g. WordToImageRoundGenerator). 15 animals total.
        private static readonly int[] PoolSizeByLevel = { 6, 8, 10, 12, 15, 15 };
        private static readonly int[] ChoiceCountByLevel = { 3, 3, 4, 4, 4, 4 };

        public static MatchRound Create(ZooFarmGameKind kind, int level, Random rng, string previousTargetId)
        {
            var config = Config(kind, level);
            return MatchRoundBuilder.Build(config.items, level, rng, previousTargetId, PoolSizeByLevel, ChoiceCountByLevel,
                config.choicePrefix, config.targetPrefix, config.promptKey, config.targetVoicePrefix);
        }

        private const string AnimalSprite = "zoofarm/animal_";

        private static (IReadOnlyList<(string id, string value)> items, string choicePrefix, string targetPrefix, string promptKey, string targetVoicePrefix) Config(ZooFarmGameKind kind, int level)
        {
            switch (kind)
            {
                case ZooFarmGameKind.Habitat: return (Items(a => a.Habitat), "zoofarm/habitat_", AnimalSprite, "zoofarm_prompt_habitat", "zoofarm_animal_");
                case ZooFarmGameKind.Mother: return (Items(a => a.Id), "zoofarm/mother_", AnimalSprite, "zoofarm_prompt_mother", "zoofarm_animal_");
                case ZooFarmGameKind.Food: return (Items(a => a.Food), "zoofarm/food_", AnimalSprite, "zoofarm_prompt_food", "zoofarm_animal_");
                case ZooFarmGameKind.Sound: return (Items(a => a.Id), AnimalSprite, null, "zoofarm_prompt_sound", "zoofarm_sound_");
                case ZooFarmGameKind.Footprint: return (Items(a => a.Footprint), "zoofarm/footprint_", AnimalSprite, "zoofarm_prompt_footprint", "zoofarm_animal_");
                case ZooFarmGameKind.Covering: return (Items(a => a.Covering), "zoofarm/covering_", AnimalSprite, "zoofarm_prompt_covering", "zoofarm_animal_");
                case ZooFarmGameKind.Babies: return (Items(a => a.Id), "zoofarm/baby_", AnimalSprite, "zoofarm_prompt_babies", "zoofarm_animal_");
                case ZooFarmGameKind.DomesticVsWild: return (Items(a => a.Domestic ? "domestic" : "wild"), "zoofarm/bucket_", AnimalSprite, "zoofarm_prompt_domestic_wild", "zoofarm_animal_");
                case ZooFarmGameKind.LandSeaAir: return (Items(a => a.RealmOf.ToString().ToLowerInvariant()), "zoofarm/bucket_", AnimalSprite, "zoofarm_prompt_land_sea_air", "zoofarm_animal_");
                case ZooFarmGameKind.Classification: return ClassificationConfig(level);
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        // Composes the other nine games' own attributes, built last per the plan: two buckets at low levels
        // (reuses Domestic vs Wild's own split), three at mid levels (Land/Sea/Air's), four compound buckets
        // (domestic/wild x land/water) once the child has seen both splits on their own.
        private static (IReadOnlyList<(string id, string value)>, string, string, string, string) ClassificationConfig(int level)
        {
            if (level <= 2) return (Items(a => a.Domestic ? "domestic" : "wild"), "zoofarm/bucket_", AnimalSprite, "zoofarm_prompt_classification", "zoofarm_animal_");
            if (level <= 4) return (Items(a => a.RealmOf.ToString().ToLowerInvariant()), "zoofarm/bucket_", AnimalSprite, "zoofarm_prompt_classification", "zoofarm_animal_");
            return (Items(a => (a.Domestic ? "domestic_" : "wild_") + (a.RealmOf == Realm.Land ? "land" : "water")), "zoofarm/bucket_", AnimalSprite, "zoofarm_prompt_classification", "zoofarm_animal_");
        }

        private static IReadOnlyList<(string id, string value)> Items(Func<Animal, string> selector)
        {
            var list = new List<(string, string)>();
            foreach (var a in ZooFarmAnimals.All)
            {
                var value = selector(a);
                if (value != null) list.Add((a.Id, value));
            }
            return list;
        }
    }
}
