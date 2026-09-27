using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    public sealed class SentenceBuilderItem
    {
        // The catalogue word this slot depicts. AsWord picks which sprite convention shows it: true draws the
        // printed word ("words/<Word>", Word to Image's own sprite), false draws its picture
        // ("wordtoimage/<Word>") - "pictograms first, words gradually replacing pictures at higher levels" per
        // the spec, gated by SentenceBuilderRoundGenerator's own WordSlotsByLevel.
        public string Word;
        public bool AsWord;

        // Where this item belongs - its position in the spoken sentence, left to right.
        public WorldPoint HomePosition;

        // Where it starts, scattered in the tray under a shuffled slot.
        public WorldPoint TrayPosition;
    }

    public sealed class SentenceBuilderRound
    {
        // Identifies the sentence Eva speaks in full ("sentence_<Key>") - a short, simplified stand-in sentence
        // built from the shared spelling catalogue's own words (real sentence-content authoring, beyond this
        // placeholder pool, is a later pass, same caveat as every other content catalogue this session).
        public string Key;
        public SentenceBuilderItem[] Items;

        // How close a dragged item's centre must land to its own HomePosition to count as placed.
        public float SnapRadius;
    }

    // Simple Sentence Builder (spec 4.2, Literacy): DRAG & DROP, the cluster's closing game and its second drag
    // interaction. Reuses Scrambled Word's own "snap when close to its own correct region" mechanic
    // (Rules/ScrambledWord.cs) with sentence pieces standing in for letters: each piece has a fixed home slot
    // (its position in the spoken sentence) and starts scattered in the tray under a shuffled slot. A small
    // curated catalogue of 2-slot sentences ("The cat has a cup.") stands in for real sentence content, which is
    // a later authoring pass; each needs its own full spoken-sentence line ("sentence_<key>") rather than
    // concatenating single-word audio, since a real sentence reads naturally only spoken whole. Progression is
    // two-fold: the sentence pool grows with level (PoolSizeByLevel), and pictograms are gradually replaced by
    // printed words (WordSlotsByLevel - 0 of 2 slots at low levels, up to both at the top), per the spec's own
    // "pictograms first, words gradually replacing pictures at higher levels." Own difficulty ladder
    // (PlayerProgress.SentenceBuilderLevel/Buffer).
    public static class SentenceBuilderRoundGenerator
    {
        // Fewer, longer rounds than a tap game - same reasoning as Jigsaw/Scrambled Word.
        public const int RoundsPerSession = 3;

        private sealed class Sentence
        {
            public readonly string Key;
            public readonly string Word1, Word2;
            public Sentence(string key, string word1, string word2) { Key = key; Word1 = word1; Word2 = word2; }
        }

        // Each sentence names two catalogue words (shared with Word to Image/Missing Letter/Build a
        // Word/Scrambled Word) in a fixed spoken order - placeholder sentence content, flagged for a real
        // authoring pass same as every other content pool this session.
        private static readonly Sentence[] Catalogue =
        {
            new Sentence("cat_cup", "cat", "cup"),
            new Sentence("dog_box", "dog", "box"),
            new Sentence("sun_hat", "sun", "hat"),
            new Sentence("cap_bed", "cap", "bed"),
            new Sentence("fox_bed", "fox", "bed"),
            new Sentence("cup_box", "cup", "box"),
        };

        private static readonly int[] PoolSizeByLevel = { 2, 3, 4, 5, 6, 6 };
        // How many of the 2 slots show the printed word instead of the picture at this level.
        private static readonly int[] WordSlotsByLevel = { 0, 0, 1, 1, 2, 2 };

        public const float SlotSize = 240f; // EvaUi.MinTap
        private const float SlotPitch = 320f;
        public static readonly WorldPoint BoardCenter = new WorldPoint(-200f, 100f);
        public static readonly WorldPoint TrayCenter = new WorldPoint(-200f, -220f);

        public static SentenceBuilderRound Create(int level, Random rng, string previousKey)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var index = level - DifficultyLadder.MinLevel;
            var poolSize = PoolSizeByLevel[index];
            var wordSlots = WordSlotsByLevel[index];
            var pool = Catalogue.Take(poolSize).ToList();

            Sentence sentence;
            do { sentence = pool[rng.Next(pool.Count)]; } while (previousKey != null && sentence.Key == previousKey && pool.Count > 1);

            var words = new[] { sentence.Word1, sentence.Word2 };
            var asWord = new bool[2];
            if (wordSlots == 1) asWord[rng.Next(2)] = true;
            else if (wordSlots >= 2) { asWord[0] = true; asWord[1] = true; }

            const int length = 2;
            var order = new int[length];
            for (var i = 0; i < length; i++) order[i] = i;
            do { Shuffle(order, rng); } while (IsIdentity(order));

            var items = new SentenceBuilderItem[length];
            for (var i = 0; i < length; i++)
            {
                var trayIndex = order[i];
                items[i] = new SentenceBuilderItem
                {
                    Word = words[i],
                    AsWord = asWord[i],
                    HomePosition = new WorldPoint(BoardCenter.X + (i - (length - 1) / 2f) * SlotPitch, BoardCenter.Y),
                    TrayPosition = new WorldPoint(TrayCenter.X + (trayIndex - (length - 1) / 2f) * SlotPitch, TrayCenter.Y),
                };
            }

            return new SentenceBuilderRound { Key = sentence.Key, Items = items, SnapRadius = SlotSize * 0.6f };
        }

        private static bool IsIdentity(int[] order)
        {
            for (var i = 0; i < order.Length; i++) if (order[i] != i) return false;
            return true;
        }

        private static void Shuffle(int[] values, Random rng)
        {
            for (var i = values.Length - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
        }
    }
}
