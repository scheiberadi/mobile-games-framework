using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    public sealed class ScrambledWordLetter
    {
        public char Letter;

        // Where this letter belongs - its position in the target word, left to right.
        public WorldPoint HomePosition;

        // Where it starts, scattered in the tray under a shuffled slot.
        public WorldPoint TrayPosition;
    }

    public sealed class ScrambledWordRound
    {
        // The target word (a picture of it, "wordtoimage/<Word>", is shown above the slots as a memory aid -
        // gameplay is ordering letters by sound/shape, not reading the picture - and Eva also speaks it aloud).
        public string Word;
        public ScrambledWordLetter[] Letters;

        // How close a dragged letter's centre must land to its own HomePosition to count as placed.
        public float SnapRadius;
    }

    // Scrambled Word (spec 4.2, Literacy): DRAG & DROP, the cluster's second spelling-composition game and its
    // first drag interaction - reuses Jigsaw's own "snap when close to its own correct region" mechanic
    // (Rules/Jigsaw.cs, App/Screens/JigsawScreen.cs) with letters standing in for puzzle pieces: each letter
    // has a fixed HomePosition (its slot in reading order) and a shuffled TrayPosition it starts from. Same
    // 3-letter catalogue as Missing Letter/Build a Word, so every round has exactly 3 slots - a scope
    // simplification, since this session has no longer-word content to draw on; a picture of the target word
    // (Word to Image's own "wordtoimage/<word>" sprite) is shown as a memory aid, since ordering letters purely
    // from a spoken word with no visual anchor would be too hard for this age group. Own difficulty ladder
    // (PlayerProgress.ScrambledWordLevel/Buffer) - only the word pool grows with level, not the slot count.
    public static class ScrambledWordRoundGenerator
    {
        // Fewer, longer rounds than a tap game - assembling a whole word takes longer than one tap, same
        // reasoning as Jigsaw's own RoundsPerSession.
        public const int RoundsPerSession = 3;

        private static readonly string[] Catalogue =
        {
            "cat", "hat", "dog", "fog", "sun", "fun", "cup", "cap", "box", "fox", "bed", "red",
        };
        private static readonly int[] PoolSizeByLevel = { 4, 6, 8, 10, 12, 12 };

        public const float SlotSize = 240f; // EvaUi.MinTap
        private const float SlotPitch = 280f;
        public static readonly WorldPoint BoardCenter = new WorldPoint(-200f, 100f);
        public static readonly WorldPoint TrayCenter = new WorldPoint(-200f, -220f);

        public static ScrambledWordRound Create(int level, Random rng, string previousWord)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var index = level - DifficultyLadder.MinLevel;
            var poolSize = PoolSizeByLevel[index];
            var pool = Catalogue.Take(poolSize).ToList();

            string word;
            do { word = pool[rng.Next(pool.Count)]; } while (previousWord != null && word == previousWord && pool.Count > 1);

            var length = word.Length;
            var order = new int[length];
            for (var i = 0; i < length; i++) order[i] = i;
            // Never hand out an already-solved scramble - the tray order must differ from reading order.
            do { Shuffle(order, rng); } while (IsIdentity(order) && length > 1);

            var letters = new ScrambledWordLetter[length];
            for (var i = 0; i < length; i++)
            {
                var trayIndex = order[i];
                letters[i] = new ScrambledWordLetter
                {
                    Letter = word[i],
                    HomePosition = new WorldPoint(BoardCenter.X + (i - (length - 1) / 2f) * SlotPitch, BoardCenter.Y),
                    TrayPosition = new WorldPoint(TrayCenter.X + (trayIndex - (length - 1) / 2f) * SlotPitch, TrayCenter.Y),
                };
            }

            return new ScrambledWordRound { Word = word, Letters = letters, SnapRadius = SlotSize * 0.6f };
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
