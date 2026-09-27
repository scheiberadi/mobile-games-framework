using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    public sealed class RhymingRound
    {
        // The spoken word (also shown as the target tile's picture - Rhyming can't work purely by ear without
        // per-word audio naming it, unlike Beginning Sound's letter-name stand-in, so this game gets real
        // per-word voice lines instead). Sprite key is "rhyming/<key>".
        public string TargetKey;
        public string[] Choices;
        public int CorrectIndex;
    }

    // Rhyming (spec 4.2, Literacy): MATCH, audio-led. Reuses Item to Shadow's own MATCH shell (target tile +
    // choice row) - unlike Beginning Sound, this one keeps the shown target tile, since the child needs to see
    // which word is being asked about while listening for its rhyme; the picture is a memory aid, the match
    // itself is still by sound, not shape. A small curated rhyme-family catalogue (four families of four words
    // each) gets its own per-word voice lines ("word_<key>") - the target word must be genuinely spoken for
    // this game to be solvable, so unlike Beginning Sound's letter-name stand-in, this one didn't cut that
    // corner. Own difficulty ladder (PlayerProgress.RhymingLevel/Buffer).
    public static class RhymingRoundGenerator
    {
        public const int RoundsPerSession = 5;

        private sealed class Item
        {
            public readonly string Key, Family;
            public Item(string key, string family) { Key = key; Family = family; }
        }

        private static readonly Item[] Catalogue =
        {
            new Item("cat", "at"), new Item("hat", "at"), new Item("bat", "at"), new Item("mat", "at"),
            new Item("dog", "og"), new Item("frog", "og"), new Item("log", "og"), new Item("jog", "og"),
            new Item("pan", "an"), new Item("fan", "an"), new Item("van", "an"), new Item("man", "an"),
            new Item("bug", "ug"), new Item("rug", "ug"), new Item("mug", "ug"), new Item("jug", "ug"),
        };

        // Each family's designated "near miss" - a phonetically closer false friend than a random other family,
        // guaranteed among the choices from NearFamilyFromLevel on (the same guaranteed-confusable idea as
        // every other game in this session, scoped to word families instead of single letters/numerals).
        private static readonly Dictionary<string, string> NearFamily = new Dictionary<string, string>
        {
            { "at", "an" }, { "an", "at" }, { "og", "ug" }, { "ug", "og" },
        };

        private static readonly int[] ChoiceCountByLevel = { 3, 3, 4, 4, 4, 4 };
        private const int NearFamilyFromLevel = 3;

        public static RhymingRound Create(int level, Random rng, string previousTarget)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var index = level - DifficultyLadder.MinLevel;
            var choiceCount = ChoiceCountByLevel[index];
            var families = Catalogue.Select(i => i.Family).Distinct().ToList();

            Item target;
            do
            {
                var family = families[rng.Next(families.Count)];
                var members = Catalogue.Where(i => i.Family == family).ToList();
                target = members[rng.Next(members.Count)];
            } while (previousTarget != null && target.Key == previousTarget && families.Count > 1);

            var sameFamilyOthers = Catalogue.Where(i => i.Family == target.Family && i.Key != target.Key).ToList();
            var correct = sameFamilyOthers[rng.Next(sameFamilyOthers.Count)];

            var choices = new List<string> { correct.Key };
            if (level >= NearFamilyFromLevel && NearFamily.TryGetValue(target.Family, out var nearFamily))
            {
                var nearItems = Catalogue.Where(i => i.Family == nearFamily).ToList();
                if (nearItems.Count > 0) choices.Add(nearItems[rng.Next(nearItems.Count)].Key);
            }

            var remaining = Catalogue.Where(i => i.Family != target.Family && !choices.Contains(i.Key)).ToList();
            Shuffle(remaining, rng);
            while (choices.Count < choiceCount && remaining.Count > 0)
            {
                choices.Add(remaining[0].Key);
                remaining.RemoveAt(0);
            }
            Shuffle(choices, rng);

            return new RhymingRound { TargetKey = target.Key, Choices = choices.ToArray(), CorrectIndex = choices.IndexOf(correct.Key) };
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
