using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    // Which of the six shopping skills a round tests. Unlike every other game this session, level and mode are
    // the same thing here - each difficulty level is a different skill in the brief's own curriculum, not a
    // harder version of the same task. See SentenceBuilderRoundGenerator... no, see ShoppingRoundGenerator's own
    // class comment for the full reasoning.
    public enum ShoppingMode { Recognize, ExactPayment, Addition, Change, ComparePrices, Budget }

    public sealed class ShoppingRound
    {
        public ShoppingMode Mode;

        // Recognize: the shown coin/note's value ("money/coin_<v>" for v < 20, "money/note_<v>" otherwise).
        // Choices are bare numeral tiles; the child taps the one matching Denomination.
        public int Denomination;

        // ExactPayment/Addition/Change: the priced item on display ("objects/<item>").
        public CountObject Item;
        public int Price;

        // ExactPayment: Choices holds candidate denomination values (coin/note tiles); tap the one equal to Price.
        // Addition: ComboChoices holds candidate 2-coin combos (coin/coin tiles); tap the one summing to Price.
        // Change: Paid is the tendered denomination (Paid > Price); Choices holds candidate change amounts
        // (bare numeral tiles); tap the one equal to Paid - Price.
        public int Paid;
        public int[] Choices;
        public int[][] ComboChoices;

        // ComparePrices: two items shown side by side, each with its own price; tap the cheaper one.
        public CountObject Item2;
        public int Price2;

        // Budget: Items/Prices hold up to 3 priced items; exactly one is affordable within Budget.
        public int Budget;
        public CountObject[] Items;
        public int[] Prices;

        public int CorrectIndex;
    }

    // Shopping (spec 4.3, Store): Store's second Activity, own presenter - a mini shop scene (item + price +
    // money), never an equation screen, per the brief. Levels 1-7 in the brief (recognize coins -> notes ->
    // exact payment -> simple addition -> subtraction/change -> compare prices -> budget within an amount) don't
    // fit the shared 6-level DifficultyLadder one-for-one; rather than widen DifficultyLadder itself (a global
    // type every other game in the app also depends on - risky to touch for one activity), "recognize coins" and
    // "recognize notes" are merged into one level whose denomination pool already spans both coins and notes, so
    // the six ShoppingMode values below map onto the existing 6 levels exactly. Unlike every other game this
    // session, a round's *shape* changes with level, not just its numbers - this is the brief's own progressive
    // curriculum, not a simplification. Scope note: unlike every catalogue elsewhere this session, rounds here
    // don't track a "previous" value to avoid repeats - each mode already draws from several independent random
    // fields (item, price, denominations), so back-to-back repeats are already rare; flagged here rather than
    // adding a per-mode anti-repeat parameter for a game whose modes aren't comparable round to round anyway.
    // Real coin/note art is a separate content pipeline prerequisite the plan itself calls out (auto-placeholder
    // covers it meanwhile, same as every other placeholder catalogue this session). Own difficulty ladder
    // (PlayerProgress.ShoppingLevel/Buffer).
    public static class ShoppingRoundGenerator
    {
        public const int RoundsPerSession = 5;

        // Coins: 1, 2, 5, 10. Notes: 20, 50, 100 - a generic base-10 denomination sequence, not tied to any
        // real-world currency. Sprite key: "money/coin_<v>" below 20, "money/note_<v>" from 20 up.
        public static readonly int[] Denominations = { 1, 2, 5, 10, 20, 50, 100 };
        private static readonly int[] SmallDenominations = { 1, 2, 5, 10 };
        private static readonly int[] PaymentDenominations = { 1, 2, 5, 10, 20 };

        public static bool IsNote(int value) => value >= 20;

        // Same magnitude-confusion idea as every other guaranteed-distractor table this session, scoped to
        // "same digit, ten times the value" - the classic real-money mistake (a 1 for a 10, a 5 for a 50).
        private static readonly Dictionary<int, int> MagnitudePartner = new Dictionary<int, int>
        {
            { 1, 10 }, { 10, 1 }, { 2, 20 }, { 20, 2 }, { 5, 50 }, { 50, 5 },
        };

        public static ShoppingRound Create(int level, Random rng)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            switch (level)
            {
                case 1: return CreateRecognize(rng);
                case 2: return CreateExactPayment(rng);
                case 3: return CreateAddition(rng);
                case 4: return CreateChange(rng);
                case 5: return CreateComparePrices(rng);
                default: return CreateBudget(rng);
            }
        }

        private static ShoppingRound CreateRecognize(Random rng)
        {
            var target = Denominations[rng.Next(Denominations.Length)];
            var choices = new List<int> { target };
            if (MagnitudePartner.TryGetValue(target, out var partner) && !choices.Contains(partner)) choices.Add(partner);

            var pool = Denominations.Where(v => !choices.Contains(v)).ToList();
            Shuffle(pool, rng);
            while (choices.Count < 3 && pool.Count > 0)
            {
                choices.Add(pool[0]);
                pool.RemoveAt(0);
            }
            Shuffle(choices, rng);

            return new ShoppingRound
            {
                Mode = ShoppingMode.Recognize,
                Denomination = target,
                Choices = choices.ToArray(),
                CorrectIndex = choices.IndexOf(target),
            };
        }

        private static ShoppingRound CreateExactPayment(Random rng)
        {
            var price = PaymentDenominations[rng.Next(PaymentDenominations.Length)];
            var choices = new List<int> { price };
            if (MagnitudePartner.TryGetValue(price, out var partner) && !choices.Contains(partner)) choices.Add(partner);

            var pool = Denominations.Where(v => !choices.Contains(v)).ToList();
            Shuffle(pool, rng);
            while (choices.Count < 3 && pool.Count > 0)
            {
                choices.Add(pool[0]);
                pool.RemoveAt(0);
            }
            Shuffle(choices, rng);

            return new ShoppingRound
            {
                Mode = ShoppingMode.ExactPayment,
                Item = RandomItem(rng),
                Price = price,
                Choices = choices.ToArray(),
                CorrectIndex = choices.IndexOf(price),
            };
        }

        private static ShoppingRound CreateAddition(Random rng)
        {
            var d1 = SmallDenominations[rng.Next(SmallDenominations.Length)];
            int d2;
            do { d2 = SmallDenominations[rng.Next(SmallDenominations.Length)]; } while (d2 == d1);
            var price = d1 + d2;

            var combos = new List<int[]> { new[] { d1, d2 } };
            var sums = new List<int> { price };
            var attempts = 0;
            while (combos.Count < 3 && attempts < 50)
            {
                attempts++;
                var a = SmallDenominations[rng.Next(SmallDenominations.Length)];
                int b;
                do { b = SmallDenominations[rng.Next(SmallDenominations.Length)]; } while (b == a);
                var sum = a + b;
                if (sums.Contains(sum)) continue;
                combos.Add(new[] { a, b });
                sums.Add(sum);
            }
            ShuffleCombos(combos, sums, rng);

            return new ShoppingRound
            {
                Mode = ShoppingMode.Addition,
                Item = RandomItem(rng),
                Price = price,
                ComboChoices = combos.ToArray(),
                CorrectIndex = sums.IndexOf(price),
            };
        }

        private static ShoppingRound CreateChange(Random rng)
        {
            var price = rng.Next(1, 16);
            var paid = Denominations.First(v => v > price);
            var change = paid - price;

            var choices = new List<int> { change };
            var under = change - 1;
            var over = change + 1;
            if (under >= 0 && !choices.Contains(under)) choices.Add(under);
            else if (!choices.Contains(over)) choices.Add(over);

            var pool = new List<int>();
            for (var n = 0; n <= paid; n++) if (!choices.Contains(n)) pool.Add(n);
            Shuffle(pool, rng);
            while (choices.Count < 3 && pool.Count > 0)
            {
                choices.Add(pool[0]);
                pool.RemoveAt(0);
            }
            Shuffle(choices, rng);

            return new ShoppingRound
            {
                Mode = ShoppingMode.Change,
                Item = RandomItem(rng),
                Price = price,
                Paid = paid,
                Choices = choices.ToArray(),
                CorrectIndex = choices.IndexOf(change),
            };
        }

        private static ShoppingRound CreateComparePrices(Random rng)
        {
            var item1 = RandomItem(rng);
            CountObject item2;
            do { item2 = RandomItem(rng); } while (item2 == item1);

            int price1, price2;
            do
            {
                price1 = rng.Next(1, 31);
                price2 = rng.Next(1, 31);
            } while (price1 == price2);

            return new ShoppingRound
            {
                Mode = ShoppingMode.ComparePrices,
                Item = item1,
                Price = price1,
                Item2 = item2,
                Price2 = price2,
                CorrectIndex = price1 < price2 ? 0 : 1,
            };
        }

        private static ShoppingRound CreateBudget(Random rng)
        {
            var budget = rng.Next(10, 31);
            var objects = ((CountObject[])Enum.GetValues(typeof(CountObject))).ToList();
            Shuffle(objects, rng);
            var items = objects.Take(3).ToArray();
            var correctSlot = rng.Next(3);

            var prices = new int[3];
            var usedPrices = new List<int>();
            for (var i = 0; i < 3; i++)
            {
                int price;
                if (i == correctSlot)
                    do { price = rng.Next(1, budget + 1); } while (usedPrices.Contains(price));
                else
                    do { price = rng.Next(budget + 1, budget + 16); } while (usedPrices.Contains(price));
                usedPrices.Add(price);
                prices[i] = price;
            }

            return new ShoppingRound
            {
                Mode = ShoppingMode.Budget,
                Budget = budget,
                Items = items,
                Prices = prices,
                CorrectIndex = correctSlot,
            };
        }

        private static CountObject RandomItem(Random rng) => (CountObject)rng.Next(0, 4);

        private static void Shuffle<T>(List<T> list, Random rng)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // Keeps ComboChoices and their pre-computed sums in step while shuffling display order.
        private static void ShuffleCombos(List<int[]> combos, List<int> sums, Random rng)
        {
            for (var i = combos.Count - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                (combos[i], combos[j]) = (combos[j], combos[i]);
                (sums[i], sums[j]) = (sums[j], sums[i]);
            }
        }
    }
}
