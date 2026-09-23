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
    }

    public static class CountRoundGenerator
    {
        public const int RoundsPerSession = 5;
        // Quantity ceiling per round: the first rounds stay at 1 to 3 (a 4-year-old), the last reach 5.
        public static readonly int[] MaxQuantityByRound = { 3, 3, 4, 5, 5 };

        public static CountRound Create(int roundIndex, Random rng, CountObject? previous)
        {
            if (roundIndex < 0 || roundIndex >= RoundsPerSession) throw new ArgumentOutOfRangeException(nameof(roundIndex));
            var max = MaxQuantityByRound[roundIndex];
            var pool = Math.Max(3, max);
            var quantity = rng.Next(1, max + 1);
            var choices = new List<int> { quantity };
            while (choices.Count < 3)
            {
                var candidate = rng.Next(1, pool + 1);
                if (!choices.Contains(candidate)) choices.Add(candidate);
            }
            choices.Sort();
            CountObject obj;
            do { obj = (CountObject)rng.Next(0, 4); } while (previous.HasValue && obj == previous.Value);
            return new CountRound { Quantity = quantity, Object = obj, Choices = choices.ToArray() };
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
