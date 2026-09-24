using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public enum CountObject { Apple, Star, Duck, Flower }

    public sealed class CountRound
    {
        public int Quantity;
        public CountObject Object;
        public int[] Choices;
        // Levels 5-6: other-object items mixed in with the counted ones (one entry per item, never the target
        // type). They are not counted and get no number; only Quantity items are the child's to count.
        public CountObject[] Distractors = new CountObject[0];

        public int TotalItems => Quantity + Distractors.Length;

        // One object type per on-screen slot (layout order): the target type for Quantity slots, distractor
        // types for the rest, shuffled so distractors are mixed in among the targets.
        public CountObject[] AssignSlots(Random rng)
        {
            var slots = new List<CountObject>(TotalItems);
            for (var i = 0; i < Quantity; i++) slots.Add(Object);
            slots.AddRange(Distractors);
            for (var i = slots.Count - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                (slots[i], slots[j]) = (slots[j], slots[i]);
            }
            return slots.ToArray();
        }

        // For each slot: its position in the counting order over target slots only (slots are already in
        // reading order), or -1 for a distractor that is never counted.
        public int[] TargetIndexBySlot(CountObject[] slots)
        {
            var result = new int[slots.Length];
            var next = 0;
            for (var i = 0; i < slots.Length; i++) result[i] = slots[i] == Object ? next++ : -1;
            return result;
        }
    }

    // Difficulty ladder (spec 4.3): six levels, each with its own quantity range and answer-choice count.
    // Levels 1-2 draw the quantity uniformly; levels 3-4 draw the max of two independent uniform draws, which
    // skews towards the top of the (bigger) range so level 4 reads as harder than level 3, not just "bigger".
    public static class CountRoundGenerator
    {
        public const int RoundsPerSession = 5;

        // Index i = level (i + 1). Same shape as the old MaxQuantityByRound, just keyed by level instead of
        // in-session round position.
        private static readonly int[] QuantityMaxByLevel = { 3, 5, 10, 20, 10, 15 };
        private static readonly int[] ChoiceCountByLevel = { 3, 3, 5, 5, 6, 6 };
        public const int MaxItems = 20;

        public static CountRound Create(int level, Random rng, CountObject? previous)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var index = level - DifficultyLadder.MinLevel;
            var max = QuantityMaxByLevel[index];
            var choiceCount = ChoiceCountByLevel[index];

            var quantity = level <= 2 ? rng.Next(1, max + 1) : Math.Max(rng.Next(1, max + 1), rng.Next(1, max + 1));

            // Deterministically guarantee the exact choice count: start with the correct quantity, then sample
            // without replacement from every other integer in the range (always large enough to fill), then
            // shuffle - never sorted, so a child can't learn "the answer is usually in this slot".
            var pool = new List<int>(max - 1);
            for (var n = 1; n <= max; n++) if (n != quantity) pool.Add(n);
            var choices = new List<int> { quantity };
            while (choices.Count < choiceCount)
            {
                var pick = rng.Next(0, pool.Count);
                choices.Add(pool[pick]);
                pool.RemoveAt(pick);
            }
            Shuffle(choices, rng);

            CountObject obj;
            do { obj = (CountObject)rng.Next(0, 4); } while (previous.HasValue && obj == previous.Value);
            var round = new CountRound { Quantity = quantity, Object = obj, Choices = choices.ToArray() };
            if (level >= 5) round.Distractors = DrawDistractors(level, quantity, obj, rng);
            return round;
        }

        // Level 5: 2-6 items of one other type. Level 6: 3-8 items of one or two other types. Always capped so
        // the total stays within MaxItems (level 6's minimum of 3 still fits: 15 + 3 <= 20).
        private static CountObject[] DrawDistractors(int level, int quantity, CountObject target, Random rng)
        {
            var count = level == 5 ? rng.Next(2, 7) : rng.Next(3, 9);
            count = Math.Min(count, MaxItems - quantity);
            var others = new List<CountObject>();
            foreach (CountObject o in Enum.GetValues(typeof(CountObject))) if (o != target) others.Add(o);
            var first = others[rng.Next(0, others.Count)];
            var result = new CountObject[count];
            var twoTypes = level == 6 && count >= 2 && rng.Next(0, 2) == 1;
            var firstCount = twoTypes ? rng.Next(1, count) : count;
            others.Remove(first);
            var second = others[rng.Next(0, others.Count)];
            for (var i = 0; i < count; i++) result[i] = i < firstCount ? first : second;
            return result;
        }

        private static void Shuffle(List<int> list, Random rng)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }

    // The rolling difficulty ladder (spec 4.3): keeps a small ring buffer (max 5) of the current level's most
    // recent round outcomes (true = clean, false = demonstrated) and evaluates after every round once the
    // buffer is full, sliding the window rather than waiting for fixed 5-round blocks. Level-up needs 4+ clean
    // of the latest 5; level-down needs 3+ demonstrated; a real change clears the buffer, a clamped-at-boundary
    // "would change but can't" is treated as no-change and keeps the buffer rolling. Stateless/static and
    // operates directly on the caller's buffer list so PlayerProgress can hold that list as-is (persistence).
    public static class DifficultyLadder
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 6;
        private const int WindowSize = 5;
        private const int LevelUpCleanThreshold = 4;
        private const int LevelDownDemonstratedThreshold = 3;

        // Appends `clean` to `buffer` (capped at WindowSize, oldest dropped first). Once the buffer holds a
        // full window, evaluates it and returns the resulting level; clears `buffer` only on an actual level
        // change. Returns `level` unchanged (buffer left rolling) when the buffer isn't full yet, when neither
        // threshold is met, or when a threshold is met but clamped at the boundary.
        public static int RecordRound(List<bool> buffer, int level, bool clean)
        {
            buffer.Add(clean);
            while (buffer.Count > WindowSize) buffer.RemoveAt(0);
            if (buffer.Count < WindowSize) return level;

            var cleanCount = 0;
            foreach (var c in buffer) if (c) cleanCount++;
            var demonstratedCount = buffer.Count - cleanCount;

            var newLevel = level;
            if (cleanCount >= LevelUpCleanThreshold) newLevel = Math.Min(MaxLevel, level + 1);
            else if (demonstratedCount >= LevelDownDemonstratedThreshold) newLevel = Math.Max(MinLevel, level - 1);

            if (newLevel != level) buffer.Clear();
            return newLevel;
        }
    }

    // One-to-one counting aid: each object is counted at most once, so repeated taps on one object
    // can never inflate the spoken number.
    public sealed class CountTally
    {
        private readonly bool[] _counted;

        public CountTally(int quantity) { _counted = new bool[quantity]; }

        public int Counted { get; private set; }
        public bool IsComplete => Counted == _counted.Length;
        public bool IsCounted(int objectIndex) => objectIndex >= 0 && objectIndex < _counted.Length && _counted[objectIndex];

        public bool TryCount(int objectIndex, out int number)
        {
            number = Counted;
            if (objectIndex < 0 || objectIndex >= _counted.Length || _counted[objectIndex]) return false;
            _counted[objectIndex] = true;
            number = ++Counted;
            return true;
        }
    }
}
