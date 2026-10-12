using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    // Science Lab's Healthy vs Unhealthy: "Feed Eva". Four foods lie on the kitchen table; the child feeds one to Eva. A healthy food lights
    // one of her five stars (and a new food takes its place); an unhealthy one makes her say "yuck": a lit star goes out and the food stays
    // where it was. The round is done when all five stars are lit. There are always at least two healthy foods and one unhealthy food on the
    // table, so there is always something right to do and something to refuse. Two unhealthy foods in a row call for a hint.
    //
    // Pure logic: no clock, no positions.
    public enum FoodResult { Healthy, Unhealthy }

    public sealed class FoodTable
    {
        public const int Slots = 4;
        public const int StarsNeeded = 5;
        public const int RoundsPerSession = 2;
        public const int MinHealthyOnTable = 2;
        public const int MinUnhealthyOnTable = 1;
        public const int UnhealthyForHint = 2;

        public static readonly IReadOnlyList<(string Id, bool Healthy)> Foods = new[]
        {
            ("apple", true), ("banana", true), ("broccoli", true), ("carrot", true), ("yogurt", true), ("grilled_fish", true), ("salad", true),
            ("candy", false), ("soda", false), ("chips", false), ("cake", false), ("donut", false), ("fries", false), ("pizza", false),
        };

        public static bool IsHealthy(string id)
        {
            foreach (var food in Foods) if (food.Id == id) return food.Healthy;
            throw new ArgumentException("no such food: " + id);
        }

        private readonly Random _rng;
        private readonly string[] _slots = new string[Slots];
        private readonly List<string> _deck = new List<string>();

        public FoodTable(Random rng)
        {
            _rng = rng;
            for (var i = 0; i < Slots; i++) _slots[i] = Draw(i);
        }

        public string FoodAt(int slot) => _slots[slot];
        public int Stars { get; private set; }
        public int UnhealthyInARow { get; private set; }
        public int TotalUnhealthy { get; private set; }
        public bool NeedsHint => UnhealthyInARow >= UnhealthyForHint;
        public bool Done => Stars >= StarsNeeded;

        // The first healthy food on the table (what a hint points at).
        public int HealthySlot()
        {
            for (var i = 0; i < Slots; i++) if (IsHealthy(_slots[i])) return i;
            return -1;
        }

        // The hint was shown: the count starts again.
        public void HintGiven() => UnhealthyInARow = 0;

        // The food in `slot` is fed to Eva. A healthy one is replaced by a new food; an unhealthy one stays on the table.
        public FoodResult Feed(int slot)
        {
            if (IsHealthy(_slots[slot]))
            {
                Stars = Math.Min(StarsNeeded, Stars + 1);
                UnhealthyInARow = 0;
                _slots[slot] = null;
                _slots[slot] = Draw(slot);
                return FoodResult.Healthy;
            }
            if (Stars > 0) Stars--;
            UnhealthyInARow++;
            TotalUnhealthy++;
            return FoodResult.Unhealthy;
        }

        // The next food for `slot` that is not on the table and keeps the minimums (healthy, unhealthy) true once the table is full.
        private string Draw(int slot)
        {
            var healthy = 0;
            var unhealthy = 0;
            var onTable = new HashSet<string>();
            for (var i = 0; i < Slots; i++)
            {
                if (i == slot || _slots[i] == null) continue;
                onTable.Add(_slots[i]);
                if (IsHealthy(_slots[i])) healthy++; else unhealthy++;
            }
            var emptyAfter = Slots - healthy - unhealthy - 1; // the other empty slots, still to fill after this one
            var needHealthy = Math.Max(0, MinHealthyOnTable - healthy) > emptyAfter;       // only a healthy one will do now
            var needUnhealthy = Math.Max(0, MinUnhealthyOnTable - unhealthy) > emptyAfter; // only an unhealthy one will do now
            for (var tries = 0; tries < 2; tries++)
            {
                for (var i = 0; i < _deck.Count; i++)
                {
                    var id = _deck[i];
                    if (onTable.Contains(id)) continue;
                    var isHealthy = IsHealthy(id);
                    if (needHealthy && !isHealthy) continue;
                    if (needUnhealthy && isHealthy) continue;
                    _deck.RemoveAt(i);
                    return id;
                }
                Refill(); // the deck ran dry: shuffle every food back in
            }
            throw new InvalidOperationException("no food fits the table");
        }

        private void Refill()
        {
            _deck.Clear();
            foreach (var food in Foods) _deck.Add(food.Id);
            for (var i = _deck.Count - 1; i > 0; i--)
            {
                var j = _rng.Next(i + 1);
                var swap = _deck[i];
                _deck[i] = _deck[j];
                _deck[j] = swap;
            }
        }
    }
}
